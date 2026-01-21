using System;
using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UI.PackageList;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;
using Directory = Pri.LongPath.Directory;

namespace CustomBeatmaps.UI
{
    public class PackageTabUISubmission : AbstractPackageTab<CustomPackageServer>
    {
        private BeatmapDownloadStatus DLStatus;

        protected int _selectedHeaderIndex = 0;

        public PackageTabUISubmission(PackageManagerSubmission manager) : base(manager)
        {  
            LeftRender = () =>
            {
                GUILayout.BeginHorizontal();
                // Render list
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                RenderReloadHeader($"Got {_pkgHeaders.Count} Packages", () =>
                {
                    GUILayout.FlexibleSpace();
                    DifficultyPickerUI.Render(_difficulty, SetDifficulty);
                    GUILayout.FlexibleSpace();
                    SortModePickerUI.Render(SortMode, SetSortMode);
                });
                RenderSearchbar();
                if (_pkgHeaders.Count != 0)
                    PackageListUI.Render($"Submission Packages", _pkgHeaders, SelectedPackageIndex, SetSelectedPackageIndex);
                AssistAreaUI.Render();
                GUILayout.EndVertical();
            };
            
            RightRenders = [
                () =>
                    {
                        PackageInfoTopUI.Render(_selectableBeatmaps, _selectedBeatmapIndex);
                    },
                    () =>
                    {
                        if (_selectedPackage.BeatmapDatas.Length != 0)
                        {
                            // LOCAL high score

                            if (DLStatus == BeatmapDownloadStatus.Downloaded)
                            {
                                try
                                {
                                    PersonalHighScoreUI.Render(_selectedBeatmap.SongPath);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning("Invalid package found: (ignoring)");
                                    Debug.LogException(e);
                                }
                            }
                            // SERVER high scores
                            //HighScoreListUI.Render(UserServerHelper.GetHighScoreBeatmapKeyFromServerBeatmap(ServerURL, _selectedBeatmap.DirectoryPath));
                        }
                    },
                    () =>
                    {
                        if (_selectedPackage.BeatmapDatas.Length == 0)
                        {
                            GUILayout.Label("No beatmaps found...");
                        }
                        else
                        {

                            string buttonText = "??";
                            string buttonSub = "";
                            switch (DLStatus)
                            {
                                case BeatmapDownloadStatus.Downloaded:
                                    buttonText = "PLAY";
                                    buttonSub = $"{_selectedBeatmap.SongName}: {_selectedBeatmap.Difficulty}";
                                    break;
                                case BeatmapDownloadStatus.CurrentlyDownloading:
                                    buttonText = "Downloading...";
                                    break;
                                case BeatmapDownloadStatus.Queued:
                                    buttonText = "Queued for download...";
                                    break;
                                case BeatmapDownloadStatus.NotDownloaded:
                                    buttonText = "DOWNLOAD";
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            PackageBeatmapPickerUI.Render(_selectableBeatmaps, SelectedBeatmapIndex, SetSelectedBeatmapIndex);

                            if (ArcadeHelper.UsingHighScoreProhibitedAssists())
                            {
                                GUILayout.Label("<size=24><b>USING ASSISTS</b></size> (no high score)");
                            }
                            else if (!CustomBeatmaps.UserSession.LoggedIn != (CustomBeatmaps.UserSession.LocalSessionExists() != CustomBeatmaps.UserSession.LoginFailed))
                            {
                                GUILayout.Label("<b>Register above to post your own high scores!<b>");
                            }

                            bool buttonPressed = PlayButtonUI.Render(buttonText, buttonSub);
                            switch (DLStatus)
                            {
                                case BeatmapDownloadStatus.Downloaded:
                                    try
                                    {
                                        if (buttonPressed)
                                        {
                                            CustomBeatmaps.PlayedPackageManager.RegisterPlay(_selectedPackage.BaseDirectory);
                                            RunSong();

                                        }
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        if (PlayButtonUI.Render("INVALID PACKAGE: Redownload"))
                                        {
                                            // Delete + redownload
                                            Directory.Delete(_selectedPackage.BaseDirectory);
                                            CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage, manager.Folder);
                                        }
                                    }

                                    break;
                                case BeatmapDownloadStatus.NotDownloaded:
                                    BGM.StopSongPreview();
                                    if (buttonPressed)
                                    {
                                        CustomBeatmaps.Downloader.QueueDownloadPackage(_selectedPackage, manager.Folder);
                                    }
                                    break;
                            }

                        }
                    }
                ];
        }

        private void RenderReloadHeader(string label, Action renderHeaderSortPicker = null) // ok this part is jank but that's all I need
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", GUILayout.ExpandWidth(false)))
            {
                //ReloadPackageList();
                //Reload(false);
                Manager.ReloadAll();
            }
            GUILayout.Label(label, GUILayout.ExpandWidth(false));

            renderHeaderSortPicker?.Invoke();
            GUILayout.EndHorizontal();
            //GUILayout.Space(20);
        }

        protected override bool MapPackages()
        {
            // Run base Method first and abort if No Packages (base returns false)
            if (base.MapPackages())
            {
                DLStatus = _selectedPackage.DownloadStatus;
                return false;
            }

            DLStatus = _selectedPackage.DownloadStatus;
            return true;
        }

    }
    
}
