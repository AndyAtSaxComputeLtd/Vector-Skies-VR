using UnityEngine;
using System.Collections.Generic;

namespace VectorSkiesVR.GameModes
{
    /// <summary>
    /// Ring navigation mode - fly through neon gates for points
    /// </summary>
    public class RingNavigationMode : MonoBehaviour
    {
        [Header("Ring Settings")]
        [SerializeField] private float ringSpacing = 50f;
        [SerializeField] private float ringRadius = 10f;
        [SerializeField] private int ringsAhead = 5;
        [SerializeField] private float ringThickness = 0.5f;
        
        [Header("Scoring")]
        [SerializeField] private int ringScore = 100;
        [SerializeField] private int perfectRingBonus = 50; // Center hit
        [SerializeField] private float perfectRingDistance = 2f;
        [SerializeField] private float timeBonus = 10f; // Points per second remaining
        [SerializeField] private float timeLimitPerRing = 15f;
        
        [Header("Colors")]
        [SerializeField] private Color activeRingColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color upcomingRingColor = new Color(0.5f, 0.5f, 1f, 0.5f); // Dim
        
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Material ringMaterial;
        
        // Game state
        private List<NavigationRing> rings = new List<NavigationRing>();
        private int currentRingIndex = 0;
        private int score = 0;
        private int ringsPassed = 0;
        private float timeRemaining;
        private bool isGameActive;
        
        // Rate limiting for logs
        private static float lastUpdateLogTime;
        
        void Start()
        {
            VSVRLog.Info("RingNavigationMode", "Initializing ring navigation mode");
            
            if (playerTransform == null)
            {
                playerTransform = Camera.main?.transform;
                if (playerTransform == null)
                {
                    VSVRLog.Error("RingNavigationMode", "Player transform/camera not found!");
                    return;
                }
            }
            
            GenerateInitialRings();
            StartGame();
            
            VSVRLog.Info("RingNavigationMode", $"Initialized | RingSpacing: {ringSpacing}m | RingsAhead: {ringsAhead} | TimeLimit: {timeLimitPerRing}s");
        }
        
        void Update()
        {
            if (!isGameActive) return;
            
            // Update timer
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                VSVRLog.Warning("RingNavigationMode", "Time ran out!");
                GameOver();
                return;
            }
            
            // Rate-limited status logging
            if (Time.time - lastUpdateLogTime >= 5f)
            {
                VSVRLog.Verbose("RingNavigationMode", $"Score: {score} | Rings: {ringsPassed} | Time: {timeRemaining:F1}s | ActiveRing: {currentRingIndex}");
                lastUpdateLogTime = Time.time;
            }
            
            CheckRingProgress();
            UpdateRings();
        }
        
        /// <summary>
        /// Generate initial set of rings
        /// </summary>
        private void GenerateInitialRings()
        {
            VSVRLog.Verbose("RingNavigationMode", $"Generating initial {ringsAhead} rings");
            for (int i = 0; i < ringsAhead; i++)
            {
                CreateRing(i);
            }
        }
        
        /// <summary>
        /// Create a single navigation ring
        /// </summary>
        private void CreateRing(int index)
        {
            NavigationRing ring = new NavigationRing();
            ring.index = index;
            ring.position = new Vector3(
                Random.Range(-20f, 20f), // Random X offset
                Random.Range(5f, 15f),   // Random height
                index * ringSpacing
            );
            
            ring.gameObject = CreateRingMesh(ring.position);
            ring.isPassed = false;
            ring.isActive = (index == 0);
            
            rings.Add(ring);
            
            VSVRLog.Verbose("RingNavigationMode", $"Ring {index} created at {ring.position}");
            
            UpdateRingAppearance(ring);
        }
        
        /// <summary>
        /// Create ring mesh geometry
        /// </summary>
        private GameObject CreateRingMesh(Vector3 position)
        {
            GameObject ringObj = new GameObject($"NavigationRing_{rings.Count}");
            ringObj.transform.position = position;
            
            // Create torus mesh for ring
            MeshFilter meshFilter = ringObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = ringObj.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = GenerateTorusMesh(ringRadius, ringThickness, 32, 16);
            meshRenderer.material = ringMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            
            // Add trigger collider
            SphereCollider collider = ringObj.AddComponent<SphereCollider>();
            collider.radius = ringRadius;
            collider.isTrigger = true;
            
            // Add ring component for detection
            RingTrigger trigger = ringObj.AddComponent<RingTrigger>();
            trigger.navigationMode = this;
            
            return ringObj;
        }
        
