# Unity Project Configuration Files

These `.meta` files will be generated automatically by Unity when you first open the project.

## First-Time Setup

1. Open Unity Hub
2. Add this project folder
3. Open with Unity 6.3 or newer
4. Unity will automatically generate:
   - `.meta` files for all assets
   - `Library/` folder (build cache)
   - `Temp/` folder (temporary files)
   - `Logs/` folder (debug logs)
   - `.sln` and `.csproj` files (Visual Studio project)

## Important Notes

- **DO NOT** manually create `.meta` files
- **DO NOT** commit `Library/`, `Temp/`, or `Logs/` folders to Git
- The `.gitignore` file already excludes these folders
- Each script, folder, and asset will get its own `.meta` file
- `.meta` files **SHOULD** be committed to Git to preserve GUIDs

## Expected Generated Files

After first opening in Unity, you should see:

```
Assets/
├── Scripts/
│   ├── FlightController.cs
│   ├── FlightController.cs.meta          ← Generated
│   ├── AudioManager.cs
│   ├── AudioManager.cs.meta              ← Generated
│   └── ...
├── Scenes.meta                            ← Generated
├── Scripts.meta                           ← Generated
├── Shaders.meta                           ← Generated
└── ...

Library/                                   ← Generated (not committed)
Temp/                                      ← Generated (not committed)
Logs/                                      ← Generated (not committed)
obj/                                       ← Generated (not committed)
UserSettings/                              ← Generated (not committed)
```

## If Unity Fails to Open

If Unity won't open the project:

1. Ensure you have Unity 6.3+ installed
2. Ensure Android Build Support is installed
3. Delete `Library/` folder if it exists
4. Let Unity regenerate everything fresh
5. Check Unity Hub → Preferences → External Tools are configured

## Version Control

This project uses Git LFS for large files. Ensure Git LFS is installed:

```bash
git lfs install
git lfs track "*.png"
git lfs track "*.jpg"
git lfs track "*.wav"
git lfs track "*.mp3"
git lfs track "*.fbx"
```

Files tracked by LFS are configured in `.gitattributes`.
