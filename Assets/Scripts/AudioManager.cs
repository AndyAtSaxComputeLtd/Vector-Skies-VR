using UnityEngine;
using System.Collections.Generic;

namespace VectorSkiesVR
{
    /// <summary>
    /// Spatial audio manager for VR environment
    /// Handles engine sounds, wind, boost effects, and collision audio
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource engineSource;
        [SerializeField] private AudioSource windSource;
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource musicSource;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip engineHumClip;
        [SerializeField] private AudioClip windLoopClip;
        [SerializeField] private AudioClip boostSurgeClip;
        [SerializeField] private AudioClip collisionZapClip;
        [SerializeField] private AudioClip ringPassClip;
        [SerializeField] private AudioClip synthwaveMusicClip;
        
        [Header("Engine Settings")]
        [SerializeField] private float minEnginePitch = 0.8f;
        [SerializeField] private float maxEnginePitch = 1.5f;
        [SerializeField] private float enginePitchSmoothing = 2f;
        
        [Header("Wind Settings")]
        [SerializeField] private float minWindVolume = 0.1f;
        [SerializeField] private float maxWindVolume = 0.6f;
        [SerializeField] private float windVolumeSmoothing = 1.5f;
        
        [Header("Music Settings")]
        [SerializeField] private float musicVolume = 0.3f;
        [SerializeField] private bool playMusicOnStart = true;
        
        [Header("Spatial Audio")]
        [SerializeField] private float spatialBlend = 1f; // Full 3D
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 100f;
        
        [Header("References")]
        [SerializeField] private FlightController flightController;
        
        private float currentEnginePitch;
        private float currentWindVolume;
        private static float lastAudioUpdateLogTime;
        
        void Start()
        {
            VSVRLog.Info("AudioManager", "Initializing audio system");
            
            InitializeAudioSources();
            
            if (flightController == null)
            {
                flightController = FindObjectOfType<FlightController>();
                if (flightController == null)
                {
                    VSVRLog.Warning("AudioManager", "FlightController not found - audio will not sync with speed");
                }
            }
            
            StartEngineSound();
            
            if (playMusicOnStart)
            {
                PlayMusic();
            }
            
            VSVRLog.Info("AudioManager", $"Initialized | SpatialBlend: {spatialBlend} | MusicVolume: {musicVolume}");
        }
        
        void Update()
        {
            UpdateEngineSound();
            UpdateWindSound();
        }
        
        /// <summary>
        /// Initialize audio sources with proper settings
        /// </summary>
        private void InitializeAudioSources()
        {
            VSVRLog.Verbose("AudioManager", "Creating audio sources");
            
            // Create audio sources if not assigned
            if (engineSource == null)
            {
                GameObject engineObj = new GameObject("EngineAudio");
                engineObj.transform.parent = transform;
                engineSource = engineObj.AddComponent<AudioSource>();
            }
            
            if (windSource == null)
            {
                GameObject windObj = new GameObject("WindAudio");
                windObj.transform.parent = transform;
                windSource = windObj.AddComponent<AudioSource>();
            }
            
            if (effectsSource == null)
            {
                GameObject effectsObj = new GameObject("EffectsAudio");
                effectsObj.transform.parent = transform;
                effectsSource = effectsObj.AddComponent<AudioSource>();
            }
            
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicAudio");
                musicObj.transform.parent = transform;
                musicSource = musicObj.AddComponent<AudioSource>();
            }
            
            // Configure spatial settings for engine and wind
            ConfigureSpatialSource(engineSource);
            ConfigureSpatialSource(windSource);
            ConfigureSpatialSource(effectsSource);
            
