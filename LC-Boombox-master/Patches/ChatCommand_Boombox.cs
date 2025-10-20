// Patches/ChatCommand_Boombox.cs
using HarmonyLib;
using UnityEngine;
using CustomBoomboxTracks.Managers;
using CustomBoomboxTracks.Utilities;

namespace CustomBoomboxTracks.Patches
{
    [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
    internal class ChatCommand_Boombox
    {
        static bool Prefix(HUDManager __instance)
        {
            // Grab the last message typed
            string msg = __instance.chatTextField.text.Trim();
            if (string.IsNullOrWhiteSpace(msg))
                return true; // allow normal chat behavior

            // Match "/boombox play"
            if (msg.Equals("/boombox play", System.StringComparison.OrdinalIgnoreCase))
            {
                PlayBoombox();
                __instance.chatTextField.text = ""; // clear input
                __instance.chatTextField.ActivateInputField();
                return false; // skip sending this as a normal chat message
            }

            return true; // let other chat messages pass through
        }

        private static void PlayBoombox()
        {
            var boombox = Object.FindObjectOfType<BoomboxItem>();
            if (boombox == null)
            {
                ChatUtil.PostToAll("<color=#FF8080>[Boombox]</color> No boombox found in the scene!");
                return;
            }

            boombox.StartMusic(true);

            // Inform players
            var clip = boombox.boomboxAudio?.clip;
            if (clip != null)
            {
                if (AudioManager.TryGetYoutubeUrl(clip, out var url))
                    ChatUtil.PostToAll($"<color=#8EE8FF>[Boombox]</color> Manually started: {clip.name} — {url}");
                else
                    ChatUtil.PostToAll($"<color=#8EE8FF>[Boombox]</color> Manually started: {clip.name}");
            }
            else
            {
                ChatUtil.PostToAll("<color=#8EE8FF>[Boombox]</color> Started boombox (no clip info).");
            }
        }
    }
}
