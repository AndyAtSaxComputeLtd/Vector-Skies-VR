using UnityEngine;

namespace VectorSkiesVR.ProceduralCity
{
    /// <summary>
    /// Represents a single solid black tower with neon edges in the cyberpunk city
    /// Uses GPU instancing for performance
    /// </summary>
    public class WireframeTower : MonoBehaviour
    {
        [Header("Tower Dimensions")]
        [SerializeField] private float width = 10f;
        [SerializeField] private float height = 50f;
        [SerializeField] private float depth = 10f;
        
        [Header("Wireframe Settings")]
        [SerializeField] private float lineThickness = 0.3f; // Thicker for visibility against black mesh
        [SerializeField] private Color wireframeColor = new Color(0f, 1f, 1f, 1f); // Cyan default
        
        [Header("VR Optimization")]
        [SerializeField] private bool useGPUInstancing = true;
        [SerializeField] private int edgeSegments = 12; // Vertical edge divisions
        
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh wireframeMesh;
        
        public enum TowerColor
        {
            Cyan,    // Primary
            Purple,  // Accent
            Red      // Hazard
        }
        
        public enum BuildingShape
        {
            Square,      // Equal width and depth
            Rectangle,   // Different width and depth
            Rhomboid     // Parallelogram base (skewed)
        }
        
        private TowerColor towerColorType = TowerColor.Cyan;
        private BuildingShape buildingShape = BuildingShape.Square;
        private float skewAmount = 0f; // For rhomboid shapes
        
        void Awake()
        {
            // Ensure components are initialized
            InitializeComponents();
        }
        
        /// <summary>
        /// Initialize mesh components
        /// </summary>
        private void InitializeComponents()
        {
            if (meshFilter == null)
            {
                meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }
            
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }
            
            // Enable GPU instancing for Quest 3 performance
            if (useGPUInstancing && meshRenderer != null)
            {
                meshRenderer.receiveShadows = false;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            
            Debug.Log("[VSVR:WireframeTower] Tower component initialized");
        }
        
        /// <summary>
        /// Initialize tower with specific dimensions, color, and shape
        /// </summary>
        public void Initialize(float width, float height, float depth, TowerColor colorType, BuildingShape shape = BuildingShape.Square)
        {
            // Ensure components exist before initialization
            InitializeComponents();
            
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.towerColorType = colorType;
            this.buildingShape = shape;
            
            // Set skew for rhomboid shapes
            if (shape == BuildingShape.Rhomboid)
            {
                skewAmount = Random.Range(width * 0.15f, width * 0.35f);
            }
            
            SetColorByType();
            GenerateWireframeMesh();
            
            Debug.Log($"[VSVR:WireframeTower] Tower initialized - Type: {colorType} | Shape: {shape} | Size: {width:F1}x{height:F1}x{depth:F1}");
        }
        
        /// <summary>
        /// Set color based on tower type
        /// </summary>
        private void SetColorByType()
        {
            switch (towerColorType)
            {
                case TowerColor.Cyan:
                    wireframeColor = new Color(0f, 1f, 1f, 1f); // Cyan
                    break;
                case TowerColor.Purple:
                    wireframeColor = new Color(0.8f, 0f, 1f, 1f); // Purple
                    break;
                case TowerColor.Red:
                    wireframeColor = new Color(1f, 0f, 0f, 1f); // Red
                    break;
            }
        }
        
        /// <summary>
        /// Generate solid black mesh with neon wireframe edges
        /// Creates a solid building with glowing outlines for VR
        /// </summary>
        private void GenerateWireframeMesh()
        {
            if (meshFilter == null)
            {
                Debug.LogError("[VSVR:WireframeTower] MeshFilter is null! Cannot generate mesh.");
                return;
            }
            
            wireframeMesh = new Mesh();
            wireframeMesh.name = $"WireframeTower_{towerColorType}";
            
            Debug.Log("[VSVR:WireframeTower] Generating solid mesh with neon edges");
            
            // Generate vertices and indices for solid faces + neon edges
            Vector3[] vertices;
            int[] triangles;
            Color[] colors;
            
            GenerateSolidMeshWithNeonEdges(out vertices, out triangles, out colors);
            
            wireframeMesh.vertices = vertices;
            wireframeMesh.triangles = triangles;
            wireframeMesh.colors = colors;
            
            wireframeMesh.RecalculateNormals();
            wireframeMesh.RecalculateBounds();
            
            meshFilter.mesh = wireframeMesh;
        }
        
