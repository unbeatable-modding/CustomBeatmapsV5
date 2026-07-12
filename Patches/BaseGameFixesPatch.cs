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

        /// <remarks>
        /// Fix found by Stefyfresh
        /// </remarks>
        [HarmonyPatch(typeof(RhythmTracker))]
        [HarmonyPatch("HandleCreateProgrammerSound")]
        static IEnumerable<CodeInstruction> SoundCreationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Add MODE.ACCURATETIME to the Mode parameter when creating the programmer sound

            bool found = false;
            foreach (var instruction in instructions)
            {
                // transpiler nonsense
                if (instruction.opcode == OpCodes.Ldc_I4 && (int)instruction.operand == 66050) // 66050 = MODE.LOOP_NORMAL | MODE.CREATECOMPRESSEDSAMPLE | MODE.NONBLOCKING
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 82434); // 82434 = MODE.LOOP_NORMAL | MODE.CREATECOMPRESSEDSAMPLE | MODE.NONBLOCKING | MODE.ACCURATETIME
                }
                else
                {
                    yield return instruction;
                }
            }

            if (found)
            {
                CustomBeatmaps.Log.LogDebug("Successfully patched sound mode info.");
            }
            else
            {
                CustomBeatmaps.Log.LogError("Could not find sound mode info to patch!");
            }
        }

        /// <remarks>
        /// Fix found by Stefyfresh
        /// </remarks>
        [HarmonyPatch(typeof(RhythmTracker))]
        [HarmonyPatch("GetSongDuration")]
        private static IEnumerable<CodeInstruction> GetSongDurationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            // Add MODE.ACCURATETIME to the Mode parameter when getting song duration

            bool found = false;
            foreach (var instruction in instructions)
            {
                // transpiler nonsense
                if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int num && num == 8448) // 8448 = MODE.CREATESAMPLE | MODE.OPENONLY
                {
                    found = true;
                    instruction.operand = 24832; // 24832 = MODE.CREATESAMPLE | MODE.OPENONLY | MODE.ACCURATETIME
                    yield return instruction;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (found)
            {
                CustomBeatmaps.Log.LogDebug("Successfully patched sound mode info for duration.");
            }
            else
            {
                CustomBeatmaps.Log.LogError("Could not find sound mode info for duration to patch!");
            }
        }
    }
}
