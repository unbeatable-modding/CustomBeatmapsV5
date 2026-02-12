using CustomBeatmaps.CustomData;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace CustomBeatmaps.UI
{
    public class PackageTabUILocal : AbstractPackageTab<CustomPackageLocal>
    {
        protected override string Folder => string.Join(" & ", ((PackageManagerMulti)Manager).Folders);

        public PackageTabUILocal(PackageManagerMulti pkgManager) : base(pkgManager)
        {

            RightRenders = [
                () =>
                {
                    PackageInfoTopUI.Render(_selectableBeatmaps, SelectedBeatmapIndex);
                },
                () =>
                {
                },
                () =>
                {
                    if (ArcadeHelper.UsingHighScoreProhibitedAssists())
                    {
                        GUILayout.Label("<size=24><b>USING ASSISTS</b></size> (no high score)");
                    }
                    PackageBeatmapPickerUI.Render(_selectableBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);
                    if (PlayButtonUI.Render("Play", $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}"))
                    {
                        // Play a local beatmap
                        RunSong();
                    }
                }
            ];
            
        }

    }
}
