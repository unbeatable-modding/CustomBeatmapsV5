using Arcade.UI.SongSelect;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using static Arcade.UI.SongSelect.ArcadeSongDatabase;

namespace CustomBeatmaps.Patches
{
    // base game fixes because i can
    public static class BaseGameFixesPatch
    {

        [HarmonyPatch(typeof(ArcadeSongDatabase), "PlaySong")]
        [HarmonyPrefix]
        public static bool RememberLastSong(BeatmapItem beatmapItem)
        {
            CustomBeatmaps.Memory.lastSelectedSong = beatmapItem.Path;
            FileStorage.SaveOptions();
            return true;
        }

        [HarmonyPatch(typeof(ArcadeSongList), "GetBaseSongIndex")]
        [HarmonyPrefix]
        public static bool LoadLastSong(ref int defaultSongIndex)
        {
            var index = ArcadeSongDatabase.Instance.IndexOfSong(CustomBeatmaps.Memory.lastSelectedSong);
            if (index > 0)
            {
                defaultSongIndex = index;
            }
            return true;
        }

        /// <summary>
        /// Removes starting spaces that may or may not exist
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch(typeof(BeatmapParserEngine), "ParseLineMetadata")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> IHateWhitespace(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);

            codeMatcher.MatchForward(true,
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Ldelem_Ref),
                    // code gets injected right here because it always likes inserting in front of the code
                    new CodeMatch(OpCodes.Stloc_1)
                )
                .InsertAndAdvance(
                    [
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BaseGameFixesPatch), nameof(BaseGameFixesPatch.GetRidOfThatWhitespace)))
                    ]
                );

            return codeMatcher.Instructions();
        }

        private static string GetRidOfThatWhitespace(string toCull)
        {
            var match = Regex.Match(toCull, @"^\s+(.*)");
            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return toCull;
        }
    }
}
