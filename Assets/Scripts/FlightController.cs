using UnityEngine;
using UnityEngine.InputSystem;

namespace VectorSkiesVR
{
    /// <summary>
    /// FPV Drone flight controller with realistic physics
    /// Mode 2 Controls: Left = Throttle/Yaw, Right = Pitch/Roll
    /// Uses physics-based simulation with gravity, thrust, and mass
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FlightController : MonoBehaviour
    {
        public enum ControlMode
        {
            Mode1, // Left: Pitch/Yaw, Right: Throttle/Roll
            Mode2  // Left: Throttle/Yaw, Right: Pitch/Roll (Standard FPV)
        }
        
        [Header("Control Settings")]
        [SerializeField] private ControlMode controlMode = ControlMode.Mode2;
        
        [Header("Drone Physical Properties")]
        [SerializeField] private float mass = 0.75f; // 750g - typical 5" racing drone
        [SerializeField] private float dragCoefficient = 0.5f; // Air resistance
        [SerializeField] private float angularDrag = 2f; // Rotational resistance
        
        [Header("Motor & Thrust")]
        [SerializeField] private float maxThrust = 25f; // N (Newtons) - ~2500g total thrust (3.3:1 thrust-to-weight)
        [SerializeField] private float minThrust = 0f; // Idle thrust
        [SerializeField] private float throttleResponse = 8f; // How fast throttle responds
        
        [Header("Angular Rates (deg/s)")]
        [SerializeField] private float maxPitchRate = 600f; // Degrees per second
        [SerializeField] private float maxRollRate = 600f;
        [SerializeField] private float maxYawRate = 400f;
        [SerializeField] private float angularAcceleration = 10f; // How fast rotation builds up
        
        [Header("Tilt Limits")]
        [SerializeField] private float maxTiltAngle = 60f; // Maximum pitch/roll angle
        [SerializeField] private bool limitTilt = true; // Enable angle mode (vs acro mode)
        
        [Header("Boost Settings")]
        [SerializeField] private float boostThrustMultiplier = 1.5f;
        [SerializeField] private float boostDuration = 2f;
        [SerializeField] private float boostCooldown = 4f;
        
        [Header("Speed Reference (for UI/Audio)")]
        [SerializeField] private float expectedMinSpeed = 5f; // m/s - hovering speed
        [SerializeField] private float expectedMaxSpeed = 60f; // m/s - full throttle terminal velocity
        
        [Header("Input Actions (XR Controllers)")]
        [SerializeField] private InputActionReference leftStickAction;
        [SerializeField] private InputActionReference rightStickAction;
        [SerializeField] private InputActionReference boostAction;
        
        // Components
        private Rigidbody rb;
        private Transform cameraTransform;
        
        // Current state
        private float currentThrottle;
        private float targetThrottle;
        private Vector3 angularVelocity; // Current rotation rates
        
        // Boost state
        private bool isBoosting;
        private float boostTimer;
        private float boostCooldownTimer;
        
        // Input values
        private Vector2 leftStickInput;
        private Vector2 rightStickInput;
        private bool boostInput;
        
        // Debug
        private float inputLogTimer;
        private float physicsLogTimer;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void Start()
        {
            cameraTransform = Camera.main?.transform;
            
            // Enable input actions
            if (leftStickAction?.action != null) leftStickAction.action.Enable();
            if (rightStickAction?.action != null) rightStickAction.action.Enable();
            if (boostAction?.action != null) boostAction.action.Enable();
            
            Debug.Log($"[FlightController] Initialized - FPV Physics Mode - Mass: {mass}kg, Max Thrust: {maxThrust}N");
            Debug.Log($"[FlightController] Starting Position: {transform.position} | Rotation: {transform.eulerAngles}");
            Debug.Log($"[FlightController] Rigidbody - UseGravity: {rb.useGravity} | IsKinematic: {rb.isKinematic} | Constraints: {rb.constraints}");
            Debug.Log($"[FlightController] Input Actions - Left: {(leftStickAction != null)} | Right: {(rightStickAction != null)} | Boost: {(boostAction != null)}");
        }
        
        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // Configure rigidbody for drone physics
            rb.mass = mass;
            rb.useGravity = true; // Let Unity handle gravity (-9.81 m/s²)
            rb.linearDamping = dragCoefficient;
            rb.angularDamping = angularDrag;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth for VR
            rb.constraints = RigidbodyConstraints.None; // Free flight
        }
        
        void Update()
        {
            ReadInput();
            UpdateBoost();
        }
        
