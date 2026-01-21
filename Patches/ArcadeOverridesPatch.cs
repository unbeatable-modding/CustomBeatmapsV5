using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using FMODUnity;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using static Rhythm.BeatmapIndex;
using static Rhythm.RhythmController;

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
        [HarmonyDebug]
        [HarmonyPatch(typeof(ArcadeSongDatabase), "Awake")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SongDatabaseTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);

            CodeInstruction[] tmp = [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ArcadeSongDatabase), "_beatmapIndex"))
                ];

            codeMatcher
                .MatchForward(false,
                    new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(UnityEngine.Application), nameof(UnityEngine.Application.isEditor)))
                    ) // Get rid of the "--customsongs" check
                .RemoveInstructions(11)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ArcadeSongDatabase), "_loadCustomSongs"))
                    ) // Don't add the "custom" category
                .RemoveInstructions(10)
                .MatchForward(false,
                    //new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ArcadeSongDatabase), "_beatmapIndex")),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BeatmapIndex), nameof(BeatmapIndex.GetVisibleCategories))),
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Dup)
                )
                .RemoveInstructions(19)
                .InsertAndAdvance(
                    [
                        //new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ArcadeSongDatabase), "_beatmapIndex")),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ArcadeSongDatabase), "allCategory")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArcadeOverridesPatch), nameof(ArcadeOverridesPatch.LazyCategoryFix)))
                    ]
                );

            return codeMatcher.Instructions();

        }

        [HarmonyPatch(typeof(ArcadeSongInfos), "SongInfosStringChanged")]
        [HarmonyPostfix]
        public static void ShowCustomInfo(string value, ref TextMeshProUGUI ___songInfos, ArcadeSongDatabase.BeatmapItem ___song)
        {
            ___songInfos.text = value;
            if (___song != null && ___song.CustomSong && (((ModdedBeatmapInfo)___song.BeatmapInfo).Data.Attributes.Count > 0))
            {
                ___songInfos.text += "\n\n";
                ___songInfos.text += "Attributes:";
                TextMeshProUGUI textMeshProUGUI = ___songInfos;
                textMeshProUGUI.text = textMeshProUGUI.text + "\n" + string.Join(", ", ((ModdedBeatmapInfo)___song.BeatmapInfo).Data.Attributes);
            }
        }

        [HarmonyPatch(typeof(ArcadeSongInfos), "SongInfosStringSelectedChanged")]
        [HarmonyPostfix]
        public static void ShowCustomSelectedInfo(string value, ref TextMeshProUGUI ___songInfosSelected, ArcadeSongDatabase.BeatmapItem ___song)
        {
            ___songInfosSelected.text = value;
            if (___song != null && ___song.CustomSong && (((ModdedBeatmapInfo)___song.BeatmapInfo).Data.Attributes.Count > 0))
            {
                ___songInfosSelected.text += "\n\n";
                ___songInfosSelected.text += "<b>Attributes:</b>";
                TextMeshProUGUI textMeshProUGUI = ___songInfosSelected;
                textMeshProUGUI.text = textMeshProUGUI.text + " " + string.Join(", ", ((ModdedBeatmapInfo)___song.BeatmapInfo).Data.Attributes);
            }
        }

        [HarmonyPatch(typeof(LevelManager), "LoadCustomArcadeLevel")]
        [HarmonyPrefix]
        public static void SaveMyBoy(Song song)
        {
            if (song is CustomSong cusSong)
            {
                CustomBeatmaps.LocalServerPackages.TryImmortalizeBeatmap(cusSong.Data.DirectoryPath);
                CustomBeatmaps.LocalUserPackages.TryImmortalizeBeatmap(cusSong.Data.DirectoryPath);
                CustomBeatmaps.LocalEditorPackages.TryImmortalizeBeatmap(cusSong.Data.DirectoryPath);
            }
        }

        [HarmonyPatch(typeof(LevelManager), "LoadArcadeLevel")]
        [HarmonyPatch(typeof(LevelManager), "LoadCustomArcadeLevel")]
        [HarmonyPostfix]
        public static void WatcherMassacare()
        {
            CustomBeatmaps.LocalServerPackages.KillAllWatchers();
            CustomBeatmaps.LocalUserPackages.KillAllWatchers();
            CustomBeatmaps.LocalEditorPackages.KillAllWatchers();
            murdered = true;
        }

        private static bool murdered = false;

        [HarmonyPatch(typeof(LevelManager), "LoadLevel", new Type[] { typeof(string), typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPatch(typeof(ArcadeProgression), "Finish")]
        [HarmonyPatch(typeof(ArcadeProgression), "Back")]
        [HarmonyPostfix]
        public static void WatcherNecromancy(object[] __args, MethodBase __originalMethod)
        {
            if (__originalMethod.Name == "LoadLevel")
            {
                if ((string)__args[0] != JeffBezosController.arcadeMenuScene)
                    return;
            }
            if (!murdered)
                return;
            CustomBeatmaps.LocalServerPackages.WaitNoBringThemBack();
            CustomBeatmaps.LocalUserPackages.WaitNoBringThemBack();
            CustomBeatmaps.LocalEditorPackages.WaitNoBringThemBack();
            murdered = false;
        }

        // this is a base game issue...
        private static Category LazyCategoryFix(BeatmapIndex _beatmapIndex, Category allCategory)
        {
            return ((_beatmapIndex.GetVisibleCategories().FirstOrDefault((BeatmapIndex.Category c) => c.Name == FileStorage.beatmapOptions.arcadeSelectedCategory) != null)
                ? _beatmapIndex.GetVisibleCategories().FirstOrDefault((BeatmapIndex.Category c) => c.Name == FileStorage.beatmapOptions.arcadeSelectedCategory) 
                : allCategory);
        }
    }
}
