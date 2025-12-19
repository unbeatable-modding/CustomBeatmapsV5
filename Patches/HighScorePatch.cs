using CustomBeatmaps.Util.CustomData;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace CustomBeatmaps.Patches
{
    public static class HighScorePatch
    {
        [HarmonyPatch(typeof(HighScoreScreenArcade), "OnScoreScreenUpdated")]
        [HarmonyTranspiler]
        [HarmonyDebug]
        public static IEnumerable<CodeInstruction> ScoreScreenTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);

            bool foundBeatmapParse = false, foundCustomArt = false;
            int beatmapParseIndex = -1, customArtIndex = -1;
            Label endOfCoverIfStatementLabel = il.DefineLabel();

            // adds "Pri.LongPath.File.ReadAllText(string)"
            var instructionsToReadBeatmapFile = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Pri.LongPath.File), nameof(Pri.LongPath.File.ReadAllText), new Type[] { typeof(string) })),
            };

            // adds if "(PackageHelper.TryGetSong(songName out var song))"
            var instructionsToInsertForCover = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_S, 7),
                new CodeInstruction(OpCodes.Ldloca_S, 11),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PackageHelper), nameof(PackageHelper.TryGetSong), 
                    new Type[] { typeof(string), typeof(BeatmapIndex.Song).MakeByRefType() })),
                new CodeInstruction(OpCodes.Brtrue_S, endOfCoverIfStatementLabel)
            };

            var codeMatcher = new CodeMatcher(instructions);
            for (int i = 0; i < code.Count - 6; i++)
            {
                // looking for "beatmap = BeatmapParser.ParseBeatmap(((ArcadeProgression)JeffBezosController.rhythmProgression).customChartPath);"
                /*
                 IL CODE REFERENCE:
                	IL_0516: ldsfld class Rhythm.IProgression JeffBezosController::rhythmProgression
	                IL_051b: castclass Rhythm.ArcadeProgression
	                IL_0520: ldfld string Rhythm.ArcadeProgression::customChartPath
	                IL_0525: call class Rhythm.Beatmap Rhythm.BeatmapParser::ParseBeatmap(string)
	                IL_052a: stloc.s 6
                 */
                if (!foundBeatmapParse)
                {
                    foundBeatmapParse = (code[i].opcode == OpCodes.Ldsfld &&
                    object.ReferenceEquals(code[i].operand, AccessTools.Field(typeof(JeffBezosController), nameof(JeffBezosController.rhythmProgression))) &&
                    code[i + 1].opcode == OpCodes.Castclass &&
                    code[i + 2].opcode == OpCodes.Ldfld &&
                    object.ReferenceEquals(code[i + 2].operand, AccessTools.Field(typeof(ArcadeProgression), nameof(ArcadeProgression.customChartPath))) &&
                    code[i + 3].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i + 3].operand, AccessTools.Method(typeof(BeatmapParser), nameof(BeatmapParser.ParseBeatmap), new Type[] { typeof(string) })) &&
                    code[i + 4].opcode == OpCodes.Stloc_S
                    );

                    if (foundBeatmapParse)
                        beatmapParseIndex = i + 3;

                    // transpiles into "beatmap = BeatmapParser.ParseBeatmap(Pri.LongPath.File.ReadAllText(((ArcadeProgression)JeffBezosController.rhythmProgression).customChartPath))"
                }

                // looking for "if (BeatmapIndex.defaultIndex.TryGetSong(songName, out var song))"
                /*
                IL CODE REFERENCE:
	                IL_069f: call class Rhythm.BeatmapIndex Rhythm.BeatmapIndex::get_defaultIndex()
	                IL_06a4: ldloc.s 7
	                IL_06a6: ldloca.s 11
	                IL_06a8: callvirt instance bool Rhythm.BeatmapIndex::TryGetSong(string, class Rhythm.BeatmapIndex/Song&)
	                IL_06ad: brfalse.s IL_0727
                 */
                if (!foundCustomArt)
                {
                    foundCustomArt = (code[i].opcode == OpCodes.Call &&
                    object.ReferenceEquals(code[i].operand, AccessTools.PropertyGetter(typeof(BeatmapIndex), nameof(BeatmapIndex.defaultIndex))) &&
                    code[i + 1].opcode == OpCodes.Ldloc_S &&
                    code[i + 2].opcode == OpCodes.Ldloca_S &&
                    code[i + 3].opcode == OpCodes.Callvirt &&
                    object.ReferenceEquals(code[i + 3].operand, AccessTools.Method(typeof(BeatmapIndex), nameof(BeatmapIndex.TryGetSong), 
                    new Type[] { typeof(string), typeof(BeatmapIndex.Song).MakeByRefType() } )) &&
                    (code[i + 4].opcode == OpCodes.Brfalse_S || code[i + 4].opcode == OpCodes.Brfalse) 
                    );

                    if (foundCustomArt)
                    {
                        CustomBeatmaps.Log.LogWarning($"WE FOUND THE CODE!");
                        customArtIndex = i;
                        code[i + 5].labels.Add(endOfCoverIfStatementLabel);
                    }

                    // transpiles into "if (PackageHelper.TryGetSong(songName out var song) || BeatmapIndex.defaultIndex.TryGetSong(songName, out var song))"
                }

            }

            if (!foundBeatmapParse || !foundCustomArt)
            {
                CustomBeatmaps.Log.LogError($"FAILED TO FIND CODE, RETURNING ORIGINAL...");
                if (!foundBeatmapParse)
                    CustomBeatmaps.Log.LogError($"beatmapParse NOT FOUND");
                if (!foundCustomArt)
                    CustomBeatmaps.Log.LogError($"foundCustomArt NOT FOUND");
            }

            // NOTE TO SELF: insert last to first or index will change and break everything
            code.InsertRange(customArtIndex, instructionsToInsertForCover);
            code.InsertRange(beatmapParseIndex, instructionsToReadBeatmapFile);

            return code;
        }

    }
}
