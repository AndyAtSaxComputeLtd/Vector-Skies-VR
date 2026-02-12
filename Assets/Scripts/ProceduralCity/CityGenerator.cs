using UnityEngine;
using System.Collections.Generic;

namespace VectorSkiesVR.ProceduralCity
{
    /// <summary>
    /// Procedural city generator with chunk-based streaming
    /// Generates infinite neon wireframe towers ahead of player
    /// Cleans up chunks behind player for performance
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int chunksAhead = 5;
        [SerializeField] private int chunksBehind = 2;
        [SerializeField] private float chunkSize = 100f;
        [SerializeField] private int towersPerChunk = 20;
        
        [Header("Tower Settings")]
        [SerializeField] private float minTowerWidth = 8f;
        [SerializeField] private float maxTowerWidth = 20f;
        [SerializeField] private float minTowerHeight = 30f;
        [SerializeField] private float maxTowerHeight = 100f;
        [SerializeField] private float minSpacing = 15f;
        
        [Header("Color Distribution")]
        [Range(0f, 1f)]
        [SerializeField] private float cyanProbability = 0.7f; // 70% cyan
        [Range(0f, 1f)]
        [SerializeField] private float purpleProbability = 0.2f; // 20% purple
        // Remaining is red (hazard)
        
        [Header("Materials")]
        [SerializeField] private Material wireframeMaterial;
        
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        
        // Chunk management
        private Dictionary<int, CityChunk> activeChunks = new Dictionary<int, CityChunk>();
        private int currentChunkIndex;
        private int lastGeneratedChunk = -999;
        
        // Prefab pool
        private GameObject towerPrefab;
        
        void Start()
        {
            if (playerTransform == null)
            {
                playerTransform = Camera.main?.transform;
            }
            
            CreateTowerPrefab();
            GenerateInitialChunks();
            
            Debug.Log($"[CityGenerator] Started - Chunk size: {chunkSize}m");
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
            towerPrefab = new GameObject("TowerPrefab");
            towerPrefab.AddComponent<WireframeTower>();
            towerPrefab.SetActive(false);
        }
        
        /// <summary>
        /// Generate initial chunks around player
        /// </summary>
        private void GenerateInitialChunks()
        {
            currentChunkIndex = GetChunkIndex(playerTransform.position.z);
            
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
        /// Generate a new chunk at specified index
        /// </summary>
        private void GenerateChunk(int chunkIndex)
        {
            if (activeChunks.ContainsKey(chunkIndex)) return;
            
            CityChunk newChunk = new CityChunk();
            newChunk.chunkIndex = chunkIndex;
            newChunk.chunkObject = new GameObject($"Chunk_{chunkIndex}");
            newChunk.chunkObject.transform.parent = transform;
            newChunk.towers = new List<GameObject>();
            
            // Calculate chunk world position
            float chunkZ = chunkIndex * chunkSize;
            newChunk.chunkObject.transform.position = new Vector3(0, 0, chunkZ);
            
            // Generate towers in this chunk
            Random.InitState(chunkIndex * 12345); // Deterministic randomness per chunk
            
            for (int i = 0; i < towersPerChunk; i++)
            {
                GenerateTowerInChunk(newChunk, chunkZ);
            }
            
            activeChunks.Add(chunkIndex, newChunk);
            
            Debug.Log($"[CityGenerator] Generated chunk {chunkIndex} at Z={chunkZ}");
        }
        
        /// <summary>
        /// Generate a single tower within a chunk
        /// </summary>
        private void GenerateTowerInChunk(CityChunk chunk, float chunkZ)
        {
            // Random position within chunk
            float x = Random.Range(-chunkSize * 0.4f, chunkSize * 0.4f);
            float z = chunkZ + Random.Range(0, chunkSize);
            Vector3 position = new Vector3(x, 0, z);
            
            // Random dimensions
            float width = Random.Range(minTowerWidth, maxTowerWidth);
            float height = Random.Range(minTowerHeight, maxTowerHeight);
            float depth = Random.Range(minTowerWidth, maxTowerWidth);
            
            // Determine color type
            WireframeTower.TowerColor colorType = GetRandomTowerColor();
            
            // Create tower
            GameObject towerObj = Instantiate(towerPrefab, position, Quaternion.identity, chunk.chunkObject.transform);
            towerObj.SetActive(true);
            towerObj.name = $"Tower_{colorType}_{chunk.towers.Count}";
            
            WireframeTower tower = towerObj.GetComponent<WireframeTower>();
            tower.Initialize(width, height, depth, colorType);
            tower.SetMaterial(wireframeMaterial);
            
            chunk.towers.Add(towerObj);
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
            
            // Cleanup chunk object
            Destroy(chunk.chunkObject);
            
            activeChunks.Remove(chunkIndex);
            
            Debug.Log($"[CityGenerator] Removed chunk {chunkIndex}");
        }
        
        /// <summary>
        /// Chunk data structure
        /// </summary>
        private class CityChunk
        {
            public int chunkIndex;
            public GameObject chunkObject;
            public List<GameObject> towers;
        }
    }
}
