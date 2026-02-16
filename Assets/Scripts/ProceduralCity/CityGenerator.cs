using UnityEngine;
using System.Collections.Generic;

namespace VectorSkiesVR.ProceduralCity
{
    /// <summary>
    /// Procedural city generator with grid-based layout and chunk streaming
    /// Generates infinite cyberpunk city with roads in a grid pattern
    /// Buildings are squares, rectangles, and rhomboids with no overlap
    /// Roads are trenches with 75-degree angled walls creating flight corridors
    /// Buildings sit at street level (Y=roadDepth), roads are sunken below
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int chunksAhead = 5;
        [SerializeField] private int chunksBehind = 2;
        [SerializeField] private float chunkSize = 100f;
        
        [Header("Grid Layout Settings")]
        [SerializeField] private float blockSize = 25f; // Size of each city block
        [SerializeField] private float roadWidth = 8f; // Width of roads between blocks
        [SerializeField] private int blocksPerRow = 4; // Number of blocks across (X axis)
        [SerializeField] private float buildingDensity = 0.85f; // Probability a block has a building
        
        [Header("Tower Settings")]
        [SerializeField] private float minTowerWidth = 8f;
        [SerializeField] private float maxTowerWidth = 20f;
        [SerializeField] private float minTowerHeight = 30f;
        [SerializeField] private float maxTowerHeight = 100f;
        [SerializeField] private float buildingPadding = 2f; // Spacing from block edges
        
        [Header("Color Distribution")]
        [Range(0f, 1f)]
        [SerializeField] private float cyanProbability = 0.7f; // 70% cyan
        [Range(0f, 1f)]
        [SerializeField] private float purpleProbability = 0.2f; // 20% purple
        // Remaining is red (hazard)
        
        [Header("Materials")]
        [SerializeField] private Material wireframeMaterial;
        [SerializeField] private Material roadMaterial;
        
        [Header("Road Visuals")]
        [SerializeField] private float roadDepth = 2f; // How deep roads are below building level
        [SerializeField] private float roadSideAngle = 75f; // Angle of road sides in degrees
        [SerializeField] private float roadEdgeThickness = 0.2f; // Neon edge line thickness
        [SerializeField] private Color roadSurfaceColor = new Color(0.02f, 0.02f, 0.02f, 1f); // Dark road surface
        [SerializeField] private Color roadSideColor = new Color(0.04f, 0.04f, 0.04f, 1f); // Slightly lighter for sides
        [SerializeField] private Color roadLineColor = new Color(0f, 0.8f, 1f, 1f); // Cyan edge lines
        
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        
        // Chunk management
        private Dictionary<int, CityChunk> activeChunks = new Dictionary<int, CityChunk>();
        private int currentChunkIndex;
        private int lastGeneratedChunk = -999;
        
        // Prefab pool
        private GameObject towerPrefab;
        private GameObject roadPrefab;
        
        // Rate limiting for logs
        private static float lastChunkUpdateLogTime;
        
        void Start()
        {
            VSVRLog.Info("CityGenerator", "Initializing procedural city generator");
            
            if (playerTransform == null)
            {
                playerTransform = Camera.main?.transform;
                if (playerTransform == null)
                {
                    VSVRLog.Error("CityGenerator", "Player transform/camera not found!");
                    return;
                }
            }
            
            CreateTowerPrefab();
            CreateRoadPrefab();
            GenerateInitialChunks();
            
            VSVRLog.Info("CityGenerator", $"Initialized | ChunkSize: {chunkSize}m | BlockSize: {blockSize}m | RoadWidth: {roadWidth}m | BlocksPerRow: {blocksPerRow}");
        }
        
        void Update()
        {
            UpdateChunks();
        }
        
        /// <summary>
        /// Create tower prefab for instantiation
        /// </summary>
        private void CreateTowerPrefab()
        {
            VSVRLog.Verbose("CityGenerator", "Creating tower prefab");
            towerPrefab = new GameObject("TowerPrefab");
            towerPrefab.AddComponent<WireframeTower>();
            towerPrefab.SetActive(false);
        }
        
