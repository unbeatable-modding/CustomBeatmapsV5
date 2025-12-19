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

        // NOTE: there are more of these i need to do
    }
}