        void FixedUpdate()
        {
            // Physics calculations happen in FixedUpdate for stability
            ApplyThrust();
            ApplyRotation();
            ApplyTiltLimits();
            
            // Rate-limited physics logging
            physicsLogTimer += Time.fixedDeltaTime;
            if (physicsLogTimer >= 1f)
            {
                physicsLogTimer = 0f;
                Debug.Log($"[Physics] Velocity: {rb.linearVelocity.magnitude:F2} m/s | Pos: {transform.position} | Throttle: {currentThrottle:F2} | IsKinematic: {rb.isKinematic}");
            }
        }
        
        /// <summary>
        /// Read controller input from XR controllers
        /// Stick assignments vary based on control mode
        /// </summary>
        private void ReadInput()
        {
            // Read from Input Actions (works with OpenXR on Quest 3)
            if (leftStickAction != null && leftStickAction.action != null)
            {
                leftStickInput = leftStickAction.action.ReadValue<Vector2>();
            }
            
            if (rightStickAction != null && rightStickAction.action != null)
            {
                rightStickInput = rightStickAction.action.ReadValue<Vector2>();
            }
            
            if (boostAction != null && boostAction.action != null)
            {
                boostInput = boostAction.action.ReadValue<float>() > 0.5f;
            }
            
            // Log input action status once per second
            if (inputLogTimer < Time.deltaTime)
            {
                bool leftEnabled = leftStickAction?.action?.enabled ?? false;
                bool rightEnabled = rightStickAction?.action?.enabled ?? false;
                bool boostEnabled = boostAction?.action?.enabled ?? false;
                Debug.Log($"[Input Actions] Left: {(leftStickAction != null ? "Assigned" : "NULL")} (Enabled: {leftEnabled}) | Right: {(rightStickAction != null ? "Assigned" : "NULL")} (Enabled: {rightEnabled}) | Boost: {(boostAction != null ? "Assigned" : "NULL")} (Enabled: {boostEnabled})");
            }
            
            // Debug input for testing without VR or in editor (using keyboard)
            #if UNITY_EDITOR
            var keyboard = Keyboard.current;
            if (keyboard != null && (leftStickAction == null || rightStickAction == null))
            {
                if (controlMode == ControlMode.Mode2)
                {
                    // Mode 2: Left = Throttle/Yaw, Right = Pitch/Roll
                    float yawInput = 0f;
                    float throttleInput = 0f;
                    if (keyboard.aKey.isPressed) yawInput -= 1f;
                    if (keyboard.dKey.isPressed) yawInput += 1f;
                    if (keyboard.leftShiftKey.isPressed) throttleInput = 1f;
                    if (keyboard.leftCtrlKey.isPressed) throttleInput = -1f;
                    leftStickInput = new Vector2(yawInput, throttleInput);
                    
                    // WS = Pitch, QE = Roll
                    float pitchInput = 0f;
                    float rollInput = 0f;
                    if (keyboard.wKey.isPressed) pitchInput += 1f;
                    if (keyboard.sKey.isPressed) pitchInput -= 1f;
                    if (keyboard.qKey.isPressed) rollInput = -1f;
                    if (keyboard.eKey.isPressed) rollInput = 1f;
                    rightStickInput = new Vector2(rollInput, pitchInput);
                }
                
                boostInput = keyboard.spaceKey.isPressed;
            }
            #endif
            
            // Debug input for testing without VR (using new Input System)
            #if UNITY_EDITOR
            if (!Application.isPlaying || !XRSettings.isDeviceActive)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (controlMode == ControlMode.Mode2)
                    {
                        // Mode 2: Left = Throttle/Yaw, Right = Pitch/Roll
                        float yawInput = 0f;
                        float throttleInput = 0f;
                        if (keyboard.aKey.isPressed) yawInput -= 1f;
                        if (keyboard.dKey.isPressed) yawInput += 1f;
                        if (keyboard.leftShiftKey.isPressed) throttleInput = 1f;
                        if (keyboard.leftCtrlKey.isPressed) throttleInput = -1f;
                        leftStickInput = new Vector2(yawInput, throttleInput);
                        
                        // WS = Pitch, QE = Roll
                        float pitchInput = 0f;
                        float rollInput = 0f;
                        if (keyboard.wKey.isPressed) pitchInput += 1f;
                        if (keyboard.sKey.isPressed) pitchInput -= 1f;
                        if (keyboard.qKey.isPressed) rollInput = -1f;
                        if (keyboard.eKey.isPressed) rollInput = 1f;
                        rightStickInput = new Vector2(rollInput, pitchInput);
                    }
                    
                    boostInput = keyboard.spaceKey.isPressed;
                }
            }
            #endif
            
            // Rate-limited input logging (1 per second)
            inputLogTimer += Time.deltaTime;
            if (inputLogTimer >= 1f)
            {
                inputLogTimer = 0f;
                Debug.Log($"[FlightController Input] Left: {leftStickInput} | Right: {rightStickInput} | Boost: {boostInput} | Mode: {controlMode}");
            }
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
        /// Apply thrust force based on throttle input
        /// Thrust is directed upward relative to drone orientation
        /// </summary>
        private void ApplyThrust()
        {
            // Get throttle input (0 to 1 range)
            float throttleInput = (controlMode == ControlMode.Mode1) 
                ? rightStickInput.y 
                : leftStickInput.y;
            
            // Convert from -1,1 to 0,1 range
            float throttle = (throttleInput + 1f) * 0.5f;
            
            // Smooth throttle response
            targetThrottle = throttle;
            currentThrottle = Mathf.Lerp(currentThrottle, targetThrottle, throttleResponse * Time.fixedDeltaTime);
            
            // Calculate thrust force (in Newtons)
            float thrust = Mathf.Lerp(minThrust, maxThrust, currentThrottle);
            
            // Apply boost multiplier
            if (isBoosting)
            {
                thrust *= boostThrustMultiplier;
            }
            
            // Apply upward thrust force relative to drone's orientation
            // This, combined with gravity, creates realistic flight dynamics
            Vector3 thrustForce = transform.up * thrust;
            rb.AddForce(thrustForce, ForceMode.Force);
            
            // Debug log first time throttle changes significantly
            if (Mathf.Abs(throttleInput) > 0.1f && inputLogTimer < 0.5f)
            {
                Debug.Log($"[Thrust] Input: {throttleInput:F2} -> Throttle: {currentThrottle:F2} -> Thrust: {thrust:F2}N | Force: {thrustForce}");
            }
        }
        
        /// <summary>
        /// Apply rotational torques based on stick input
        /// Simulates motor differential thrust for rotation
        /// </summary>
        private void ApplyRotation()
        {
            float pitchInput, yawInput, rollInput;
            
            if (controlMode == ControlMode.Mode2)
            {
                // Mode 2: Right stick = Pitch/Roll, Left stick X = Yaw
                pitchInput = -rightStickInput.y; // Inverted for natural feel
                yawInput = leftStickInput.x;
                rollInput = rightStickInput.x;
            }
            else
            {
                // Mode 1: Left stick = Pitch/Yaw, Right stick X = Roll
                pitchInput = -leftStickInput.y;
                yawInput = leftStickInput.x;
                rollInput = rightStickInput.x;
            }
            
            // Target angular velocities in degrees per second
            Vector3 targetAngularVelocity = new Vector3(
                pitchInput * maxPitchRate,
                yawInput * maxYawRate,
                rollInput * maxRollRate
            );
            
            // Smooth angular acceleration
            angularVelocity = Vector3.Lerp(angularVelocity, targetAngularVelocity, angularAcceleration * Time.fixedDeltaTime);
            
            // Convert degrees/second to radians/second and apply
            Vector3 angularVelocityRad = angularVelocity * Mathf.Deg2Rad;
            rb.angularVelocity = transform.TransformDirection(angularVelocityRad);
        }
        
        /// <summary>
        /// Limit tilt angles for angle mode (stabilized flight)
        /// Disable for acro mode (unlimited flips)
        /// </summary>
        private void ApplyTiltLimits()
        {
            if (!limitTilt) return;
            
            // Get current euler angles
            Vector3 currentRotation = transform.eulerAngles;
            
            // Normalize angles to -180 to 180 range
            float pitch = NormalizeAngle(currentRotation.x);
            float roll = NormalizeAngle(currentRotation.z);
            
            // Limit pitch and roll
            pitch = Mathf.Clamp(pitch, -maxTiltAngle, maxTiltAngle);
            roll = Mathf.Clamp(roll, -maxTiltAngle, maxTiltAngle);
            
            // Apply limited rotation
            transform.rotation = Quaternion.Euler(pitch, currentRotation.y, roll);
        }
        
        /// <summary>
        /// Normalize angle to -180 to 180 range
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            if (angle > 180f) angle -= 360f;
            return angle;
        }
        
        /// <summary>
        /// Public getters for other systems
        /// </summary>
        public float GetCurrentThrottle() => currentThrottle;
        public Vector3 GetVelocity() => rb.linearVelocity;
        public float GetSpeed() => rb.linearVelocity.magnitude;
        public float GetCurrentSpeed() => GetSpeed(); // Legacy compatibility
        public float GetSpeedPercent() => Mathf.Clamp01((GetSpeed() - expectedMinSpeed) / (expectedMaxSpeed - expectedMinSpeed));
        public bool IsBoosting() => isBoosting;
        public float GetBoostCooldownPercent() => boostCooldownTimer > 0 ? 1f - (boostCooldownTimer / boostCooldown) : 1f;    }
}