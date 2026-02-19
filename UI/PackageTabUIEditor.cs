using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageTabUIEditor : AbstractPackageTab<CustomPackageLocal>
    {
        private static bool _overrideCountdown = true;
        public PackageTabUIEditor(PackageManagerLocal pkgManager) : base(pkgManager)
        {
            RightRenders = [
                () =>
                    {
                        PackageInfoTopUI.Render(_selectableBeatmaps, SelectedBeatmapIndex);
                    },
                    () =>
                    {
                        GUILayout.TextArea(
                                        "GUIDE UNDER CONSTRUCTION!!! (Check Discord)\n" +
                                        "CURRENT WORKING MAPPING TOOLS:\n" +
                                        "https://github.com/Splash02/CBM-Editor (unofficial editor)\n" +
                                        "https://github.com/ErikGXDev/UnbeatableOsuEditor (osu! lazer ruleset)\n" +
                                        "https://osu.ppy.sh/home/download (osu STABLE exclusive!!!)"
                            );
                        MetadataUI.Render(_selectedBeatmap);
                        if (GUILayout.Button($"Init Editor Packages"))
                        {
                            CustomBeatmaps.LocalEditorPackages.GenerateCorePackages();
                            //CustomBeatmaps.LocalServerPackages.GenerateCorePackages();
                        }
                        if (GUILayout.Button($"Init ALL Packages"))
                        {
                            CustomBeatmaps.LocalEditorPackages.GenerateCorePackages();
                            CustomBeatmaps.LocalServerPackages.GenerateCorePackages();
                            CustomBeatmaps.LocalUserPackages.GenerateCorePackages();
                        }
                    },
                    () =>
                    {
                        _overrideCountdown = GUILayout.Toggle(_overrideCountdown, "Do Countdown?");
                        if (GUILayout.Button($"EXPORT"))
                        {
                            string exportFolder = Config.Mod.OsuExportDirectory;
                            string exportName = _selectedBeatmap.SongName;
                            OSUHelper.CreateExportZipFile(_selectedPackage, exportFolder);
                        }
                        PackageBeatmapPickerUI.Render(_selectableBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                        if (PlayButtonUI.Render("EDIT", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                        {
                            // Play a local beatmap
                            var package = _pkgHeaders[SelectedPackageIndex];
                            RunSong();
                        }
                    }
            ];
        }

        protected override void RunSong()
        {
            //Manager.ImmortalizeBeatmap(_selectedBeatmap.di);
            ArcadeHelper.PlaySongEdit(_selectedBeatmap, _overrideCountdown);
        }

    }
}
