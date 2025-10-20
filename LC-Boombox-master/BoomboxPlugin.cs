// BoomboxPlugin.cs
using System;
using BepInEx;
using BepInEx.Logging;
using CustomBoomboxTracks.Managers;
using HarmonyLib;

namespace CustomBoomboxTracks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BoomboxPlugin : BaseUnityPlugin
    {
        private const string GUID = "com.steven.lethalcompany.boomboxmusic";
        private const string NAME = "Custom Boombox Music";
        private const string VERSION = "1.4.0";

        private static BoomboxPlugin Instance;

        void Awake()
        {
            Instance = this;

            LogInfo("Loading...");

            // Init config first so we know if YouTube mode is on
            Configuration.Config.Init();

            // Only make local folders if we are using local files
            if (!Configuration.Config.UseYouTube)
                AudioManager.GenerateFolders();

            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            LogInfo("Loading Complete!");
        }

        #region logging
        internal static void LogDebug(string message) => Instance.Log(message, LogLevel.Debug);
        internal static void LogInfo(string message) => Instance.Log(message, LogLevel.Info);
        internal static void LogWarning(string message) => Instance.Log(message, LogLevel.Warning);
        internal static void LogError(string message) => Instance.Log(message, LogLevel.Error);
        internal static void LogError(Exception ex) => Instance.Log($"{ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        private void Log(string message, LogLevel logLevel) => Logger.Log(logLevel, message);
        #endregion
    }
}
