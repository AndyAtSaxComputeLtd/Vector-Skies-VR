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
        [SerializeField] private Color roadSurfaceColor = new Color(0.02f, 0.02f, 0.02f, 1f); // Dark road surface
        [SerializeField] private Color roadSideColor = new Color(0.04f, 0.04f, 0.04f, 1f); // Slightly lighter for sides
        
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
            
            // Calculate all road positions
            List<float> horizontalRoadZPositions = new List<float>();
            List<float> verticalRoadXPositions = new List<float>();
            
            // Horizontal roads (running along X axis)
            for (int row = 0; row < blocksInChunk; row++)
            {
                float roadZ = chunkZ + (row * (blockSize + roadWidth)) + blockSize;
                
                if (row < blocksInChunk - 1 || chunkZ + chunkSize > roadZ + roadWidth)
                {
                    horizontalRoadZPositions.Add(roadZ);
                }
            }
            
            // Vertical roads (running along Z axis)
            for (int col = 0; col < blocksPerRow - 1; col++)
            {
                float roadX = startX + ((col + 1) * blockSize) + (col * roadWidth);
                verticalRoadXPositions.Add(roadX);
            }
            
            // Generate horizontal road segments (cut at vertical road intersections)
            foreach (float roadZ in horizontalRoadZPositions)
            {
                if (verticalRoadXPositions.Count == 0)
                {
                    // No vertical roads, create full horizontal road
                    Vector3 roadPos = new Vector3(0, -roadDepth + 0.01f, roadZ + roadWidth * 0.5f);
                    CreateRoadSegment(chunk, roadPos, totalWidth + roadWidth, roadWidth, true);
                }
                else
                {
                    // Create segments between vertical roads
                    float startXPos = -(totalWidth + roadWidth) * 0.5f;
                    
                    for (int i = 0; i <= verticalRoadXPositions.Count; i++)
                    {
                        float segmentStartX, segmentEndX;
                        
                        if (i == 0)
                        {
                            // First segment: from edge to first vertical road
                            segmentStartX = startXPos;
                            segmentEndX = verticalRoadXPositions[0];
                        }
                        else if (i == verticalRoadXPositions.Count)
                        {
                            // Last segment: from last vertical road to edge
                            segmentStartX = verticalRoadXPositions[i - 1] + roadWidth;
                            segmentEndX = -startXPos;
                        }
                        else
                        {
                            // Middle segments: between vertical roads
                            segmentStartX = verticalRoadXPositions[i - 1] + roadWidth;
                            segmentEndX = verticalRoadXPositions[i];
                        }
                        
                        float segmentWidth = segmentEndX - segmentStartX;
                        if (segmentWidth > 0.1f) // Only create if segment is large enough
                        {
                            Vector3 roadPos = new Vector3(
                                segmentStartX + segmentWidth * 0.5f,
                                -roadDepth + 0.01f,
                                roadZ + roadWidth * 0.5f
                            );
                            CreateRoadSegment(chunk, roadPos, segmentWidth, roadWidth, true);
                        }
                    }
                }
            }
            
            // Generate vertical road segments (cut at horizontal road intersections)
            foreach (float roadX in verticalRoadXPositions)
            {
                if (horizontalRoadZPositions.Count == 0)
                {
                    // No horizontal roads, create full vertical road
                    Vector3 roadPos = new Vector3(roadX + roadWidth * 0.5f, -roadDepth + 0.01f, chunkZ + chunkSize * 0.5f);
                    CreateRoadSegment(chunk, roadPos, roadWidth, chunkSize, false);
                }
                else
                {
                    // Create segments between horizontal roads
                    float startZPos = chunkZ;
                    float endZPos = chunkZ + chunkSize;
                    
                    for (int i = 0; i <= horizontalRoadZPositions.Count; i++)
                    {
                        float segmentStartZ, segmentEndZ;
                        
                        if (i == 0)
                        {
                            // First segment: from chunk start to first horizontal road
                            segmentStartZ = startZPos;
                            if (horizontalRoadZPositions[0] >= startZPos && horizontalRoadZPositions[0] < endZPos)
                                segmentEndZ = horizontalRoadZPositions[0];
                            else
                                continue;
                        }
                        else if (i == horizontalRoadZPositions.Count)
                        {
                            // Last segment: from last horizontal road to chunk end
                            if (horizontalRoadZPositions[i - 1] >= startZPos && horizontalRoadZPositions[i - 1] < endZPos)
                            {
                                segmentStartZ = horizontalRoadZPositions[i - 1] + roadWidth;
                                segmentEndZ = endZPos;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            // Middle segments: between horizontal roads
                            if (horizontalRoadZPositions[i - 1] >= startZPos && horizontalRoadZPositions[i] < endZPos)
                            {
                                segmentStartZ = horizontalRoadZPositions[i - 1] + roadWidth;
                                segmentEndZ = horizontalRoadZPositions[i];
                            }
                            else
                                continue;
                        }
                        
                        float segmentLength = segmentEndZ - segmentStartZ;
                        if (segmentLength > 0.1f) // Only create if segment is large enough
                        {
                            Vector3 roadPos = new Vector3(
                                roadX + roadWidth * 0.5f,
                                -roadDepth + 0.01f,
                                segmentStartZ + segmentLength * 0.5f
                            );
                            CreateRoadSegment(chunk, roadPos, roadWidth, segmentLength, false);
                        }
                    }
                }
            }
            
            // Generate intersections
            foreach (float roadZ in horizontalRoadZPositions)
            {
                foreach (float roadX in verticalRoadXPositions)
                {
                    Vector3 intersectionPos = new Vector3(
                        roadX + roadWidth * 0.5f,
                        -roadDepth + 0.01f,
                        roadZ + roadWidth * 0.5f
                    );
                    CreateIntersection(chunk, intersectionPos, roadWidth);
                }
            }
        }
        
        /// <summary>
        /// Create an intersection mesh where roads cross
        /// </summary>
        private void CreateIntersection(CityChunk chunk, Vector3 position, float size)
        {
            GameObject intersectionObj = Instantiate(roadPrefab, position, Quaternion.identity, chunk.chunkObject.transform);
            intersectionObj.SetActive(true);
            intersectionObj.name = $"Intersection_{chunk.roads.Count}";
            
            MeshFilter meshFilter = intersectionObj.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = intersectionObj.GetComponent<MeshRenderer>();
            
            // Generate intersection mesh (square with sloped walls on all 4 sides)
            Mesh intersectionMesh = GenerateIntersectionMesh(size, size);
            meshFilter.mesh = intersectionMesh;
            
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
            
            chunk.roads.Add(intersectionObj);
        }
        
        /// <summary>
        /// Generate intersection mesh with corner walls to connect road segments
        /// </summary>
        private Mesh GenerateIntersectionMesh(float width, float length)
        {
            Mesh mesh = new Mesh();
            mesh.name = "IntersectionMesh";
            
            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            
            // Calculate side wall offset
            float angleFromHorizontal = 90f - roadSideAngle;
            float sideOffset = roadDepth * Mathf.Tan(angleFromHorizontal * Mathf.Deg2Rad);
            
            // Create surface + corner walls
            // Surface covers the intersection to prevent Z-fighting where road surfaces overlap
            // Surface (4) + 4 corner walls (3 verts each = 12)
            Vector3[] vertices = new Vector3[4 + 12];
            Color[] colors = new Color[vertices.Length];
            
            // Intersection surface at bottom (slightly higher than road segments to prevent Z-fighting)
            float surfaceY = 0.02f;
            vertices[0] = new Vector3(-halfWidth, surfaceY, -halfLength);
            vertices[1] = new Vector3(halfWidth, surfaceY, -halfLength);
            vertices[2] = new Vector3(halfWidth, surfaceY, halfLength);
            vertices[3] = new Vector3(-halfWidth, surfaceY, halfLength);
            
            colors[0] = colors[1] = colors[2] = colors[3] = roadSurfaceColor;
            
            // 4 corner walls to fill gaps between road segments
            // Each corner is a triangle from bottom corner to two top edges
            int cornerStart = 4;
            
            // Front-left corner
            vertices[cornerStart + 0] = new Vector3(-halfWidth, 0, -halfLength); // Bottom corner
            vertices[cornerStart + 1] = new Vector3(-halfWidth - sideOffset, roadDepth, -halfLength); // Top left edge
            vertices[cornerStart + 2] = new Vector3(-halfWidth, roadDepth, -halfLength - sideOffset); // Top front edge
            
            // Front-right corner
            vertices[cornerStart + 3] = new Vector3(halfWidth, 0, -halfLength); // Bottom corner
            vertices[cornerStart + 4] = new Vector3(halfWidth, roadDepth, -halfLength - sideOffset); // Top front edge
            vertices[cornerStart + 5] = new Vector3(halfWidth + sideOffset, roadDepth, -halfLength); // Top right edge
            
            // Back-right corner
            vertices[cornerStart + 6] = new Vector3(halfWidth, 0, halfLength); // Bottom corner
            vertices[cornerStart + 7] = new Vector3(halfWidth + sideOffset, roadDepth, halfLength); // Top right edge
            vertices[cornerStart + 8] = new Vector3(halfWidth, roadDepth, halfLength + sideOffset); // Top back edge
            
            // Back-left corner
            vertices[cornerStart + 9] = new Vector3(-halfWidth, 0, halfLength); // Bottom corner
            vertices[cornerStart + 10] = new Vector3(-halfWidth, roadDepth, halfLength + sideOffset); // Top back edge
            vertices[cornerStart + 11] = new Vector3(-halfWidth - sideOffset, roadDepth, halfLength); // Top left edge
            
            // Set corner wall colors
            for (int i = cornerStart; i < cornerStart + 12; i++)
            {
                colors[i] = roadSideColor;
            }
            
            // Triangles: surface (2) + 4 corner triangles (4)
            int[] triangles = new int[6 + 12];
            int triIndex = 0;
            
            // Surface triangles
            triangles[triIndex++] = 0; triangles[triIndex++] = 2; triangles[triIndex++] = 1;
            triangles[triIndex++] = 0; triangles[triIndex++] = 3; triangles[triIndex++] = 2;
            
            // 4 corner walls
            for (int i = 0; i < 4; i++)
            {
                int baseIdx = cornerStart + (i * 3);
                triangles[triIndex++] = baseIdx + 0;
                triangles[triIndex++] = baseIdx + 1;
                triangles[triIndex++] = baseIdx + 2;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
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
            Mesh roadMesh = GenerateRoadMesh(width, length, horizontal);
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
        /// Generate road mesh with dark surface and sloped side walls
        /// Roads are trenches with angled walls for flight corridors
        /// </summary>
        private Mesh GenerateRoadMesh(float width, float length, bool horizontal)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoadMesh";
            
            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            
            // Calculate side wall offset based on angle
            // 75 degrees from vertical = 15 degrees from horizontal
            float angleFromHorizontal = 90f - roadSideAngle;
            float sideOffset = roadDepth * Mathf.Tan(angleFromHorizontal * Mathf.Deg2Rad);
            
            // Vertices needed:
            // Road surface: 4 vertices
            // Two side walls: 2 * 4 vertices each = 8
            int surfaceVertCount = 4;
            int sideWallVertCount = 8; // 2 walls * 4 verts each
            
            Vector3[] vertices = new Vector3[surfaceVertCount + sideWallVertCount];
            Color[] colors = new Color[vertices.Length];
            
            // Road surface vertices (at bottom of trench)
            vertices[0] = new Vector3(-halfWidth, 0, -halfLength);
            vertices[1] = new Vector3(halfWidth, 0, -halfLength);
            vertices[2] = new Vector3(halfWidth, 0, halfLength);
            vertices[3] = new Vector3(-halfWidth, 0, halfLength);
            
            colors[0] = colors[1] = colors[2] = colors[3] = roadSurfaceColor;
            
            // Side wall vertices (sloped at 75 degrees)
            // Walls are perpendicular to road direction
            int wall1Start = 4;
            int wall2Start = 8;
            
            if (!horizontal)
            {
                // Vertical road (runs along Z): walls on left (-X) and right (+X) sides
                // Left wall
                vertices[wall1Start + 0] = new Vector3(-halfWidth, 0, -halfLength); // Bottom front
                vertices[wall1Start + 1] = new Vector3(-halfWidth, 0, halfLength);  // Bottom back
                vertices[wall1Start + 2] = new Vector3(-halfWidth - sideOffset, roadDepth, halfLength);  // Top back
                vertices[wall1Start + 3] = new Vector3(-halfWidth - sideOffset, roadDepth, -halfLength); // Top front
                
                // Right wall
                vertices[wall2Start + 0] = new Vector3(halfWidth, 0, -halfLength); // Bottom front
                vertices[wall2Start + 1] = new Vector3(halfWidth, 0, halfLength);  // Bottom back
                vertices[wall2Start + 2] = new Vector3(halfWidth + sideOffset, roadDepth, halfLength);  // Top back
                vertices[wall2Start + 3] = new Vector3(halfWidth + sideOffset, roadDepth, -halfLength); // Top front
            }
            else
            {
                // Horizontal road (runs along X): walls on front (-Z) and back (+Z) sides
                // Front wall (runs along X)
                vertices[wall1Start + 0] = new Vector3(-halfWidth, 0, -halfLength); // Bottom left
                vertices[wall1Start + 1] = new Vector3(halfWidth, 0, -halfLength);  // Bottom right
                vertices[wall1Start + 2] = new Vector3(halfWidth, roadDepth, -halfLength - sideOffset);  // Top right
                vertices[wall1Start + 3] = new Vector3(-halfWidth, roadDepth, -halfLength - sideOffset); // Top left
                
                // Back wall (runs along X)
                vertices[wall2Start + 0] = new Vector3(-halfWidth, 0, halfLength); // Bottom left
                vertices[wall2Start + 1] = new Vector3(halfWidth, 0, halfLength);  // Bottom right
                vertices[wall2Start + 2] = new Vector3(halfWidth, roadDepth, halfLength + sideOffset);  // Top right
                vertices[wall2Start + 3] = new Vector3(-halfWidth, roadDepth, halfLength + sideOffset); // Top left
            }
            
            colors[wall1Start + 0] = colors[wall1Start + 1] = colors[wall1Start + 2] = colors[wall1Start + 3] = roadSideColor;
            colors[wall2Start + 0] = colors[wall2Start + 1] = colors[wall2Start + 2] = colors[wall2Start + 3] = roadSideColor;
            
            // Triangles: surface (2 tris) + 2 side walls (2 tris each)
            int[] triangles = new int[6 + 12];
            int triIndex = 0;
            
            // Road surface triangles
            triangles[triIndex++] = 0; triangles[triIndex++] = 2; triangles[triIndex++] = 1;
            triangles[triIndex++] = 0; triangles[triIndex++] = 3; triangles[triIndex++] = 2;
            
            // Wall triangles (winding order depends on orientation)
            if (!horizontal)
            {
                // Vertical roads: walls on ±X sides
                // Wall 1 (left)
                triangles[triIndex++] = wall1Start + 0; triangles[triIndex++] = wall1Start + 2; triangles[triIndex++] = wall1Start + 1;
                triangles[triIndex++] = wall1Start + 0; triangles[triIndex++] = wall1Start + 3; triangles[triIndex++] = wall1Start + 2;
                
                // Wall 2 (right)
                triangles[triIndex++] = wall2Start + 0; triangles[triIndex++] = wall2Start + 1; triangles[triIndex++] = wall2Start + 2;
                triangles[triIndex++] = wall2Start + 0; triangles[triIndex++] = wall2Start + 2; triangles[triIndex++] = wall2Start + 3;
            }
            else
            {
                // Horizontal roads: walls on ±Z sides
                // Wall 1 (front)
                triangles[triIndex++] = wall1Start + 0; triangles[triIndex++] = wall1Start + 1; triangles[triIndex++] = wall1Start + 2;
                triangles[triIndex++] = wall1Start + 0; triangles[triIndex++] = wall1Start + 2; triangles[triIndex++] = wall1Start + 3;
                
                // Wall 2 (back)
                triangles[triIndex++] = wall2Start + 0; triangles[triIndex++] = wall2Start + 2; triangles[triIndex++] = wall2Start + 1;
                triangles[triIndex++] = wall2Start + 0; triangles[triIndex++] = wall2Start + 3; triangles[triIndex++] = wall2Start + 2;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
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
