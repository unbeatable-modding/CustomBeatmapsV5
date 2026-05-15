using CustomBeatmaps.UI;
using CustomBeatmaps.UI.Highscore;
using HarmonyLib;
using IL3DN;
using Rewired.Demos;
using UnityEngine;

namespace CustomBeatmaps.Patches
{
    /// <summary>
    /// JeffBezosController hides the cursor, but we have our UI's open sometimes so don't do that lel
    /// </summary>
    public static class CursorUnhidePatch
    {
        [HarmonyPatch(typeof(JeffBezosController), "Update")]
        [HarmonyPostfix]
        public static void JeffBezosPostUpdate()
        {
            if (CustomBeatmapsUIBehaviour.Opened || HighScoreUIBehaviour.Opened || !CustomBeatmaps.Memory.OpeningDisclaimerDisabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}