        /// <summary>
        /// Generate solid black mesh with neon wireframe edges
        /// Creates box faces (black) + edge lines (neon colored)
        /// </summary>
        private void GenerateSolidMeshWithNeonEdges(out Vector3[] vertices, out int[] triangles, out Color[] colors)
        {
            // Solid box: 8 corners, 24 vertices (4 per face * 6 faces)
            // Edges: 12 edges * 8 vertices per edge
            int solidVertexCount = 24; // 6 faces * 4 vertices
            int edgeCount = 12;
            int verticesPerEdge = 8;
            
            int totalVertices = solidVertexCount + (edgeCount * verticesPerEdge);
            vertices = new Vector3[totalVertices];
            colors = new Color[totalVertices];
            
            // Solid faces: 6 faces * 2 triangles * 3 indices = 36
            // Edges: 12 edges * 12 triangles * 3 indices = 432
            int solidTriangleCount = 36;
            int edgeTriangleCount = edgeCount * 12 * 3;
            triangles = new int[solidTriangleCount + edgeTriangleCount];
            
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;
            float thick = lineThickness * 0.5f;
            
            Color blackColor = new Color(0.05f, 0.05f, 0.05f, 1f); // Dark black for solid faces
            
            // Define 8 corners based on building shape
            Vector3 b1, b2, b3, b4, t1, t2, t3, t4;
            
            // Bottom face is always square/rectangular
            b1 = new Vector3(-halfWidth, 0, -halfDepth);
            b2 = new Vector3(halfWidth, 0, -halfDepth);
            b3 = new Vector3(halfWidth, 0, halfDepth);
            b4 = new Vector3(-halfWidth, 0, halfDepth);
            
            switch (buildingShape)
            {
                case BuildingShape.Rhomboid:
                    // Top is square but offset, creating skewed vertical faces
                    t1 = new Vector3(-halfWidth + skewAmount, height, -halfDepth);
                    t2 = new Vector3(halfWidth + skewAmount, height, -halfDepth);
                    t3 = new Vector3(halfWidth + skewAmount, height, halfDepth);
                    t4 = new Vector3(-halfWidth + skewAmount, height, halfDepth);
                    break;
                    
                default: // Square or Rectangle
                    // Top matches bottom exactly
                    t1 = new Vector3(-halfWidth, height, -halfDepth);
                    t2 = new Vector3(halfWidth, height, -halfDepth);
                    t3 = new Vector3(halfWidth, height, halfDepth);
                    t4 = new Vector3(-halfWidth, height, halfDepth);
                    break;
            }
            
            int vertIndex = 0;
            int triIndex = 0;
            
            // ===== PART 1: Generate solid black box faces =====
            
            // Front face (b1, b2, t2, t1)
            AddQuad(b1, b2, t2, t1, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Back face (b4, b3, t3, t4)
            AddQuad(b3, b4, t4, t3, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Left face (b4, b1, t1, t4)
            AddQuad(b4, b1, t1, t4, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Right face (b2, b3, t3, t2)
            AddQuad(b2, b3, t3, t2, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Bottom face (b4, b3, b2, b1)
            AddQuad(b4, b3, b2, b1, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Top face (t1, t2, t3, t4)
            AddQuad(t1, t2, t3, t4, blackColor, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // ===== PART 2: Generate neon colored edges =====
            
            // Bottom edges (4)
            AddEdge(b1, b2, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b2, b3, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b3, b4, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b4, b1, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Top edges (4)
            AddEdge(t1, t2, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(t2, t3, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(t3, t4, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(t4, t1, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            
            // Vertical edges (4)
            AddEdge(b1, t1, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b2, t2, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b3, t3, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
            AddEdge(b4, t4, thick, ref vertices, ref triangles, ref colors, ref vertIndex, ref triIndex);
        }
        
        /// <summary>
        /// Add a solid quad face to the mesh
        /// </summary>
        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color,
                            ref Vector3[] vertices, ref int[] triangles, ref Color[] colors,
                            ref int vertIndex, ref int triIndex)
        {
            // Add 4 vertices
            vertices[vertIndex + 0] = v1;
            vertices[vertIndex + 1] = v2;
            vertices[vertIndex + 2] = v3;
            vertices[vertIndex + 3] = v4;
            
            // Set color for all vertices
            colors[vertIndex + 0] = color;
            colors[vertIndex + 1] = color;
            colors[vertIndex + 2] = color;
            colors[vertIndex + 3] = color;
            
            // Create two triangles
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 2;
            triangles[triIndex++] = vertIndex + 1;
            
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 3;
            triangles[triIndex++] = vertIndex + 2;
            
            vertIndex += 4;
        }
        
        /// <summary>
        /// Add a single edge as a thin box mesh
        /// </summary>
        private void AddEdge(Vector3 start, Vector3 end, float thickness, 
                           ref Vector3[] vertices, ref int[] triangles, ref Color[] colors,
                           ref int vertIndex, ref int triIndex)
        {
            // Simplified edge creation - creates a quad facing camera
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
            
            Vector3 offset = perpendicular * thickness;
            
            // Create quad
            vertices[vertIndex + 0] = start - offset;
            vertices[vertIndex + 1] = start + offset;
            vertices[vertIndex + 2] = end + offset;
            vertices[vertIndex + 3] = end - offset;
            
            // Fill remaining vertices for box (simplified)
            for (int i = 4; i < 8; i++)
            {
                vertices[vertIndex + i] = vertices[vertIndex + i - 4];
            }
            
            // Set colors
            for (int i = 0; i < 8; i++)
            {
                colors[vertIndex + i] = wireframeColor;
            }
            
            // Create triangles for quad
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 2;
            triangles[triIndex++] = vertIndex + 1;
            
            triangles[triIndex++] = vertIndex + 0;
            triangles[triIndex++] = vertIndex + 3;
            triangles[triIndex++] = vertIndex + 2;
            
            // Simplified - duplicate for back face
            for (int i = 0; i < 30; i++)
            {
                if (triIndex < triangles.Length)
                    triangles[triIndex++] = vertIndex;
            }
            
            vertIndex += 8;
        }
        
        /// <summary>
        /// Set material for wireframe rendering
        /// </summary>
        public void SetMaterial(Material wireframeMaterial)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material = wireframeMaterial;
                
                // Enable GPU instancing
                if (useGPUInstancing)
                {
                    meshRenderer.material.enableInstancing = true;
                }
                
                Debug.Log("[VSVR:WireframeTower] Material applied with GPU instancing");
            }
            else
            {
                Debug.LogWarning("[VSVR:WireframeTower] MeshRenderer is null, cannot set material");
            }
        }
        
        /// <summary>
        /// Cleanup for pooling
        /// </summary>
        public void Cleanup()
        {
            if (wireframeMesh != null)
            {
                Destroy(wireframeMesh);
            }
            Debug.Log("[VSVR:WireframeTower] Tower cleaned up");
        }
    }
}
