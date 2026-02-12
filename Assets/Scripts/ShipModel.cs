using UnityEngine;

namespace VectorSkiesVR
{
    /// <summary>
    /// Simple ship model visualizer for external chase camera
    /// Creates a basic geometric ship representation
    /// </summary>
    public class ShipModel : MonoBehaviour
    {
        [Header("Ship Dimensions")]
        [SerializeField] private float length = 3f;
        [SerializeField] private float width = 1.5f;
        [SerializeField] private float height = 0.5f;
        
        [Header("Visual Settings")]
        [SerializeField] private Color primaryColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color accentColor = new Color(1f, 0.5f, 0f, 1f); // Orange
        [SerializeField] private Material wireframeMaterial;
        
        [Header("Effects")]
        [SerializeField] private bool showEngineGlow = true;
        [SerializeField] private float engineGlowIntensity = 2f;
        
        private GameObject shipMesh;
        private GameObject[] engineGlows;
        
        void Start()
        {
            CreateShipGeometry();
        }
        
        /// <summary>
        /// Create simple wireframe ship geometry
        /// </summary>
        private void CreateShipGeometry()
        {
            shipMesh = new GameObject("ShipMesh");
            shipMesh.transform.parent = transform;
            shipMesh.transform.localPosition = Vector3.zero;
            
            // Create main body
            GameObject body = CreateWireframeBox("Body", Vector3.zero, length, width, height);
            body.transform.parent = shipMesh.transform;
            
            // Create nose cone
            GameObject nose = CreateWireframeCone("Nose", new Vector3(0, 0, length * 0.6f), width * 0.3f, length * 0.4f);
            nose.transform.parent = shipMesh.transform;
            
            // Create wings
            GameObject leftWing = CreateWireframeBox("LeftWing", new Vector3(-width * 1.2f, 0, -length * 0.2f), 
                                                     length * 0.4f, width * 1.5f, height * 0.2f);
            leftWing.transform.parent = shipMesh.transform;
            
            GameObject rightWing = CreateWireframeBox("RightWing", new Vector3(width * 1.2f, 0, -length * 0.2f),
                                                      length * 0.4f, width * 1.5f, height * 0.2f);
            rightWing.transform.parent = shipMesh.transform;
            
            // Create engine glows
            if (showEngineGlow)
            {
                CreateEngineGlows();
            }
        }
        
        /// <summary>
        /// Create wireframe box
        /// </summary>
        private GameObject CreateWireframeBox(string name, Vector3 position, float length, float width, float height)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.localPosition = position;
            box.transform.localScale = new Vector3(width, height, length);
            
            // Remove collider
            Destroy(box.GetComponent<Collider>());
            
            // Apply wireframe material
            if (wireframeMaterial != null)
            {
                box.GetComponent<Renderer>().material = wireframeMaterial;
            }
            
            return box;
        }
        
        /// <summary>
        /// Create wireframe cone for nose
        /// </summary>
        private GameObject CreateWireframeCone(string name, Vector3 position, float radius, float height)
        {
            GameObject cone = new GameObject(name);
            cone.transform.localPosition = position;
            
            MeshFilter meshFilter = cone.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = cone.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = CreateConeMesh(radius, height, 8);
            
            if (wireframeMaterial != null)
            {
                meshRenderer.material = wireframeMaterial;
            }
            
            return cone;
        }
        
        /// <summary>
        /// Generate cone mesh
        /// </summary>
        private Mesh CreateConeMesh(float radius, float height, int segments)
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 6];
            
            // Apex
            vertices[0] = new Vector3(0, 0, height);
            
            // Base center
            vertices[1] = Vector3.zero;
            
            // Base circle
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices[i + 2] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
            }
            
            // Triangles
            int triIndex = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments + 2;
                int current = i + 2;
                
                // Side triangle
                triangles[triIndex++] = 0;
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                
                // Base triangle
                triangles[triIndex++] = 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
            }
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// Create engine glow effects
        /// </summary>
        private void CreateEngineGlows()
        {
            engineGlows = new GameObject[2];
            
            // Left engine
            engineGlows[0] = CreateEngineGlow(new Vector3(-width * 0.8f, 0, -length * 0.5f));
            
            // Right engine
            engineGlows[1] = CreateEngineGlow(new Vector3(width * 0.8f, 0, -length * 0.5f));
        }
        
        /// <summary>
        /// Create single engine glow
        /// </summary>
        private GameObject CreateEngineGlow(Vector3 position)
        {
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "EngineGlow";
            glow.transform.parent = shipMesh.transform;
            glow.transform.localPosition = position;
            glow.transform.localScale = Vector3.one * 0.3f;
            
            // Remove collider
            Destroy(glow.GetComponent<Collider>());
            
            // Create emissive material
            Material glowMat = new Material(wireframeMaterial);
            glowMat.SetColor("_Color", accentColor);
            glowMat.SetFloat("_EmissionIntensity", engineGlowIntensity);
            glow.GetComponent<Renderer>().material = glowMat;
            
            return glow;
        }
        
        /// <summary>
        /// Update engine glow intensity based on speed
        /// </summary>
        public void UpdateEngineGlow(float intensity)
        {
            if (engineGlows == null) return;
            
            foreach (var glow in engineGlows)
            {
                if (glow != null)
                {
                    float scale = 0.3f + (intensity * 0.2f);
                    glow.transform.localScale = Vector3.one * scale;
                }
            }
        }
    }
}
