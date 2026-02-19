using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Rhythm;
using Arcade.UI.SongSelect;
using UnityEngine.SceneManagement;
using Arcade.UI;
using FMODUnity;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.Util.CustomData;
using System.Threading.Tasks;
using System.Threading;

using static Rhythm.BeatmapIndex;
using CustomBeatmaps.Patches;

namespace CustomBeatmaps.Util
{
    public class ArcadeHelper
    {

        private static Traverse traverse = Traverse.Create(BeatmapIndex.defaultIndex);
        private static List<string> songNames = traverse.Field("_songNames").GetValue<List<string>>();
        private static List<Song> songs = traverse.Field("songs").GetValue<List<Song>>();
        private static Dictionary<string, Song> _songs = traverse.Field("_songs").GetValue<Dictionary<string, Song>>();
        private static List<Song> _visibleSongs = traverse.Field("_visibleSongs").GetValue<List<Song>>();
        private static Dictionary<Category, List<Song>> _categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();

        //static OsuBeatmapHotLoader HotLoader = new OsuBeatmapHotLoader();
        public static CustomBeatmapRoom[] Rooms
        {
            get
            {
                var rooms = new List<CustomBeatmapRoom>();
                rooms.AddRange(BaseRooms);
                if (CustomBeatmaps.ModConfig.ShowHiddenStuff)
                    rooms.AddRange(ExtraRooms);
                return rooms.ToArray();
            }
        }

        private static readonly CustomBeatmapRoom[] BaseRooms = {
            new CustomBeatmapRoom("Default", "TrainStationRhythm"),
            new CustomBeatmapRoom("NSR", "NSR_Stage"),
            new CustomBeatmapRoom("Green Screen", "GreenscreenRhythm"),
            // I am not re-implementing these just yet
            //new CustomBeatmapRoom("Practice Room", "PracticeRoomRhythm"),
            //new CustomBeatmapRoom("Tutorial", "Tutorial"),
            // This one would be interesting but we already have the tutorial screen
            //new CustomBeatmapRoom("Offset Wizard", "OffsetWizard")
        };
        private static readonly CustomBeatmapRoom[] ExtraRooms = {
            new CustomBeatmapRoom("Stage", "Stage")
        };

        private static readonly string DefaultBeatmapScene = "TrainStationRhythm";

        public static string GetSceneNameByIndex(int index)
        {
            if (index < 0 || index >= Rooms.Length)
            {
                return DefaultBeatmapScene;
            }

            return Rooms[index].SceneName;
        }

        public static bool LoadingArcade { get; private set; } = false;

        /// <summary>
        /// Forcefully reload the arcade
        /// </summary>
        public static void ReloadArcadeList(Action onUpdate = null)
        {
            while (LoadingArcade) { Thread.Sleep(200); }
            LoadingArcade = true;
            //LoadCustomSongs();
            if (SceneManager.GetActiveScene().name != "ArcadeModeMenu")
            {
                LoadingArcade = false;
                return;
            }
            var currentArcade = ArcadeSongDatabase.Instance;
            var arcade = Traverse.Create(currentArcade);
            var _songDatabase = arcade.Field("_songDatabase").GetValue<Dictionary<string, ArcadeSongDatabase.BeatmapItem>>();
            _songDatabase.Clear();
            arcade.Method("LoadDatabase").GetValue();
            arcade.Method("RefreshSongList", [false]).GetValue();
            LoadingArcade = false;
            onUpdate?.Invoke();
        }

        public static ArcadeSongDatabase SongDatabase => ArcadeSongDatabase.Instance;
        public static ArcadeSongList SongList => ArcadeSongList.Instance;
        public static ArcadeBGMManager BGM => ArcadeBGMManager.Instance;

        public static void PlaySong(BeatmapData bmap)
        {
            PlaySong(bmap, GetSceneNameByIndex(CustomBeatmaps.Memory.SelectedRoom));
        }
        public static void PlaySong(BeatmapData bmap, string scene)
        {
            ForceSelectSong(bmap);
            var onSongPlaySound = Traverse.Create(SongDatabase).Field("onSongPlaySound").GetValue<EventReference>();

            if (bmap.BeatmapPointer != null)
            {
                if (!onSongPlaySound.IsNull)
                {
                    RuntimeManager.PlayOneShot(onSongPlaySound);
                }

                JeffBezosController.instance.DisableUIInputs();
                JeffBezosController.returnFromArcade = true;

                CustomBeatmaps.Memory.lastSelectedSong = bmap.SongPath;

                LevelManager.LoadCustomArcadeLevel(bmap.SongBackRef, bmap.InternalDifficulty);
            }
        }
        public static void ForceSelectSong(BeatmapData bmap)
        {
            UIButtonPatch.silent = true;
            if (ArcadeSongDatabase.SelectedCategory.Name != "all")
                SongDatabase.SetCategory(bmap.Category.InternalCategory);
            SongDatabase.SetDifficulty(bmap.InternalDifficulty);
            SongList.SetSelectedSongIndex(SongDatabase.IndexOfSong(bmap.SongPath));
            FileStorage.SaveOptions();
            //UIButtonPatch.silent = false;
        }
        public static void PlaySongEdit(BeatmapData bmap, bool enableCountdown = false)
        {
            //OsuEditorPatch.SetEditMode(true, enableCountdown, beatmap.Info.OsuPath, beatmap.Info.SongPath);
            PlaySong(bmap, DefaultBeatmapScene);
        }


        // CUSTOMBEATMAPS V3 STUFF TO CHANGE LATER BELOW


        public static HighScoreList LoadArcadeHighscores()
        {
            return HighScoreScreen.LoadHighScores(RhythmGameType.ArcadeMode);
        }

        public static float GetSongSpeed(int songSpeedIndex)
        {
            switch (songSpeedIndex)
            {
                case 0:
                    return 1f;
                case 1:
                    return 0.5f;
                case 2:
                    return 2f;
                default:
                    throw new InvalidOperationException($"Invalid song speed index: {songSpeedIndex}");
            }
        }

        public static bool UsingHighScoreProhibitedAssists()
        {
            // We include flip mode because _potentially_ it might be used to make high notes easier to hit?
            return (JeffBezosController.GetAssistMode() == 1) || GetSongSpeed(JeffBezosController.GetSongSpeed()) < 1 || (JeffBezosController.GetNoFail() == 1) || CustomBeatmaps.Memory.FlipMode;
        }

        public struct CustomBeatmapRoom
        {
            public string Name;
            public string SceneName;

            public CustomBeatmapRoom(string name, string sceneName)
            {
                Name = name;
                SceneName = sceneName;
            }
        }
    }
}

    

