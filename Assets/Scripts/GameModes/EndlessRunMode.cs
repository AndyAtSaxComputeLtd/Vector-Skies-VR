using UnityEngine;

namespace VectorSkiesVR.GameModes
{
    /// <summary>
    /// Endless run mode - speed increases gradually, navigate through towers
    /// </summary>
    public class EndlessRunMode : MonoBehaviour
    {
        [Header("Speed Progression")]
        [SerializeField] private float startSpeed = 20f;
        [SerializeField] private float maxSpeed = 80f;
        [SerializeField] private float speedIncreaseRate = 1f; // Per minute
        [SerializeField] private float speedIncreaseInterval = 10f; // Seconds
        
        [Header("Scoring")]
        [SerializeField] private float distanceMultiplier = 1f;
        [SerializeField] private int nearMissBonus = 50;
        [SerializeField] private float nearMissDistance = 3f;
        
        [Header("References")]
        [SerializeField] private FlightController flightController;
        
        // Game state
        private float gameTime;
        private float distanceTraveled;
        private int score;
        private int nearMisses;
        private bool isGameActive;
        private float lastSpeedIncreaseTime;
        
        void Start()
        {
            if (flightController == null)
            {
                flightController = FindObjectOfType<FlightController>();
            }
            
            StartGame();
        }
        
        void Update()
        {
            if (!isGameActive) return;
            
            gameTime += Time.deltaTime;
            
            // Update distance
            if (flightController != null)
            {
                float speed = flightController.GetCurrentSpeed();
                distanceTraveled += speed * Time.deltaTime;
                
                // Update score based on distance
                score = Mathf.FloorToInt(distanceTraveled * distanceMultiplier);
            }
            
            // Gradually increase difficulty
            if (gameTime - lastSpeedIncreaseTime >= speedIncreaseInterval)
            {
                IncreaseSpeed();
                lastSpeedIncreaseTime = gameTime;
            }
        }
        
        /// <summary>
        /// Start the endless run
        /// </summary>
        public void StartGame()
        {
            isGameActive = true;
            gameTime = 0f;
            distanceTraveled = 0f;
            score = 0;
            nearMisses = 0;
            lastSpeedIncreaseTime = 0f;
            
            Debug.Log("[EndlessRunMode] Game started!");
        }
        
        /// <summary>
        /// Increase speed over time
        /// </summary>
        private void IncreaseSpeed()
        {
            // TODO: Communicate speed increase to FlightController
            Debug.Log($"[EndlessRunMode] Speed increased at {gameTime:F1}s");
        }
        
        /// <summary>
        /// Handle near miss detection
        /// </summary>
        public void RegisterNearMiss(GameObject tower)
        {
            nearMisses++;
            score += nearMissBonus;
            
            Debug.Log($"[EndlessRunMode] Near miss! Bonus: +{nearMissBonus}");
            
            // TODO: Trigger near miss VFX/audio
        }
        
        /// <summary>
        /// Handle collision
        /// </summary>
        public void OnCollision(GameObject obstacle)
        {
            GameOver();
        }
        
        /// <summary>
        /// End the game
        /// </summary>
        private void GameOver()
        {
            isGameActive = false;
            
            Debug.Log($"[EndlessRunMode] Game Over! Final Score: {score}, Distance: {distanceTraveled:F1}m, Time: {gameTime:F1}s");
            
            // TODO: Show game over UI
        }
        
        /// <summary>
        /// Public getters
        /// </summary>
        public int GetScore() => score;
        public float GetDistance() => distanceTraveled;
        public float GetGameTime() => gameTime;
        public int GetNearMisses() => nearMisses;
        public bool IsActive() => isGameActive;
    }
}