        /// <summary>
        /// Generate torus mesh for ring
        /// </summary>
        private Mesh GenerateTorusMesh(float radius, float thickness, int radialSegments, int tubularSegments)
        {
            Mesh mesh = new Mesh();
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            
            for (int i = 0; i <= radialSegments; i++)
            {
                float u = (float)i / radialSegments * Mathf.PI * 2f;
                
                for (int j = 0; j <= tubularSegments; j++)
                {
                    float v = (float)j / tubularSegments * Mathf.PI * 2f;
                    
                    float x = (radius + thickness * Mathf.Cos(v)) * Mathf.Cos(u);
                    float y = (radius + thickness * Mathf.Cos(v)) * Mathf.Sin(u);
                    float z = thickness * Mathf.Sin(v);
                    
                    vertices.Add(new Vector3(x, z, y)); // Rotate to face forward
                    colors.Add(activeRingColor);
                }
            }
            
            // Generate triangles
            for (int i = 0; i < radialSegments; i++)
            {
                for (int j = 0; j < tubularSegments; j++)
                {
                    int a = i * (tubularSegments + 1) + j;
                    int b = (i + 1) * (tubularSegments + 1) + j;
                    int c = (i + 1) * (tubularSegments + 1) + (j + 1);
                    int d = i * (tubularSegments + 1) + (j + 1);
                    
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(d);
                    
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        /// <summary>
        /// Check if player has passed through rings
        /// </summary>
        private void CheckRingProgress()
        {
            if (currentRingIndex >= rings.Count) return;
            
            NavigationRing currentRing = rings[currentRingIndex];
            
            if (playerTransform.position.z > currentRing.position.z && !currentRing.isPassed)
            {
                // Player missed the ring
                MissedRing(currentRing);
            }
        }
        
        /// <summary>
        /// Called when player successfully passes through ring
        /// </summary>
        public void RingPassed(GameObject ringObject)
        {
            NavigationRing ring = rings.Find(r => r.gameObject == ringObject);
            if (ring == null || ring.isPassed) return;
            
            ring.isPassed = true;
            ringsPassed++;
            
            // Calculate score
            float distanceFromCenter = Vector3.Distance(
                playerTransform.position,
                ring.position
            );
            
            int points = ringScore;
            if (distanceFromCenter < perfectRingDistance)
            {
                points += perfectRingBonus;
                VSVRLog.Info("RingNavigationMode", "Perfect ring pass!");
            }
            
            // Add time bonus
            int timeBonusPoints = Mathf.FloorToInt(timeRemaining * timeBonus);
            points += timeBonusPoints;
            
            score += points;
            timeRemaining += timeLimitPerRing; // Add time for next ring
            
            VSVRLog.Info("RingNavigationMode", $"Ring passed! Points: +{points} | Total Score: {score} | Distance: {distanceFromCenter:F1}m");
            
            // Move to next ring
            currentRingIndex++;
            if (currentRingIndex < rings.Count)
            {
                rings[currentRingIndex].isActive = true;
                UpdateRingAppearance(rings[currentRingIndex]);
            }
            
            // Generate new ring ahead
            CreateRing(rings[rings.Count - 1].index + 1);
            
            // Cleanup old rings
            if (rings.Count > ringsAhead * 2)
            {
                Destroy(rings[0].gameObject);
                rings.RemoveAt(0);
                currentRingIndex--;
            }
        }
        
        /// <summary>
        /// Called when player misses a ring
        /// </summary>
        private void MissedRing(NavigationRing ring)
        {
            ring.isPassed = true;
            VSVRLog.Warning("RingNavigationMode", $"Ring {ring.index} missed! -5s penalty");
            
            // Penalty: lose some time
            timeRemaining -= 5f;
            
            currentRingIndex++;
            if (currentRingIndex < rings.Count)
            {
                rings[currentRingIndex].isActive = true;
                UpdateRingAppearance(rings[currentRingIndex]);
            }
        }
        
        /// <summary>
        /// Update ring visual appearance
        /// </summary>
        private void UpdateRingAppearance(NavigationRing ring)
        {
            MeshRenderer renderer = ring.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetColor("_Color", ring.isActive ? activeRingColor : upcomingRingColor);
                renderer.SetPropertyBlock(props);
            }
        }
        
        /// <summary>
        /// Update ring positions and states
        /// </summary>
        private void UpdateRings()
        {
            foreach (var ring in rings)
            {
                if (ring.isActive && !ring.isPassed)
                {
                    // Pulse active ring
                    float pulse = Mathf.Sin(Time.time * 3f) * 0.2f + 1f;
                    ring.gameObject.transform.localScale = Vector3.one * pulse;
                }
            }
        }
        
        /// <summary>
        /// Start the game
        /// </summary>
        public void StartGame()
        {
            isGameActive = true;
            score = 0;
            ringsPassed = 0;
            currentRingIndex = 0;
            timeRemaining = timeLimitPerRing;
            
            VSVRLog.Info("RingNavigationMode", "Game started!");
        }
        
        /// <summary>
        /// End the game
        /// </summary>
        private void GameOver()
        {
            isGameActive = false;
            VSVRLog.Info("RingNavigationMode", $"Game Over! Final Score: {score} | Rings Passed: {ringsPassed} | Time: {timeRemaining:F1}s");
        }
        
        /// <summary>
        /// Public getters
        /// </summary>
        public int GetScore() => score;
        public int GetRingsPassed() => ringsPassed;
        public float GetTimeRemaining() => timeRemaining;
        public bool IsActive() => isGameActive;
        
        /// <summary>
        /// Navigation ring data
        /// </summary>
        private class NavigationRing
        {
            public int index;
            public Vector3 position;
            public GameObject gameObject;
            public bool isPassed;
            public bool isActive;
        }
    }
    
    /// <summary>
    /// Trigger component for ring collision detection
    /// </summary>
    public class RingTrigger : MonoBehaviour
    {
        public RingNavigationMode navigationMode;
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                VSVRLog.Verbose("RingTrigger", "Player entered ring trigger");
                navigationMode?.RingPassed(gameObject);
            }
        }
    }
}