            // Music is 2D
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            
            VSVRLog.Verbose("AudioManager", "Audio sources configured");
        }
        
        /// <summary>
        /// Configure audio source for spatial 3D sound
        /// </summary>
        private void ConfigureSpatialSource(AudioSource source)
        {
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f; // Disable doppler for VR comfort
        }
        
        /// <summary>
        /// Start engine sound loop
        /// </summary>
        private void StartEngineSound()
        {
            if (engineSource != null && engineHumClip != null)
            {
                engineSource.clip = engineHumClip;
                engineSource.loop = true;
                engineSource.volume = 0.5f;
                engineSource.Play();
                
                currentEnginePitch = minEnginePitch;
                engineSource.pitch = currentEnginePitch;
                
                VSVRLog.Verbose("AudioManager", "Engine sound started");
            }
            else
            {
                VSVRLog.Warning("AudioManager", "Engine sound clip not assigned");
            }
            
            if (windSource != null && windLoopClip != null)
            {
                windSource.clip = windLoopClip;
                windSource.loop = true;
                windSource.volume = minWindVolume;
                windSource.Play();
                
                VSVRLog.Verbose("AudioManager", "Wind sound started");
            }
        }
        
        /// <summary>
        /// Update engine sound based on speed
        /// </summary>
        private void UpdateEngineSound()
        {
            if (flightController == null || engineSource == null) return;
            
            float speedPercent = flightController.GetSpeedPercent();
            
            // Update pitch based on speed
            float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedPercent);
            currentEnginePitch = Mathf.Lerp(currentEnginePitch, targetPitch, enginePitchSmoothing * Time.deltaTime);
            engineSource.pitch = currentEnginePitch;
            
            // Boost modifier
            if (flightController.IsBoosting())
            {
                engineSource.pitch *= 1.2f;
            }
            
            // Rate-limited logging
            if (Time.time - lastAudioUpdateLogTime >= 1f)
            {
                VSVRLog.Verbose("AudioManager", $"Engine pitch: {currentEnginePitch:F2} | Speed: {speedPercent * 100:F0}%");
                lastAudioUpdateLogTime = Time.time;
            }
        }
        
        /// <summary>
        /// Update wind sound based on speed
        /// </summary>
        private void UpdateWindSound()
        {
            if (flightController == null || windSource == null) return;
            
            float speedPercent = flightController.GetSpeedPercent();
            
            // Update volume based on speed
            float targetVolume = Mathf.Lerp(minWindVolume, maxWindVolume, speedPercent);
            currentWindVolume = Mathf.Lerp(currentWindVolume, targetVolume, windVolumeSmoothing * Time.deltaTime);
            windSource.volume = currentWindVolume;
        }
        
        /// <summary>
        /// Play boost surge sound with haptics
        /// </summary>
        public void PlayBoostSound()
        {
            if (effectsSource != null && boostSurgeClip != null)
            {
                effectsSource.PlayOneShot(boostSurgeClip, 0.7f);
                VSVRLog.Info("AudioManager", "Boost sound played");
                
                // TODO: Trigger controller haptic feedback
            }
            else
            {
                VSVRLog.Warning("AudioManager", "Boost sound clip not assigned");
            }
        }
        
        /// <summary>
        /// Play collision zap sound
        /// </summary>
        public void PlayCollisionSound(Vector3 position)
        {
            if (effectsSource != null && collisionZapClip != null)
            {
                effectsSource.transform.position = position;
                effectsSource.PlayOneShot(collisionZapClip, 1f);
                VSVRLog.Info("AudioManager", $"Collision sound played at {position}");
            }
            else
            {
                VSVRLog.Warning("AudioManager", "Collision sound clip not assigned");
            }
        }
        
        /// <summary>
        /// Play ring pass sound
        /// </summary>
        public void PlayRingPassSound(Vector3 position)
        {
            if (effectsSource != null && ringPassClip != null)
            {
                effectsSource.transform.position = position;
                effectsSource.PlayOneShot(ringPassClip, 0.8f);
                VSVRLog.Verbose("AudioManager", "Ring pass sound played");
            }
        }
        
        /// <summary>
        /// Start background synthwave music
        /// </summary>
        public void PlayMusic()
        {
            if (musicSource != null && synthwaveMusicClip != null)
            {
                musicSource.clip = synthwaveMusicClip;
                musicSource.volume = musicVolume;
                musicSource.Play();
                VSVRLog.Info("AudioManager", "Synthwave music started");
            }
            else
            {
                VSVRLog.Warning("AudioManager", "Music clip not assigned");
            }
        }
        
        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
                VSVRLog.Info("AudioManager", "Music stopped");
            }
        }
        
        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
            VSVRLog.Verbose("AudioManager", $"Music volume set to {musicVolume:F2}");
        }
        
        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            VSVRLog.Info("AudioManager", $"Master volume set to {AudioListener.volume:F2}");
        }
    }
}
