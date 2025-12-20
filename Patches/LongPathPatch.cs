using FMODUnity;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace CustomBeatmaps.Patches
{
    public static class LongPathPatch
    {

        [HarmonyPatch(typeof(CustomBeatmapInfo), "text", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool textPatch(ref string __result, string ____beatmapPath)
        {
            __result = Pri.LongPath.File.ReadAllText(____beatmapPath);
            return false;
        }

        [HarmonyPatch(typeof(CustomBeatmapInfo), "DifficultyFilename", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool diffPatch(ref string __result, CustomBeatmapInfo __instance)
        {
            __result = Pri.LongPath.Path.GetFileName(__instance.FilePath);
            return false;
        }

        // NOTE: there are more of these i need to do
    }
}
