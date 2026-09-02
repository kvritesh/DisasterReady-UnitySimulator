using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DisasterReady.Integration
{
    /// <summary>
    /// Parameters the hosting web page launched this simulator build with.
    /// Read-only, additive integration data - does not affect any existing
    /// gameplay, mission, or scoring logic. On non-WebGL builds (Editor,
    /// Windows standalone) this always returns the Aizawl defaults, so the
    /// frozen Windows vertical slice behaves exactly as before.
    /// </summary>
    [Serializable]
    public struct SimulatorLaunchParams
    {
        public string regionId;
        public string scenarioId;
        public string simulatorMode;
    }

    /// <summary>
    /// The preparedness result handed back to the hosting web page once the
    /// simulation completes. Mirrors exactly the values EmergencyScenarioController
    /// already computes for the in-Unity result panel - no new scoring logic.
    /// </summary>
    [Serializable]
    public struct SimulatorResult
    {
        public string regionId;
        public string scenarioId;
        public bool simulationCompleted;
        public int missionsCompleted;
        public int missionsTotal;
        public int xpEarned;
        public int xpMax;
        public int preparednessScore;
    }

    /// <summary>
    /// Minimal bridge between the Unity WebGL build and the DisasterReady web
    /// platform page that embeds/opens it. Every WebGL-specific call is guarded
    /// by UNITY_WEBGL &amp;&amp; !UNITY_EDITOR, so this class is a complete no-op
    /// (aside from a debug log) in the Editor and in the validated Windows
    /// standalone build - the frozen vertical slice is unaffected.
    /// </summary>
    public static class WebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern IntPtr DR_GetLaunchParams();
        [DllImport("__Internal")] private static extern void DR_SendResultToBrowser(string json);
#endif

        private static SimulatorLaunchParams? _cached;

        /// <summary>
        /// Region/scenario/mode the web app launched this build with, read once
        /// from the page's URL query string and cached. Defaults to the Aizawl
        /// preparedness scenario when there is no query string to read (Editor
        /// Play Mode, the Windows standalone build, or a WebGL build opened with
        /// no parameters).
        /// </summary>
        public static SimulatorLaunchParams GetLaunchParams()
        {
            if (_cached.HasValue) return _cached.Value;

            var result = new SimulatorLaunchParams
            {
                regionId = "aizawl-mizoram",
                scenarioId = "preparedness-default",
                simulatorMode = "standard"
            };

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                IntPtr ptr = DR_GetLaunchParams();
                string json = Marshal.PtrToStringUTF8(ptr);
                if (!string.IsNullOrEmpty(json))
                {
                    var parsed = JsonUtility.FromJson<SimulatorLaunchParams>(json);
                    if (!string.IsNullOrEmpty(parsed.regionId))
                    {
                        result = parsed;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WebGLBridge] Failed to read launch params, using defaults: " + e.Message);
            }
#endif
            _cached = result;
            return result;
        }

        /// <summary>
        /// Sends the finished preparedness result to the web page that opened
        /// this build. On non-WebGL platforms this just logs what would have
        /// been sent, so calling it from shared gameplay code is always safe.
        /// </summary>
        public static void SendResult(SimulatorResult result)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string json = JsonUtility.ToJson(result);
                DR_SendResultToBrowser(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WebGLBridge] Failed to send result to browser: " + e.Message);
            }
#else
            Debug.Log("[WebGLBridge] (non-WebGL build, no-op) Would send result: " + JsonUtility.ToJson(result));
#endif
        }
    }
}
