// Utilities/ChatUtil.cs
using System;
using UnityEngine;

namespace CustomBoomboxTracks.Utilities
{
    internal static class ChatUtil
    {
        /// <summary>
        /// Attempts to post a message to the multiplayer chat for all players.
        /// Falls back to logging if HUDManager API isn't available yet.
        /// </summary>
        public static void PostToAll(string message)
        {
            try
            {
                var hud = UnityEngine.Object.FindObjectOfType<HUDManager>();
                if (hud == null)
                {
                    CustomBoomboxTracks.BoomboxPlugin.LogInfo($"[Chat] {message}");
                    return;
                }

                // Prefer server-wide method if present (MP safe)
                var m = typeof(HUDManager).GetMethod("AddTextToChatOnServer");
                if (m != null)
                {
                    // Signature: AddTextToChatOnServer(string text, int playerId = -1)
                    m.Invoke(hud, new object[] { message, -1 });
                    return;
                }

                // Fallback older/alt API name
                m = typeof(HUDManager).GetMethod("AddChatMessage");
                if (m != null)
                {
                    m.Invoke(hud, new object[] { message });
                    return;
                }

                CustomBoomboxTracks.BoomboxPlugin.LogInfo($"[Chat] {message}");
            }
            catch (Exception ex)
            {
                CustomBoomboxTracks.BoomboxPlugin.LogError($"Chat post failed: {ex}");
                CustomBoomboxTracks.BoomboxPlugin.LogInfo($"[Chat] {message}");
            }
        }
    }
}
