using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

namespace VectorSkiesVR
{
    /// <summary>
    /// Seated VR flight controller for Vector Skies VR
    /// Controls: Left stick = Pitch/Yaw, Right stick = Throttle/Roll
    /// NO CAMERA ROLL - stable horizon enforced for VR comfort
    /// </summary>
    public class FlightController : MonoBehaviour
    {
        [Header("Flight Physics")]
        [SerializeField] private float baseSpeed = 20f;
        [SerializeField] private float maxSpeed = 60f;
        [SerializeField] private float minSpeed = 5f;
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float deceleration = 3f;
        
        [Header("Rotation Settings")]
        [SerializeField] private float pitchSpeed = 40f;
        [SerializeField] private float yawSpeed = 50f;
        [SerializeField] private float rollSpeed = 20f; // Cosmetic only
        [SerializeField] private float maxRollAngle = 25f; // Limited banking
        
        [Header("Boost Settings")]
        [SerializeField] private float boostMultiplier = 1.8f;
        [SerializeField] private float boostDuration = 2f;
        [SerializeField] private float boostCooldown = 4f;
        
        [Header("Smoothing")]
        [SerializeField] private float accelerationSmoothing = 2f;
        [SerializeField] private float rotationSmoothing = 3f;
        [SerializeField] private float rollReturnSpeed = 4f;
        
        // Current state
        private float currentSpeed;
        private float targetSpeed;
        private float currentPitch;
        private float currentYaw;
        private float currentRoll;
        private float targetRoll;
        
        // Boost state
        private bool isBoosting;
        private float boostTimer;
        private float boostCooldownTimer;
        
        // Input values
        private Vector2 leftStickInput;
        private Vector2 rightStickInput;
        private bool boostInput;
        
        // Transform references
        private Transform shipTransform;
        private Transform cameraTransform;
        
        void Start()
        {
            shipTransform = transform;
            cameraTransform = Camera.main?.transform;
            
            currentSpeed = baseSpeed;
            targetSpeed = baseSpeed;
            
            Debug.Log("[FlightController] Initialized - Seated VR Mode");
        }
        
        void Update()
        {
            ReadInput();
            UpdateBoost();
            UpdateSpeed();
            UpdateRotation();
            ApplyMovement();
        }
        
        /// <summary>
        /// Read controller input from XR controllers
        /// </summary>
        private void ReadInput()
        {
            // Read from XR Input (Quest controllers)
            InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            
            // Left stick: Pitch (Y) and Yaw (X)
            if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
            {
                leftStickInput = leftStick;
            }
            
            // Right stick: Throttle (Y) and Roll (X)
            if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick))
            {
                rightStickInput = rightStick;
            }
            
            // Right trigger: Boost
            if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed))
            {
                boostInput = triggerPressed;
            }
            
            // Debug input for testing without VR
            #if UNITY_EDITOR
            if (!Application.isPlaying || !XRSettings.isDeviceActive)
            {
                leftStickInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                rightStickInput = new Vector2(Input.GetKey(KeyCode.Q) ? -1 : Input.GetKey(KeyCode.E) ? 1 : 0,
                                             Input.GetKey(KeyCode.LeftShift) ? 1 : Input.GetKey(KeyCode.LeftControl) ? -1 : 0);
                boostInput = Input.GetKey(KeyCode.Space);
            }
            #endif
        }
        
        /// <summary>
        /// Handle boost mechanic
        /// </summary>
        private void UpdateBoost()
        {
            // Update cooldown
            if (boostCooldownTimer > 0)
            {
                boostCooldownTimer -= Time.deltaTime;
            }
            
            // Start boost
            if (boostInput && !isBoosting && boostCooldownTimer <= 0)
            {
                isBoosting = true;
                boostTimer = boostDuration;
                Debug.Log("[FlightController] Boost activated!");
                
                // TODO: Trigger boost VFX and audio
                // TODO: Trigger controller haptics
            }
            
            // Update boost
            if (isBoosting)
            {
                boostTimer -= Time.deltaTime;
                if (boostTimer <= 0)
                {
                    isBoosting = false;
                    boostCooldownTimer = boostCooldown;
                    Debug.Log("[FlightController] Boost ended - cooldown started");
                }
            }
        }
        
        /// <summary>
        /// Update speed based on throttle input and boost
        /// </summary>
        private void UpdateSpeed()
        {
            // Throttle control from right stick Y
            float throttleInput = rightStickInput.y;
            
            // Calculate target speed
            if (throttleInput > 0)
            {
                targetSpeed = Mathf.Lerp(baseSpeed, maxSpeed, throttleInput);
            }
            else if (throttleInput < 0)
            {
                targetSpeed = Mathf.Lerp(baseSpeed, minSpeed, -throttleInput);
            }
            else
            {
                targetSpeed = baseSpeed;
            }
            
            // Apply boost multiplier
            float finalTargetSpeed = isBoosting ? targetSpeed * boostMultiplier : targetSpeed;
            
            // Smooth acceleration
            float accelRate = (finalTargetSpeed > currentSpeed) ? acceleration : deceleration;
            currentSpeed = Mathf.Lerp(currentSpeed, finalTargetSpeed, accelRate * accelerationSmoothing * Time.deltaTime);
        }
        
        /// <summary>
        /// Update rotation with MANDATORY stable horizon (no camera roll)
        /// </summary>
        private void UpdateRotation()
        {
            // Pitch and Yaw from left stick
            float pitchInput = -leftStickInput.y; // Inverted for natural feel
            float yawInput = leftStickInput.x;
            
            // Roll from right stick (COSMETIC ONLY - visual banking effect)
            float rollInput = rightStickInput.x;
            targetRoll = rollInput * maxRollAngle;
            
            // Apply rotation changes with smoothing
            currentPitch += pitchInput * pitchSpeed * Time.deltaTime;
            currentYaw += yawInput * yawSpeed * Time.deltaTime;
            
            // Smooth roll banking (purely visual)
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, rollReturnSpeed * Time.deltaTime);
            
            // CRITICAL: Apply rotation to ship only, NEVER to camera parent
            // This keeps the horizon stable for VR comfort
            if (shipTransform != null)
            {
                // Ship can have cosmetic roll for visual feedback
                Quaternion shipRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);
                shipTransform.rotation = Quaternion.Slerp(shipTransform.rotation, shipRotation, rotationSmoothing * Time.deltaTime);
            }
            
            // Ensure camera maintains stable horizon (no roll on camera parent)
            if (cameraTransform != null && cameraTransform.parent != shipTransform)
            {
                // Camera follows pitch and yaw but NEVER rolls
                Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                cameraTransform.parent.rotation = Quaternion.Slerp(cameraTransform.parent.rotation, cameraRotation, rotationSmoothing * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Apply forward movement
        /// </summary>
        private void ApplyMovement()
        {
            // Move forward based on current speed
            Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
            transform.position += movement;
        }
        
        /// <summary>
        /// Public getters for other systems
        /// </summary>
        public float GetCurrentSpeed() => currentSpeed;
        public float GetSpeedPercent() => (currentSpeed - minSpeed) / (maxSpeed - minSpeed);
        public bool IsBoosting() => isBoosting;
        public float GetBoostCooldownPercent() => 1f - (boostCooldownTimer / boostCooldown);
    }
}
