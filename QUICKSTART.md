# Vector Skies VR - Quick Start Guide

## 🚀 First Time Opening the Project?

Follow these steps in order:

### 1. Install Unity 6.3
- Download Unity Hub: https://unity.com/download
- Install Unity 6.3 (LTS recommended)
- During installation, include:
  - ✅ Android Build Support
  - ✅ Android SDK & NDK Tools
  - ✅ OpenJDK

### 2. Open the Project
1. Open Unity Hub
2. Click "Add" → Browse to this folder
3. Click on the project to open it
4. **Wait** - First import takes 5-10 minutes

### 3. Install XR Packages
Once Unity opens:

1. **Window → Package Manager**
2. Switch to "Unity Registry" (top-left dropdown)
3. Install these packages:
   - "XR Plugin Management"
   - "OpenXR Plugin"
   - "XR Interaction Toolkit" (optional)

### 4. Enable OpenXR for Android
1. **Edit → Project Settings**
2. **XR Plug-in Management**
3. Click **Android** tab (robot icon)
4. Check ✅ **OpenXR**
5. Click **OpenXR** (now visible in left sidebar)
6. Click **+** under "Interaction Profiles"
7. Add "Oculus Touch Controller Profile"

### 5. Switch to Android Platform
1. **File → Build Settings**
2. Select "Android"
3. Click "Switch Platform"
4. **Wait** - This takes 10-20 minutes first time

### 6. Configure Android Build
Still in Build Settings → "Player Settings":

**Other Settings:**
- Package Name: `com.yourname.vectorskiesvr`
- Minimum API Level: Android 10 (API 29)
- Scripting Backend: **IL2CPP**
- Target Architectures: ✅ **ARM64** only

**XR Plug-in Management → Android → OpenXR:**
- Render Mode: **Single Pass Instanced**
- Depth Submission Mode: Depth 16 Bit

### 7. Create Your First Scene

1. **File → New Scene → Basic (Built-in)**
2. Delete "Main Camera"
3. Delete "Directional Light"

4. **Add XR Rig:**
   - GameObject → XR → XR Origin (Action-based)

5. **Add Game Manager:**
   - GameObject → Create Empty → Name it "GameManager"
   - Add Component → Search "VRCameraRig" → Add
   - Add Component → Search "FlightController" → Add
   - Add Component → Search "AudioManager" → Add

6. **Add City Generator:**
   - GameObject → Create Empty → Name it "CityGenerator"
   - Add Component → Search "CityGenerator" → Add

7. **Add Game Mode:**
   - GameObject → Create Empty → Name it "GameMode"
   - Add Component → Search "EndlessRunMode" → Add

8. **Create Material:**
   - Assets → Create → Material → Name it "NeonWireframe"
   - In Inspector: Shader → VectorSkies → NeonWireframe
   - Drag this material to CityGenerator's "Wireframe Material" slot

9. **Assign References:**
   - Select GameManager
   - On VRCameraRig component:
     - Camera Transform: Drag "Main Camera" from XR Origin
     - Ship Transform: Can leave empty for now
   - On CityGenerator:
     - Player Transform: Drag XR Origin

10. **Save Scene:**
    - File → Save As
    - Name: "MainGame"
    - Location: Assets/Scenes/

### 8. Connect Quest 3

1. **Enable Developer Mode:**
   - Install Meta Quest mobile app
   - Settings → Developer Mode → Enable
   - Connect Quest 3 to PC via USB-C
   - Put on headset and allow USB debugging

2. **Verify Connection:**
   - File → Build Settings
   - Click "Refresh" next to "Run Device"
   - Your Quest 3 should appear in the dropdown

### 9. Build and Run

1. **File → Build Settings**
2. "Add Open Scenes" (adds MainGame scene)
3. Ensure Scene is ✅ checked
4. Select your Quest 3 from "Run Device"
5. Click **"Build And Run"**
6. Save APK somewhere (Desktop is fine)
7. **Wait** - First build takes 15-30 minutes

### 10. Test in Quest 3

1. Put on your Quest 3
2. App will launch automatically
3. You should see:
   - Black void
   - Neon wireframe towers being generated
4. Use controllers:
   - Left stick to fly (pitch/yaw)
   - Right stick Y for throttle

## ⚠️ Common Issues

### "IL2CPP not found"
- Unity Hub → Installs → Unity 6.3 → ⚙️ → Add Modules
- Install "Android Build Support (IL2CPP)"

### "Android SDK not found"
- Edit → Preferences → External Tools
- Check all "Android" boxes to auto-install

### Black screen in headset
- Ensure you deleted the default Main Camera
- Ensure XR Origin has a camera
- Check Edit → Project Settings → XR → OpenXR is enabled

### Controllers don't work
- Verify Oculus Touch Controller Profile is added
- Check XR Plug-in Management → OpenXR → Interaction Profiles

### "Failed to load OpenXR"
- Restart Unity
- Restart Quest 3
- Reconnect USB cable

## 📚 Next Steps

- Read full [README.md](README.md) for detailed features
- Customize flight physics in FlightController.cs
- Adjust city generation in CityGenerator.cs
- Add your own audio clips to AudioManager
- Create custom ship model

## 💡 Tips

- **Testing without Quest:** Install "XR Device Simulator" package from Package Manager
- **Debugging:** Use `adb logcat` in terminal to see Android logs
- **Performance:** GPU Profiler (Window → Analysis → Frame Debugger)
- **Faster builds:** Enable "Development Build" in Build Settings for faster iteration

---

**Need help?** Check Unity's OpenXR documentation or Meta Quest developer forums.
