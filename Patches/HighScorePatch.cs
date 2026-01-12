using Arcade.UI.SongSelect;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util.CustomData;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using TMPro;

namespace CustomBeatmaps.Patches
{
    public static class HighScorePatch
    {
        // the notes are for me i need them
        // good luck comprehending random viewer :)
        [HarmonyDebug]
        [HarmonyPatch(typeof(HighScoreScreenArcade), "OnScoreScreenUpdated")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ScoreScreenTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);

            bool foundBeatmapParse = false, foundCustomArt = false, foundAttributeParse = false, foundAttrPoint = false;
            int beatmapParseIndex = -1, customArtIndex = -1, attrributeParseIndex = -1, AttrPointIndex = -1;
            Label endOfCoverIfStatementLabel = il.DefineLabel(); // Labels are points in the code you can jump to
            Label labelAttributesA = il.DefineLabel();
            Label labelAttributesB = il.DefineLabel();
            Label labelAttributesC = il.DefineLabel();


            // adds "Pri.LongPath.File.ReadAllText(string)"
            var instructionsToReadBeatmapFile = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Pri.LongPath.File), nameof(Pri.LongPath.File.ReadAllText), new Type[] { typeof(string) })),
            };

            // adds "if (PackageHelper.TryGetSong(songName out var song))"
            var instructionsToInsertForCover = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_S, 7),
                new CodeInstruction(OpCodes.Ldloca_S, 11),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PackageHelper), nameof(PackageHelper.TryGetSong), 
                    new Type[] { typeof(string), typeof(BeatmapIndex.Song).MakeByRefType() })),
                new CodeInstruction(OpCodes.Brtrue_S, endOfCoverIfStatementLabel)
            };

            /*
            Trying to patch in fucked up shit:
            this is more or less " if (song.CustomSong && ((CustomBeatmap)song.BeatmapInfo).Data.Attributes.Count > 0)"

            IL_000d: ldarg.2
	        IL_000e: ldfld bool ['Assembly-CSharp']Arcade.UI.SongSelect.ArcadeSongDatabase/BeatmapItem::CustomSong
	        IL_0013: brfalse.s IL_0034

	        // (no C# code)
	        IL_0015: ldarg.2
	        IL_0016: ldfld class ['Assembly-CSharp']Rhythm.BeatmapInfo ['Assembly-CSharp']Arcade.UI.SongSelect.ArcadeSongDatabase/BeatmapItem::BeatmapInfo
	        IL_001b: castclass CustomBeatmaps.CustomData.CustomBeatmap
	        IL_0020: callvirt instance class CustomBeatmaps.CustomData.BeatmapData CustomBeatmaps.CustomData.CustomBeatmap::get_Data()
	        IL_0025: callvirt instance class [netstandard]System.Collections.Generic.HashSet`1<string> CustomBeatmaps.CustomData.BeatmapData::get_Attributes()
	        IL_002a: callvirt instance int32 class [netstandard]System.Collections.Generic.HashSet`1<string>::get_Count()
	        IL_002f: ldc.i4.0
	        IL_0030: cgt
	        IL_0032: br.s IL_0035

	        IL_0034: ldc.i4.0

	        IL_0035: stloc.0
	        IL_0036: ldloc.0
	        IL_0037: brfalse.s IL_00a4



            ADDING THE TEXT IL REFERENCE:

            IL_06cf: ldarg.0
	        IL_06d0: ldfld class [Unity.TextMeshPro]TMPro.TextMeshProUGUI HighScoreScreenArcade::songInfosText
	        IL_06d5: dup
	        IL_06d6: callvirt instance string [Unity.TextMeshPro]TMPro.TMP_Text::get_text()
	        IL_06db: ldstr "\n\n"
	        IL_06e0: call string [netstandard]System.String::Concat(string, string)
	        // songInfosText.text += "<b>Cover Artist:</b>";
	        IL_06e5: callvirt instance void [Unity.TextMeshPro]TMPro.TMP_Text::set_text(string)
	        IL_06ea: ldarg.0
	        IL_06eb: ldfld class [Unity.TextMeshPro]TMPro.TextMeshProUGUI HighScoreScreenArcade::songInfosText
	        IL_06f0: dup
	        IL_06f1: callvirt instance string [Unity.TextMeshPro]TMPro.TMP_Text::get_text()
	        IL_06f6: ldstr "<b>Cover Artist:</b>"
	        IL_06fb: call string [netstandard]System.String::Concat(string, string)
	        // TextMeshProUGUI textMeshProUGUI2 = songInfosText;
	        IL_0700: callvirt instance void [Unity.TextMeshPro]TMPro.TMP_Text::set_text(string)
	        IL_0705: ldarg.0
	        IL_0706: ldfld class [Unity.TextMeshPro]TMPro.TextMeshProUGUI HighScoreScreenArcade::songInfosText
	        // textMeshProUGUI2.text = textMeshProUGUI2.text + " " + song.CoverArtArtist;
	        IL_070b: dup
	        IL_070c: callvirt instance string [Unity.TextMeshPro]TMPro.TMP_Text::get_text()
	        IL_0711: ldstr " "
	        IL_0716: ldloc.s 11
	        IL_0718: callvirt instance string Rhythm.BeatmapIndex/Song::get_CoverArtArtist()
	        IL_071d: call string [netstandard]System.String::Concat(string, string, string)
	        IL_0722: callvirt instance void [Unity.TextMeshPro]TMPro.TMP_Text::set_text(string)
             */
            var instructionsToInsertForAttributes = new List<CodeInstruction>
            {
                // local var reference
                // 11 = song
                // 6 = beatmap
                
                CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Ldloc_S, 11), labelAttributesA), // POINT LABEL A HERE
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapIndex.Song), nameof(BeatmapIndex.Song.IsCustomSong))),
                new CodeInstruction(OpCodes.Brfalse_S, labelAttributesC),

                // behold the most fucked up call i've written so far
                new CodeInstruction(OpCodes.Ldloc_S, 11),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapIndex.Song), nameof(BeatmapIndex.Song.Beatmaps))),
                
                new CodeInstruction(OpCodes.Ldloc, 6),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Beatmap), nameof(Rhythm.Beatmap.metadata))),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(JeffBezosController), nameof(JeffBezosController.rhythmProgression))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Rhythm.IProgression), nameof(Rhythm.IProgression.GetDifficulty))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Rhythm.MetadataInfo), nameof(Rhythm.MetadataInfo.GetDifficulty))),
                
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Dictionary<string, BeatmapInfo>), "Item")),
                new CodeInstruction(OpCodes.Castclass, typeof(ModdedBeatmapInfo)),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ModdedBeatmapInfo), nameof(ModdedBeatmapInfo.Data))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapData), nameof(ModdedBeatmapInfo.Data.Attributes))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(HashSet<string>), nameof(HashSet<string>.Count) )),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Br_S, labelAttributesB),

                new CodeInstruction(OpCodes.Ldc_I4_0),

                CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Stloc, 4), labelAttributesB),
                new CodeInstruction(OpCodes.Ldloc, 4),

                //new CodeInstruction(OpCodes.Brfalse, labelAttributesC),
                new CodeInstruction(OpCodes.Pop),
                

                // god help me
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, "\n\n"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),

                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, "<b>Attributes:</b>"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),

                // NOTE: FIX THIS
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, " "),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),

                

                //new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Dictionary<string, BeatmapInfo>).MakeArrayType(), nameof(Rhythm.BeatmapInfo))),

                /*
                new CodeInstruction(OpCodes.Ldloca_S, 11),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BeatmapInfo), nameof(ArcadeSongDatabase.BeatmapItem.BeatmapInfo))),
                new CodeInstruction(OpCodes.Castclass, typeof(CustomBeatmap)),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapData), nameof(CustomBeatmap.Data))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(HashSet<string>), nameof(BeatmapData.Attributes))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(int), nameof(HashSet<string>.Count))),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Br_S, labelAttributesB), // ADD LABEL B
                
                CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Ldc_I4_0), labelAttributesA), // POINT LABEL A HERE

                CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Stloc_0), labelAttributesB), // POINT LABEL B HERE
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Brfalse, labelAttributesC), // POINT TO END OF THIS SHIT
                */

                /*
                // Code to actually add the fucking text
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, "\n\n"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),

                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, "<b>Attributes:</b>"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new Type[] { typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),

                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HighScoreScreenArcade), "songInfosText")),

                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                new CodeInstruction(OpCodes.Ldstr, " "),
                new CodeInstruction(OpCodes.Ldstr, ","),

                new CodeInstruction(OpCodes.Ldloca_S, 11),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BeatmapInfo), nameof(ArcadeSongDatabase.BeatmapItem.BeatmapInfo))),
                new CodeInstruction(OpCodes.Castclass, typeof(CustomBeatmap)),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BeatmapData), nameof(CustomBeatmap.Data))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(HashSet<string>), nameof(BeatmapData.Attributes))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Join),
                    new Type[] { typeof(string), typeof(IEnumerable<string>) } )),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat),
                    new Type[] { typeof(string), typeof(string), typeof(string) } )),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text))),
                */
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
                        customArtIndex = i;
                        code[i + 5].labels.Add(endOfCoverIfStatementLabel);
                    }

                    // transpiles into "if (PackageHelper.TryGetSong(songName out var song) || BeatmapIndex.defaultIndex.TryGetSong(songName, out var song))"
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
                if (!foundAttrPoint)
                {
                    foundAttrPoint = (
                        code[i].opcode == OpCodes.Ldloc_S
                        && code[i + 1].opcode == OpCodes.Callvirt
                        && object.ReferenceEquals(code[i + 1].operand, AccessTools.PropertyGetter(typeof(BeatmapIndex.Song), nameof(BeatmapIndex.Song.CoverArtArtist)))
                        && code[i + 2].opcode == OpCodes.Call
                        && object.ReferenceEquals(code[i + 2].operand, AccessTools.Method(typeof(System.String), nameof(System.String.IsNullOrEmpty)))
                        && (code[i + 3].opcode == OpCodes.Brtrue_S || code[i + 3].opcode == OpCodes.Brtrue)
                    );

                    if (foundAttrPoint)
                    {
                        
                        //code[i + 3] = new CodeInstruction(OpCodes.Brtrue_S, labelAttributesA);
                    }

                    // transpiles into "if (PackageHelper.TryGetSong(songName out var song) || BeatmapIndex.defaultIndex.TryGetSong(songName, out var song))"
                }

                if (!foundAttributeParse)
                {
                    foundAttributeParse = (code[i].opcode == OpCodes.Callvirt
                        && object.ReferenceEquals(code[i].operand, AccessTools.PropertyGetter(typeof(BeatmapIndex.Song), nameof(BeatmapIndex.Song.CoverArtArtist)))
                        && code[i + 1].opcode == OpCodes.Call
                        && object.ReferenceEquals(code[i + 1].operand, AccessTools.Method(typeof(string), nameof(string.Concat),
                            new Type[] { typeof(string), typeof(string), typeof(string) }))
                        && code[i + 2].opcode == OpCodes.Callvirt
                        && object.ReferenceEquals(code[i + 2].operand, AccessTools.PropertySetter(typeof(TMP_Text), nameof(TextMeshProUGUI.text)))

                    );

                    if (foundAttributeParse)
                    {
                        attrributeParseIndex = i + 3;
                        code[i + 3].labels.Add(labelAttributesC);
                    }

                    // transpiles into "if (PackageHelper.TryGetSong(songName out var song) || BeatmapIndex.defaultIndex.TryGetSong(songName, out var song))"
                }

            }

            if (!foundBeatmapParse || !foundCustomArt || !foundAttributeParse || !foundAttrPoint)
            {
                CustomBeatmaps.Log.LogError($"FAILED TO FIND CODE, RETURNING ORIGINAL...");
                if (!foundBeatmapParse)
                    CustomBeatmaps.Log.LogError($"beatmapParse NOT FOUND");
                if (!foundCustomArt)
                    CustomBeatmaps.Log.LogError($"foundCustomArt NOT FOUND");
                if (!foundAttributeParse)
                    CustomBeatmaps.Log.LogError($"foundAttributeParse NOT FOUND");
            }

            // NOTE TO SELF: insert last to first or index will change and break everything
            //code.InsertRange(attrributeParseIndex, instructionsToInsertForAttributes); // (currently fucked)
            code.InsertRange(customArtIndex, instructionsToInsertForCover);
            code.InsertRange(beatmapParseIndex, instructionsToReadBeatmapFile);

            return code;
        }

        private static BeatmapIndex.Song song = new("dummy");
        private static TextMeshProUGUI songInfosText = new();
        //private static Beatmap beatmap = new();

        public static void DummyMethod()
        {
            Beatmap beatmap = new ();
            if (song.IsCustomSong && ((ModdedBeatmapInfo)song.Beatmaps[beatmap.metadata.GetDifficulty(JeffBezosController.rhythmProgression.GetDifficulty())]).Data.Attributes.Count > 0)
            {
                songInfosText.text += "\n\n";
                songInfosText.text += "<b>Attributes:</b>";
                TextMeshProUGUI textMeshProUGUI2 = songInfosText;
                textMeshProUGUI2.text = textMeshProUGUI2.text + " " + 
                    string.Join(", ", ((ModdedBeatmapInfo)song.Beatmaps[beatmap.metadata.GetDifficulty(JeffBezosController.rhythmProgression.GetDifficulty())]).Data.Attributes);
            }

        }
    }
}