        /// <summary>
        /// Create road prefab for instantiation
        /// </summary>
        private void CreateRoadPrefab()
        {
            VSVRLog.Verbose("CityGenerator", "Creating road prefab");
            roadPrefab = new GameObject("RoadPrefab");
            roadPrefab.AddComponent<MeshFilter>();
            roadPrefab.AddComponent<MeshRenderer>();
            roadPrefab.SetActive(false);
        }
        
        /// <summary>
        /// Generate initial chunks around player
        /// </summary>
        private void GenerateInitialChunks()
        {
            currentChunkIndex = GetChunkIndex(playerTransform.position.z);
            
            VSVRLog.Verbose("CityGenerator", $"Generating initial chunks around index {currentChunkIndex}");
            
            for (int i = -chunksBehind; i <= chunksAhead; i++)
            {
                GenerateChunk(currentChunkIndex + i);
            }
        }
        
        /// <summary>
        /// Update chunk generation based on player position
        /// </summary>
        private void UpdateChunks()
        {
            if (playerTransform == null) return;
            
            int newChunkIndex = GetChunkIndex(playerTransform.position.z);
            
            if (newChunkIndex != currentChunkIndex)
            {
                currentChunkIndex = newChunkIndex;
                
                VSVRLog.Verbose("CityGenerator", $"Player entered new chunk: {currentChunkIndex}");
                
                // Generate new chunks ahead
                for (int i = 1; i <= chunksAhead; i++)
                {
                    int chunkIndex = currentChunkIndex + i;
                    if (!activeChunks.ContainsKey(chunkIndex))
                    {
                        GenerateChunk(chunkIndex);
                    }
                }
                
                // Cleanup old chunks behind
                List<int> chunksToRemove = new List<int>();
                foreach (var chunk in activeChunks)
                {
                    if (chunk.Key < currentChunkIndex - chunksBehind)
                    {
                        chunksToRemove.Add(chunk.Key);
                    }
                }
                
                foreach (int chunkIndex in chunksToRemove)
                {
                    RemoveChunk(chunkIndex);
                }
                
                // Rate-limited status logging
                if (Time.time - lastChunkUpdateLogTime >= 5f)
                {
                    VSVRLog.Verbose("CityGenerator", $"Active chunks: {activeChunks.Count} | Current: {currentChunkIndex}");
                    lastChunkUpdateLogTime = Time.time;
                }
            }
        }
        
        /// <summary>
        /// Get chunk index from world Z position
        /// </summary>
        private int GetChunkIndex(float worldZ)
        {
            return Mathf.FloorToInt(worldZ / chunkSize);
        }
        
        /// <summary>
        /// Generate a new chunk at specified index using grid layout
        /// </summary>
        private void GenerateChunk(int chunkIndex)
        {
            if (activeChunks.ContainsKey(chunkIndex)) return;
            
            CityChunk newChunk = new CityChunk();
            newChunk.chunkIndex = chunkIndex;
            newChunk.chunkObject = new GameObject($"Chunk_{chunkIndex}");
            newChunk.chunkObject.transform.parent = transform;
            newChunk.towers = new List<GameObject>();
            newChunk.roads = new List<GameObject>();
            
            // Calculate chunk world position
            float chunkZ = chunkIndex * chunkSize;
            newChunk.chunkObject.transform.position = new Vector3(0, 0, chunkZ);
            
            // Generate grid-based city blocks
            Random.InitState(chunkIndex * 12345); // Deterministic randomness per chunk
            
            GenerateGridBlocks(newChunk, chunkZ);
            GenerateRoads(newChunk, chunkZ);
            
            activeChunks.Add(chunkIndex, newChunk);
            
            VSVRLog.Verbose("CityGenerator", $"Generated chunk {chunkIndex} at Z={chunkZ} with {newChunk.towers.Count} buildings in grid layout");
        }
        
