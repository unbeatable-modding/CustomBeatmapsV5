using Arcade.UI.SongSelect;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using FMODUnity;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.Patches
{
    public static class ArcadeOverridesPatch
    {

        // Patch the song function to return all (also hidden) songs,
        // so we can access hidden beatmaps
        [HarmonyPatch(typeof(BeatmapIndex), "GetVisibleSongs")]
        [HarmonyPrefix]
        public static bool UnhideSongs(ref BeatmapIndex __instance, ref List<Song> __result)
        {
            if (!CustomBeatmaps.ModConfig.ShowHiddenStuff)
                return true;
            CustomBeatmaps.Log.LogDebug("Overriding GetVisibleSongs");
            __result = __instance.GetAllSongs();
            return false;
        }

        [HarmonyPatch(typeof(HighScoreList), nameof(HighScoreList.ReplaceHighScore))]
        [HarmonyPrefix]
        public static bool HighScoreSaveCheck(ref bool __result)
        {
            if (!ArcadeHelper.UsingHighScoreProhibitedAssists())
                return true;
            __result = false;
            return false;
        }


        [HarmonyPatch(typeof(ArcadeSongDatabase), "LoadCustoms")]
        [HarmonyPrefix]
        public static bool LoadSuperCustomSongs(ref List<Song> __result)
        {
            __result = PackageHelper.GetAllCustomSongs.Select(a => (Song) a).ToList();
            return false;
        }

        // ALWAYS load custom songs regardless of the games thoughts
        [HarmonyPatch(typeof(ArcadeSongDatabase), "Awake")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SongDatabaseTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);
            int startIndex = -1, endIndex = -1;

            /*
             	// look for "if ((Application.isEditor || Environment.GetCommandLineArgs().Contains("-customsongs")) && Directory.Exists(Path.Combine(Application.persistentDataPath, "CustomSongs")))"
	            
                IL CODE REFERENCE:
                IL_0047: call bool [UnityEngine.CoreModule]UnityEngine.Application::get_isEditor()
	            IL_004c: brtrue.s IL_005f

	            IL_004e: call string[] [netstandard]System.Environment::GetCommandLineArgs()
	            IL_0053: ldstr "-customsongs"
	            IL_0058: call bool [netstandard]System.Linq.Enumerable::Contains<string>(class [netstandard]System.Collections.Generic.IEnumerable`1<!!0>, !!0)
	            IL_005d: brfalse.s IL_007c

	            IL_005f: call string [UnityEngine.CoreModule]UnityEngine.Application::get_persistentDataPath()
	            IL_0064: ldstr "CustomSongs"
	            IL_0069: call string [netstandard]System.IO.Path::Combine(string, string)
	            IL_006e: call bool [netstandard]System.IO.Directory::Exists(string)
	            IL_0073: brfalse.s IL_007c
             */

            //var codeMatcher = new CodeMatcher(instructions);
            for (int i = 0; i < code.Count - 11; i++)
            {
                // if ((Application.isEditor || Environment.GetCommandLineArgs().Contains("-customsongs")) && Directory.Exists(Path.Combine(Application.persistentDataPath, "CustomSongs")))
                if (code[i].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i].operand, AccessTools.PropertyGetter(typeof(UnityEngine.Application), nameof(UnityEngine.Application.isEditor))) &&
                    code[i + 1].opcode == OpCodes.Brtrue && // idk why short form doesn't work here
                    code[i + 2].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Method(typeof(System.Environment), nameof(System.Environment.GetCommandLineArgs))) &&
                    code[i + 3].opcode == OpCodes.Ldstr &&
                    code[i + 4].opcode == OpCodes.Call &&
                    code[i + 5].opcode == OpCodes.Brfalse &&

                    code[i + 6].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 6].operand, AccessTools.PropertyGetter(typeof(UnityEngine.Application), nameof(UnityEngine.Application.persistentDataPath))) &&
                    code[i + 7].opcode == OpCodes.Ldstr &&
                    code[i + 8].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 8].operand, AccessTools.Method(typeof(System.IO.Path), nameof(System.IO.Path.Combine),
                        new Type[] { typeof(string), typeof(string) })) &&
                    code[i + 9].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 9].operand, AccessTools.Method(typeof(System.IO.Directory), nameof(System.IO.Directory.Exists),
                        new Type[] { typeof(string) })) &&
                    code[i + 10].opcode == OpCodes.Brfalse
                    )
                {
                    startIndex = i;
                    endIndex = i + 11;
                }

            }

            if (startIndex < 0 || endIndex < 0)
            {
                CustomBeatmaps.Log.LogError("FAILED TO TRANSPILE OUT CLI ARGS, THIS IS VERY BAD");
                CustomBeatmaps.Log.LogError("Returning original code...");
                return instructions;
            }

            code[startIndex].opcode = OpCodes.Nop;
            code.RemoveRange(startIndex + 1, endIndex - startIndex - 1);

            return code;
        }
    }
}
