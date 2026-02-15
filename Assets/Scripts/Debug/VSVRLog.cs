using UnityEngine;

namespace VectorSkiesVR
{
    /// <summary>
    /// Centralized logging helper with proper Android logcat tag support.
    /// All logs are prefixed with "TGMC" for easy filtering in logcat.
    /// Use logcat filter: "tag:VSVR" or search for "VSVR" text.
    /// </summary>
    public static class VSVRLog
    {
        private const string TAG_PREFIX = "VSVR";

        private static string FormatMessage(string className, string message)
        {
            return $"[{TAG_PREFIX}:{className}] {message}";
        }

        // Compatibility alias: Warn -> Warning
        public static void Warn(string className, string message)
        {
            Debug.LogWarning(FormatMessage(className, message));
        }

        public static void Info(string className, string message)
        {
            if (VSVRVersion.Verbose || VSVRVersion.Debug)
            {
                Debug.Log(FormatMessage(className, message));
            }
        }

        public static void Warning(string className, string message)
        {
            Debug.LogWarning(FormatMessage(className, message));
        }

        public static void Error(string className, string message)
        {
            Debug.LogError(FormatMessage(className, message));
        }

        public static void Verbose(string className, string message)
        {
            if (VSVRVersion.Verbose)
            {
                Debug.Log(FormatMessage(className, message));
            }
        }

        public static void DebugLog(string className, string message)
        {
            if (VSVRVersion.Debug)
            {
                Debug.Log(FormatMessage(className, message));
            }
        }
    }
}
