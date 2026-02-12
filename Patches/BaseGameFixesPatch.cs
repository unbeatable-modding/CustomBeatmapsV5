using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace CustomBeatmaps.Patches
{
    // base game fixes because i can
    public static class BaseGameFixesPatch
    {

        // Make cop 4 killable
        [HarmonyPatch(typeof(BrawlInfo), "SetBrawlData")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixTheGodDamnCop(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);

            codeMatcher
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ret),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(BrawlInfo), nameof(BrawlInfo.isFinish))),
                    new CodeMatch(OpCodes.Ret)
                )
                .Advance(2) // keep that OpCodes.Ldarg_0 pointer cause i can be lazy and not override the label pointer
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_3),
                    new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(BrawlInfo), nameof(BrawlInfo.id))),
                    new CodeInstruction(OpCodes.Ldarg_0)
                    );
            
            return codeMatcher.Instructions();
        }
    }
}