        /// <summary>
        /// Generate buildings in a grid pattern with roads
        /// </summary>
        private void GenerateGridBlocks(CityChunk chunk, float chunkZ)
        {
            // Calculate how many blocks fit in this chunk (Z direction)
            int blocksInChunk = Mathf.FloorToInt(chunkSize / (blockSize + roadWidth));
            
            // Total width needed for blocks in X direction
            float totalWidth = (blocksPerRow * blockSize) + ((blocksPerRow - 1) * roadWidth);
            float startX = -totalWidth * 0.5f;
            
            for (int row = 0; row < blocksInChunk; row++)
            {
                for (int col = 0; col < blocksPerRow; col++)
                {
                    // Calculate block position
                    float blockX = startX + (col * (blockSize + roadWidth));
                    float blockZ = chunkZ + (row * (blockSize + roadWidth));
                    
                    // Decide if this block gets a building
                    if (Random.value < buildingDensity)
                    {
                        GenerateBuildingInBlock(chunk, blockX, blockZ);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate 1-4 buildings within a grid block arranged in columns
        /// </summary>
        private void GenerateBuildingInBlock(CityChunk chunk, float blockX, float blockZ)
        {
            // Randomly choose how many buildings wide (columns) this block will have
            int buildingsWide = Random.Range(1, 5); // 1 to 4 buildings
            
            // Calculate space available per building
            float availableWidth = blockSize - (buildingPadding * 2);
            float buildingSpacing = 1.5f; // Small gap between buildings
            float widthPerBuilding = (availableWidth - (buildingSpacing * (buildingsWide - 1))) / buildingsWide;
            
            // Generate each building in the row
            for (int i = 0; i < buildingsWide; i++)
            {
                // Random building shape
                WireframeTower.BuildingShape shape = GetRandomBuildingShape();
                
                // Calculate position for this building
                float localX = buildingPadding + (i * (widthPerBuilding + buildingSpacing)) + (widthPerBuilding * 0.5f);
                
                // Random dimensions that fit within the subdivided space
                float maxWidth = widthPerBuilding;
                float maxDepth = blockSize - (buildingPadding * 2);
                
                float width, depth;
                
                switch (shape)
                {
                    case WireframeTower.BuildingShape.Square:
                        // Equal width and depth
                        float size = Random.Range(minTowerWidth, Mathf.Min(maxTowerWidth, maxWidth, maxDepth));
                        width = Mathf.Min(size, maxWidth);
                        depth = Mathf.Min(size, maxDepth);
                        break;
                        
                    case WireframeTower.BuildingShape.Rectangle:
                        // Different width and depth
                        width = Random.Range(minTowerWidth * 0.6f, Mathf.Min(maxTowerWidth, maxWidth));
                        depth = Random.Range(minTowerWidth, Mathf.Min(maxTowerWidth, maxDepth));
                        // Ensure it's actually rectangular (not square)
                        if (Mathf.Abs(width - depth) < 2f)
                        {
                            if (width > depth && depth < maxDepth - 3f) depth += 3f;
                            else if (width < maxWidth - 3f) width += 3f;
                        }
                        break;
                        
                    case WireframeTower.BuildingShape.Rhomboid:
                    default:
                        // Rhomboid uses similar dimensions to rectangle
                        width = Random.Range(minTowerWidth * 0.6f, Mathf.Min(maxTowerWidth, maxWidth));
                        depth = Random.Range(minTowerWidth, Mathf.Min(maxTowerWidth, maxDepth));
                        break;
                }
                
                // Random height
                float height = Random.Range(minTowerHeight, maxTowerHeight);
                
                // Position building in block with more vertical variation
                // Each building can be placed anywhere within the block depth
                float minZ = blockZ + buildingPadding + (depth * 0.5f);
                float maxZ = blockZ + blockSize - buildingPadding - (depth * 0.5f);
                float randomZ = Random.Range(minZ, maxZ);
                
                Vector3 position = new Vector3(
                    blockX + localX,
                    roadDepth, // Buildings at street level, roads are trenches below
                    randomZ
                );
                
                // Determine color type
                WireframeTower.TowerColor colorType = GetRandomTowerColor();
                
                // Create building
                GameObject towerObj = Instantiate(towerPrefab, position, Quaternion.identity, chunk.chunkObject.transform);
                towerObj.SetActive(true);
                towerObj.name = $"Building_{shape}_{colorType}_{chunk.towers.Count}";
                
                WireframeTower tower = towerObj.GetComponent<WireframeTower>();
                tower.Initialize(width, height, depth, colorType, shape);
                tower.SetMaterial(wireframeMaterial);
                
                chunk.towers.Add(towerObj);
            }
        }
        
        /// <summary>
        /// Generate road meshes for the grid
        /// </summary>
        private void GenerateRoads(CityChunk chunk, float chunkZ)
        {
            int blocksInChunk = Mathf.FloorToInt(chunkSize / (blockSize + roadWidth));
            float totalWidth = (blocksPerRow * blockSize) + ((blocksPerRow - 1) * roadWidth);
            float startX = -totalWidth * 0.5f;
            
            // Generate horizontal roads (running along Z axis)
            for (int row = 0; row < blocksInChunk; row++)
            {
                float roadZ = chunkZ + (row * (blockSize + roadWidth)) + blockSize;
                
                if (row < blocksInChunk - 1 || chunkZ + chunkSize > roadZ + roadWidth)
                {
                    Vector3 roadPos = new Vector3(0, -roadDepth + 0.01f, roadZ + roadWidth * 0.5f);
                    CreateRoadSegment(chunk, roadPos, totalWidth + roadWidth, roadWidth, true);
                }
            }
            
            // Generate vertical roads (running along X axis)  
            for (int col = 0; col < blocksPerRow - 1; col++)
            {
                float roadX = startX + ((col + 1) * blockSize) + (col * roadWidth);
                
                Vector3 roadPos = new Vector3(roadX + roadWidth * 0.5f, -roadDepth + 0.01f, chunkZ + chunkSize * 0.5f);
                CreateRoadSegment(chunk, roadPos, roadWidth, chunkSize, false);
            }
        }
        
        /// <summary>
        /// Create a single road segment mesh
        /// </summary>
        private void CreateRoadSegment(CityChunk chunk, Vector3 position, float width, float length, bool horizontal)
        {
            GameObject roadObj = Instantiate(roadPrefab, position, Quaternion.identity, chunk.chunkObject.transform);
            roadObj.SetActive(true);
            roadObj.name = horizontal ? $"Road_H_{chunk.roads.Count}" : $"Road_V_{chunk.roads.Count}";
            
            MeshFilter meshFilter = roadObj.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = roadObj.GetComponent<MeshRenderer>();
            
            // Generate road mesh
            Mesh roadMesh = GenerateRoadMesh(width, length);
            meshFilter.mesh = roadMesh;
            
            // Apply material
            if (roadMaterial != null)
            {
                meshRenderer.material = roadMaterial;
            }
            else if (wireframeMaterial != null)
            {
                meshRenderer.material = wireframeMaterial;
            }
            
            meshRenderer.receiveShadows = false;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            chunk.roads.Add(roadObj);
        }
        
        /// <summary>
        /// Generate road mesh with dark surface, sloped sides, and neon edge lines
        /// Roads are trenches with angled walls for flight corridors
        /// </summary>
        private Mesh GenerateRoadMesh(float width, float length)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoadMesh";
            
            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            float edgeThick = roadEdgeThickness * 0.5f;
            
            // Calculate side wall offset based on angle
            // 75 degrees from vertical = 15 degrees from horizontal
            float angleFromHorizontal = 90f - roadSideAngle;
            float sideOffset = roadDepth * Mathf.Tan(angleFromHorizontal * Mathf.Deg2Rad);
            
            // Vertices needed:
            // Road surface: 4 vertices
            // Two side walls (long sides): 2 * 4 vertices each = 8
            // Neon edge lines on top: 4 edges * 8 vertices = 32
            int surfaceVertCount = 4;
            int sideWallVertCount = 8; // 2 walls * 4 verts each
            int edgeVertCount = 4 * 8;
            
            Vector3[] vertices = new Vector3[surfaceVertCount + sideWallVertCount + edgeVertCount];
            Color[] colors = new Color[vertices.Length];
            
            // Road surface vertices (at bottom of trench)
            vertices[0] = new Vector3(-halfWidth, 0, -halfLength);
            vertices[1] = new Vector3(halfWidth, 0, -halfLength);
            vertices[2] = new Vector3(halfWidth, 0, halfLength);
            vertices[3] = new Vector3(-halfWidth, 0, halfLength);
            
            colors[0] = colors[1] = colors[2] = colors[3] = roadSurfaceColor;
            
            // Side wall vertices (sloped at 75 degrees)
            // Left wall (along -X side, runs along Z)
            int leftWallStart = 4;
            vertices[leftWallStart + 0] = new Vector3(-halfWidth, 0, -halfLength); // Bottom front
            vertices[leftWallStart + 1] = new Vector3(-halfWidth, 0, halfLength);  // Bottom back
            vertices[leftWallStart + 2] = new Vector3(-halfWidth - sideOffset, roadDepth, halfLength);  // Top back
            vertices[leftWallStart + 3] = new Vector3(-halfWidth - sideOffset, roadDepth, -halfLength); // Top front
            
            colors[leftWallStart + 0] = colors[leftWallStart + 1] = colors[leftWallStart + 2] = colors[leftWallStart + 3] = roadSideColor;
            
            // Right wall (along +X side, runs along Z)
            int rightWallStart = 8;
            vertices[rightWallStart + 0] = new Vector3(halfWidth, 0, -halfLength); // Bottom front
            vertices[rightWallStart + 1] = new Vector3(halfWidth, 0, halfLength);  // Bottom back
            vertices[rightWallStart + 2] = new Vector3(halfWidth + sideOffset, roadDepth, halfLength);  // Top back
            vertices[rightWallStart + 3] = new Vector3(halfWidth + sideOffset, roadDepth, -halfLength); // Top front
            
            colors[rightWallStart + 0] = colors[rightWallStart + 1] = colors[rightWallStart + 2] = colors[rightWallStart + 3] = roadSideColor;
            
            // Triangles: surface (2 tris) + 2 side walls (2 tris each) + edge lines (36 tris each * 4)
            int[] triangles = new int[6 + 12 + (4 * 36)];
            int triIndex = 0;
            
            // Road surface triangles
            triangles[triIndex++] = 0; triangles[triIndex++] = 2; triangles[triIndex++] = 1;
            triangles[triIndex++] = 0; triangles[triIndex++] = 3; triangles[triIndex++] = 2;
            
            // Left wall triangles
            triangles[triIndex++] = leftWallStart + 0; triangles[triIndex++] = leftWallStart + 2; triangles[triIndex++] = leftWallStart + 1;
            triangles[triIndex++] = leftWallStart + 0; triangles[triIndex++] = leftWallStart + 3; triangles[triIndex++] = leftWallStart + 2;
            
            // Right wall triangles
            triangles[triIndex++] = rightWallStart + 0; triangles[triIndex++] = rightWallStart + 1; triangles[triIndex++] = rightWallStart + 2;
            triangles[triIndex++] = rightWallStart + 0; triangles[triIndex++] = rightWallStart + 2; triangles[triIndex++] = rightWallStart + 3;
            
            int vertIndex = 12;
            
            // Neon edge lines along top of walls
            Vector3[] topCorners = new Vector3[] {
                new Vector3(-halfWidth - sideOffset, roadDepth, -halfLength),
                new Vector3(halfWidth + sideOffset, roadDepth, -halfLength),
                new Vector3(halfWidth + sideOffset, roadDepth, halfLength),
                new Vector3(-halfWidth - sideOffset, roadDepth, halfLength)
            };
            
            for (int i = 0; i < 4; i++)
            {
                Vector3 start = topCorners[i];
                Vector3 end = topCorners[(i + 1) % 4];
                
                AddRoadEdge(start, end, edgeThick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Add a neon edge line to road top edge
        /// </summary>
        private void AddRoadEdge(Vector3 start, Vector3 end, float thickness,
                                 ref Vector3[] vertices, ref int[] triangles, ref Color[] colors,
                                 ref int vertIndex, ref int triIndex)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 offset = perpendicular * thickness;
            Vector3 heightOffset = Vector3.up * 0.02f;
            
            // Create thin box for edge line (already at correct height)
            vertices[vertIndex + 0] = start - offset + heightOffset;
            vertices[vertIndex + 1] = start + offset + heightOffset;
            vertices[vertIndex + 2] = end + offset + heightOffset;
            vertices[vertIndex + 3] = end - offset + heightOffset;
            vertices[vertIndex + 4] = start - offset;
            vertices[vertIndex + 5] = start + offset;
            vertices[vertIndex + 6] = end + offset;
            vertices[vertIndex + 7] = end - offset;
            
            for (int i = 0; i < 8; i++)
            {
                colors[vertIndex + i] = roadLineColor;
            }
            
            // Top face
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 2;
            triangles[triIndex++] = vertIndex + 1;
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 3;
            triangles[triIndex++] = vertIndex + 2;
            
            // Side faces (4 sides * 2 triangles each = 8 triangles = 24 indices)
            triangles[triIndex++] = vertIndex + 0; triangles[triIndex++] = vertIndex + 1; triangles[triIndex++] = vertIndex + 5;
            triangles[triIndex++] = vertIndex + 0; triangles[triIndex++] = vertIndex + 5; triangles[triIndex++] = vertIndex + 4;
            
            triangles[triIndex++] = vertIndex + 1; triangles[triIndex++] = vertIndex + 2; triangles[triIndex++] = vertIndex + 6;
            triangles[triIndex++] = vertIndex + 1; triangles[triIndex++] = vertIndex + 6; triangles[triIndex++] = vertIndex + 5;
            
            triangles[triIndex++] = vertIndex + 2; triangles[triIndex++] = vertIndex + 3; triangles[triIndex++] = vertIndex + 7;
            triangles[triIndex++] = vertIndex + 2; triangles[triIndex++] = vertIndex + 7; triangles[triIndex++] = vertIndex + 6;
            
            triangles[triIndex++] = vertIndex + 3; triangles[triIndex++] = vertIndex + 0; triangles[triIndex++] = vertIndex + 4;
            triangles[triIndex++] = vertIndex + 3; triangles[triIndex++] = vertIndex + 4; triangles[triIndex++] = vertIndex + 7;
            
            // Bottom face
            triangles[triIndex++] = vertIndex + 4; triangles[triIndex++] = vertIndex + 5; triangles[triIndex++] = vertIndex + 6;
            triangles[triIndex++] = vertIndex + 4; triangles[triIndex++] = vertIndex + 6; triangles[triIndex++] = vertIndex + 7;
            
            vertIndex += 8;
        }
        
        /// <summary>
        /// Get random building shape
        /// </summary>
        private WireframeTower.BuildingShape GetRandomBuildingShape()
        {
            float rand = Random.value;
            
            if (rand < 0.4f)
                return WireframeTower.BuildingShape.Square;
            else if (rand < 0.75f)
                return WireframeTower.BuildingShape.Rectangle;
            else
                return WireframeTower.BuildingShape.Rhomboid;
        }
        
        /// <summary>
        /// Get random tower color based on probability distribution
        /// </summary>
        private WireframeTower.TowerColor GetRandomTowerColor()
        {
            float rand = Random.value;
            
            if (rand < cyanProbability)
            {
                return WireframeTower.TowerColor.Cyan;
            }
            else if (rand < cyanProbability + purpleProbability)
            {
                return WireframeTower.TowerColor.Purple;
            }
            else
            {
                return WireframeTower.TowerColor.Red;
            }
        }
        
        /// <summary>
        /// Remove and cleanup a chunk
        /// </summary>
        private void RemoveChunk(int chunkIndex)
        {
            if (!activeChunks.ContainsKey(chunkIndex)) return;
            
            CityChunk chunk = activeChunks[chunkIndex];
            
            // Cleanup towers
            foreach (GameObject tower in chunk.towers)
            {
                WireframeTower towerComponent = tower.GetComponent<WireframeTower>();
                towerComponent?.Cleanup();
                Destroy(tower);
            }
            
            // Cleanup roads
            foreach (GameObject road in chunk.roads)
            {
                Destroy(road);
            }
            
            // Cleanup chunk object
            Destroy(chunk.chunkObject);
            
            activeChunks.Remove(chunkIndex);
            
            VSVRLog.Verbose("CityGenerator", $"Removed chunk {chunkIndex}");
        }
        
        /// <summary>
        /// Chunk data structure
        /// </summary>
        private class CityChunk
        {
            public int chunkIndex;
            public GameObject chunkObject;
            public List<GameObject> towers;
            public List<GameObject> roads;
        }
    }
}
