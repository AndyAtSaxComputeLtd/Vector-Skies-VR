# Vector Skies VR – Neon Void Flight

A seated VR flight experience for Meta Quest 3 built in Unity 6.3. Fly through an infinite procedurally-generated neon wireframe city suspended in pure black void.

![Platform](https://img.shields.io/badge/Platform-Meta%20Quest%203-blue)
![Unity](https://img.shields.io/badge/Unity-6.3-black)
![XR](https://img.shields.io/badge/XR-OpenXR-green)

## 🎮 Game Features

- **Seated VR Only** - Comfortable arcade-style flight controls
- **Stable Horizon** - No camera roll for maximum VR comfort
- **Procedural City** - Infinite neon wireframe towers (cyan/purple/red)
- **90Hz Performance** - Locked framerate optimized for Quest 3
- **Two Game Modes**:
  - **Endless Run** - Speed increases, weave through towers
  - **Ring Navigation** - Fly through gates for time-based scoring
- **Spatial Audio** - 3D engine hum, wind, and collision effects

## 🎯 Control Scheme

**Seated experience only - Quest 3 Controllers required**

| Input | Action |
|-------|--------|
| **Left Stick Y** | Pitch up/down |
| **Left Stick X** | Yaw left/right |
| **Right Stick Y** | Throttle control |
| **Right Stick X** | Cosmetic roll (banking) |
| **Right Trigger** | Boost |
| **A Button** | Pause |
| **B Button** | Snap turn (optional) |

## 🛠️ Unity 6.3 Setup Instructions

### Prerequisites

1. **Unity 6.3 LTS** or newer
2. **Android Build Support** module installed
3. **Meta Quest 3** headset
4. **Oculus/Meta Developer Account** for device deployment

### Initial Project Setup

1. **Clone this repository**:
   ```bash
   git clone https://github.com/AndyAtSaxComputeLtd/Vector-Skies-VR.git
   cd Vector-Skies-VR
   ```

2. **Open in Unity Hub**:
   - Click "Add" → Select the `Vector-Skies-VR` folder
   - Ensure Unity 6.3+ is selected
   - Click to open the project

3. **Wait for initial import** - Unity will import all scripts and assets

### Install Required Packages

Open **Window → Package Manager** and install:

1. **Universal Render Pipeline (URP)**
   - Already included in Unity 6.3

2. **XR Plugin Management**
   - Window → Package Manager → Unity Registry
   - Search "XR Plugin Management"
   - Install latest version

3. **OpenXR Plugin**
   - In Package Manager
   - Search "OpenXR"
   - Install "OpenXR Plugin"

4. **XR Interaction Toolkit** (Optional but recommended)
   - Search "XR Interaction Toolkit"
   - Install latest version

### Configure XR Settings

1. **Open XR Settings**:
   - Edit → Project Settings → XR Plug-in Management

2. **Enable OpenXR**:
   - Switch to **Android** tab (icon at top)
   - Check ✅ **OpenXR**
   - Ignore any warnings about Oculus (OpenXR is the modern standard)

3. **Configure OpenXR**:
   - Click **OpenXR** under XR Plug-in Management
   - Under **Interaction Profiles**, add:
     - ✅ Oculus Touch Controller Profile
   - Under **OpenXR Feature Groups**, enable:
     - ✅ Meta Quest Support
     - ✅ Hand Tracking (optional)

### Android Build Configuration

1. **Switch to Android Platform**:
   - File → Build Settings
   - Select **Android**
   - Click "Switch Platform" (wait for reimport)

2. **Player Settings**:
   - In Build Settings, click "Player Settings"
   
3. **Configure Android Settings**:
   ```
   Company Name: [Your Name]
   Product Name: Vector Skies VR
   
   Other Settings:
   - Package Name: com.[yourname].vectorskiesvr
   - Minimum API Level: Android 10.0 (API 29)
   - Target API Level: Android 13.0 (API 33) or higher
   - Scripting Backend: IL2CPP
   - ARM64: ✅ Enabled
   - ARMv7: ❌ Disabled
   ```

4. **Graphics Settings**:
   ```
   Graphics APIs:
   - Remove Vulkan if present
   - Use: OpenGLES3 only (or add Vulkan first in list)
   
   Color Space: Linear
   ```

5. **Quality Settings**:
   - Edit → Project Settings → Quality
   - Set Default quality level to "Medium" or "High"
   - Ensure VSync is OFF (XR handles this)

6. **XR Settings Verification**:
   - Edit → Project Settings → XR Plug-in Management → Android
   - Ensure OpenXR is checked
   - Click OpenXR settings:
     - Render Mode: Single Pass Instanced
     - Depth Submission Mode: Depth 16 Bit

### Create Initial Scene

1. **Create Main Scene**:
   - File → New Scene
   - Delete default "Main Camera"
   - Delete "Directional Light" (pure black void, no lighting)

2. **Add XR Origin**:
   - GameObject → XR → XR Origin (Action-based) or XR Origin
   - This creates the VR camera rig automatically

3. **Add Game Systems**:
   - Create Empty GameObject: "GameManager"
     - Add Component → `VRCameraRig` script
     - Add Component → `FlightController` script
     - Add Component → `AudioManager` script
   
   - Create Empty GameObject: "CityGenerator"
     - Add Component → `CityGenerator` script
   
   - Create Empty GameObject: "GameMode"
     - Add Component → `EndlessRunMode` OR `RingNavigationMode` script

4. **Assign References in Inspector**:
   - On `VRCameraRig`: Assign the XR Origin's Camera as "Camera Transform"
   - On `FlightController`: References auto-find in Start()
   - On `CityGenerator`: Assign player transform (XR Origin or Camera)

5. **Create Wireframe Material**:
   - Assets → Create → Material → "NeonWireframeMat"
   - Shader: Select `VectorSkies/NeonWireframe`
   - Assign this material to CityGenerator's "Wireframe Material" slot

6. **Save Scene**:
   - File → Save As → `Assets/Scenes/MainGame.unity`

### Build and Deploy to Quest 3

1. **Enable Developer Mode on Quest 3**:
   - Install Meta Quest mobile app
   - Enable Developer Mode in headset settings
   - Connect Quest 3 to PC via USB-C

2. **Build Settings**:
   - File → Build Settings
   - Add Open Scenes: Click "Add Open Scenes"
   - Ensure "MainGame" scene is checked
   - Texture Compression: ASTC
   - Click "Build And Run"
   - Choose a save location for the APK

3. **First Build**:
   - Unity will compile and install to Quest 3
   - Put on headset and allow installation prompt
   - App appears in "Unknown Sources" in Quest library

4. **Testing in Editor** (optional):
   - Install **XR Device Simulator** package for testing without headset
   - Or use **Meta Quest Link** for tethered testing

## 📁 Project Structure

```
Vector-Skies-VR/
├── Assets/
│   ├── Scenes/
│   │   └── MainGame.unity          # Main gameplay scene
│   ├── Scripts/
│   │   ├── FlightController.cs     # Seated VR flight controls
│   │   ├── VRCameraRig.cs          # Camera with stable horizon
│   │   ├── AudioManager.cs         # Spatial audio system
│   │   ├── GameModes/
│   │   │   ├── EndlessRunMode.cs   # Endless mode logic
│   │   │   └── RingNavigationMode.cs # Ring mode logic
│   │   └── ProceduralCity/
│   │       ├── CityGenerator.cs    # Chunk-based city generation
│   │       └── WireframeTower.cs   # Individual tower meshes
│   ├── Shaders/
│   │   └── NeonWireframe.shader    # URP wireframe shader
│   ├── Materials/
│   ├── Prefabs/
│   └── Audio/
├── ProjectSettings/
└── Packages/
```

## 🎨 Art Style

- **Environment**: Pure black void (no skybox, stars, or fog)
- **Geometry**: Neon wireframe only
- **Colors**: 
  - Cyan (primary) - 70%
  - Purple (accent) - 20%
  - Red (hazard) - 10%
- **Effects**: Moderate bloom (avoid eye fatigue)

## ⚡ Performance Optimization

**Target: 90Hz locked on Quest 3**

- ✅ Single-pass instanced rendering
- ✅ GPU instancing for towers
- ✅ No dynamic shadows
- ✅ No real-time lighting
- ✅ Chunk-based streaming
- ✅ Aggressive cleanup behind player
- ✅ Occlusion culling
- ✅ Minimal post-processing

## 🎯 VR Comfort Features

- ✅ Seated-only experience
- ✅ **Stable horizon (NO camera roll)**
- ✅ Smooth acceleration ramping
- ✅ No forced head movement
- ✅ Large, readable wireframes for VR
- ✅ No barrel rolls or extreme maneuvers
- ✅ Optional snap turn for comfort

## 🐛 Troubleshooting

### Build Errors

**"OpenXR not found"**
- Ensure XR Plugin Management is installed
- Check Android tab has OpenXR enabled

**"IL2CPP not installed"**
- Unity Hub → Installs → Unity 6.3 → Add Modules
- Install "Android Build Support" and "IL2CPP"

### Runtime Issues

**Black screen in headset**
- Check XR Origin camera is active
- Ensure scene has XR Rig, not regular camera
- Verify OpenXR is running (check logs)

**Low FPS**
- Reduce tower density in CityGenerator
- Lower quality settings
- Ensure Single Pass Instanced is enabled
- Check GPU Profiler for bottlenecks

**Controls not working**
- Verify OpenXR Interaction Profile includes Oculus Touch
- Check Input System is not conflicting
- Test in Quest home to confirm controllers work

## 📝 Development Notes

### Adding Audio Clips

The `AudioManager` script references audio clips that need to be added:
1. Create or import audio files to `Assets/Audio/`
2. Assign in AudioManager inspector:
   - Engine Hum Clip (looping drone)
   - Wind Loop Clip (spatial wind)
   - Boost Surge Clip (one-shot power-up)
   - Collision Zap Clip (electric zap)
   - Ring Pass Clip (success chime)
   - Synthwave Music Clip (background loop)

### Customizing Flight Physics

Edit `FlightController.cs` values:
- `baseSpeed`, `maxSpeed`, `minSpeed` - Speed range
- `pitchSpeed`, `yawSpeed` - Turn rates
- `maxRollAngle` - Cosmetic banking limit
- `boostMultiplier`, `boostDuration` - Boost behavior

### Adjusting City Generation

Edit `CityGenerator.cs` values:
- `chunksAhead` - View distance
- `towersPerChunk` - Tower density
- `minTowerHeight`, `maxTowerHeight` - Size variation
- Color probabilities for cyan/purple/red distribution

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Credits

Built with Unity 6.3 and OpenXR for Meta Quest 3.

---

**Ready to fly through the neon void?** 🕶️✨
