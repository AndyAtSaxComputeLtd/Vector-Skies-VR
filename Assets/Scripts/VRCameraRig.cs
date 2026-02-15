using UnityEngine;
using UnityEngine.XR;

namespace VectorSkiesVR
{
    /// <summary>
    /// VR camera rig for seated experience
    /// Maintains stable horizon (no roll) for comfort
    /// Supports external chase camera or minimal cockpit view
    /// </summary>
    public class VRCameraRig : MonoBehaviour
    {
        public enum CameraMode
        {
            ExternalChase,  // Default: Ship visible ahead
            Cockpit         // Minimal cockpit frame
        }
        
        [Header("Camera Mode")]
        [SerializeField] private CameraMode cameraMode = CameraMode.ExternalChase;
        
        [Header("External Chase Settings")]
        [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 2f, -8f);
        [SerializeField] private float chaseSmoothSpeed = 5f;
        [SerializeField] private bool lookAtShip = true;
        
        [Header("Cockpit Settings")]
        [SerializeField] private GameObject cockpitFrame;
        [SerializeField] private Vector3 cockpitOffset = new Vector3(0f, 1.5f, 0f);
        
        [Header("VR Comfort")]
        [SerializeField] private bool enforceStableHorizon = true;
        [SerializeField] private float maxPitchAngle = 60f;
        [SerializeField] private float horizonStabilization = 0.9f;
        
        [Header("References")]
        [SerializeField] private Transform shipTransform;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private GameObject shipModel;
        
        private Vector3 currentVelocity;
        private Quaternion targetRotation;
        private static float lastCameraModeLogTime;
        
        void Start()
        {
            VSVRLog.Info("VRCameraRig", "Initializing VR camera rig");
            
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    VSVRLog.Error("VRCameraRig", "Main camera not found!");
                    return;
                }
            }
            
            InitializeCameraMode();
            
            VSVRLog.Info("VRCameraRig", $"Initialized - Mode: {cameraMode} | StableHorizon: {enforceStableHorizon} | MaxPitch: {maxPitchAngle}");
        }
        
        void LateUpdate()
        {
            UpdateCameraPosition();
            UpdateCameraRotation();
        }
        
        /// <summary>
        /// Initialize camera based on selected mode
        /// </summary>
        private void InitializeCameraMode()
        {
            switch (cameraMode)
            {
                case CameraMode.ExternalChase:
                    SetupExternalChase();
                    break;
                    
                case CameraMode.Cockpit:
                    SetupCockpit();
                    break;
            }
        }
        
        /// <summary>
        /// Setup external chase camera
        /// </summary>
        private void SetupExternalChase()
        {
            VSVRLog.Verbose("VRCameraRig", "Setting up external chase camera");
            
            if (shipModel != null)
            {
                shipModel.SetActive(true); // Ship visible
            }
            
            if (cockpitFrame != null)
            {
                cockpitFrame.SetActive(false);
            }
        }
        
        /// <summary>
        /// Setup cockpit camera
        /// </summary>
        private void SetupCockpit()
        {
            VSVRLog.Verbose("VRCameraRig", "Setting up cockpit camera");
            
            if (shipModel != null)
            {
                shipModel.SetActive(false); // Ship not visible in cockpit view
            }
            
            if (cockpitFrame != null)
            {
                cockpitFrame.SetActive(true);
            }
        }
        
        /// <summary>
        /// Update camera position based on mode
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (shipTransform == null || cameraTransform == null) return;
            
            Vector3 targetPosition;
            
            switch (cameraMode)
            {
                case CameraMode.ExternalChase:
                    // Chase camera follows behind ship
                    targetPosition = shipTransform.position + shipTransform.TransformDirection(chaseOffset);
                    cameraTransform.position = Vector3.SmoothDamp(
                        cameraTransform.position,
                        targetPosition,
                        ref currentVelocity,
                        1f / chaseSmoothSpeed
                    );
                    break;
                    
                case CameraMode.Cockpit:
                    // Camera fixed inside cockpit
                    targetPosition = shipTransform.position + shipTransform.TransformDirection(cockpitOffset);
                    cameraTransform.position = targetPosition;
                    break;
            }
        }
        
        /// <summary>
        /// Update camera rotation with MANDATORY stable horizon
        /// </summary>
        private void UpdateCameraRotation()
        {
            if (shipTransform == null || cameraTransform == null) return;
            
            // Get ship rotation
            Vector3 shipEuler = shipTransform.rotation.eulerAngles;
            
            // CRITICAL: Enforce stable horizon - NO ROLL for VR comfort
            float pitch = shipEuler.x;
            float yaw = shipEuler.y;
            float roll = 0f; // ALWAYS zero roll
            
            // Normalize pitch angle
            if (pitch > 180f) pitch -= 360f;
            
            // Limit pitch for comfort
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
            
            // Apply stabilization to reduce extreme movements
            if (enforceStableHorizon)
            {
                pitch *= horizonStabilization;
            }
            
            // Create target rotation with stable horizon
            targetRotation = Quaternion.Euler(pitch, yaw, roll);
            
            // Apply rotation
            switch (cameraMode)
            {
                case CameraMode.ExternalChase:
                    if (lookAtShip)
                    {
                        // Look at ship while maintaining stable horizon
                        Vector3 lookDirection = shipTransform.position - cameraTransform.position;
                        lookDirection.y = 0; // Remove vertical component for stable horizon
                        
                        if (lookDirection.magnitude > 0.1f)
                        {
                            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                            cameraTransform.rotation = Quaternion.Slerp(
                                cameraTransform.rotation,
                                lookRotation,
                                Time.deltaTime * chaseSmoothSpeed
                            );
                        }
                    }
                    else
                    {
                        cameraTransform.rotation = Quaternion.Slerp(
                            cameraTransform.rotation,
                            targetRotation,
                            Time.deltaTime * chaseSmoothSpeed
                        );
                    }
                    break;
                    
                case CameraMode.Cockpit:
                    // In cockpit mode, camera follows ship orientation but with NO ROLL
                    cameraTransform.rotation = targetRotation;
                    break;
            }
        }
        
        /// <summary>
        /// Switch camera mode at runtime
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            if (cameraMode != mode)
            {
                cameraMode = mode;
                InitializeCameraMode();
                VSVRLog.Info("VRCameraRig", $"Camera mode changed to: {mode}");
            }
        }
        
        /// <summary>
        /// Toggle between camera modes
        /// </summary>
        public void ToggleCameraMode()
        {
            CameraMode newMode = (cameraMode == CameraMode.ExternalChase) 
                ? CameraMode.Cockpit 
                : CameraMode.ExternalChase;
            SetCameraMode(newMode);
        }
        
        /// <summary>
        /// Public getters
        /// </summary>
        public CameraMode GetCameraMode() => cameraMode;
        public bool IsStableHorizonEnabled() => enforceStableHorizon;
    }
}
