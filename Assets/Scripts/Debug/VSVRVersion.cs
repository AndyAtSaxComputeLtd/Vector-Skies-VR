using UnityEngine;

namespace VectorSkiesVR
{
    /// <summary>
    /// Global version number and debug flags for Vector Skies VR.
    /// Version is automatically pulled from Unity Project Settings > Player > Version.
    /// </summary>
    public static class VSVRVersion
    {
        /// <summary>
        /// Returns the version from Unity Project Settings (Player Settings > Version)
        /// </summary>
        public static string VERSION => Application.version;
        
        /// <summary>
        /// Enable verbose logging (general debug info, state changes, etc.)
        /// </summary>
        public static bool Verbose = false;
        
        /// <summary>
        /// Enable debug logging (detailed diagnostics, method calls, etc.)
        /// Automatically enabled for development builds.
        /// </summary>
        public static bool Debug => UnityEngine.Debug.isDebugBuild;
        
        /// <summary>
        /// Returns formatted log prefix: "TGMC [ClassName] v3.1"
        /// </summary>
        public static string LogPrefix(string className)
        {
            return $"VSVR [{className}] {VERSION}";
        }
    }
}
