using Arcade.UI;
using CustomBeatmaps.Util.CustomData;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Rhythm.BeatmapIndex;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    public class SongData
    {
        /// <summary>
        /// The Song the Game uses
        /// </summary>
        public CustomSong Song;

        // FOR ALL SONGS
        /// <summary>
        /// The song name that is displayed to the user
        /// </summary>
        public string Name { get; private set; }
        public string InternalName { get; private set; }
        public string Creator { get; private set; }
        public string Artist { get; private set; }

        public List<BeatmapData> BeatmapDatas = new();

        public ModdedBeatmapInfo[] BeatmapInfos
        {
            get
            {
                return BeatmapDatas.Where(b => b.IsLocal).Select(b => b.BeatmapPointer).ToArray();
            }
        }

        public HashSet<string> InternalDifficulties
        {
            get
            {
                try
                {
                    return BeatmapDatas.Select(i => i.InternalDifficulty).ToHashSet();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        // LOCAL FOR NOW

        public string CoverPath { get; private set; }

        // LOCAL ONLY
        public string AudioPath { get; private set; }
        public string DirectoryPath { get; private set; }
        public CCategory Category { get; private set; }

        private bool isLocal = false;
        public bool Local
        {
            get
            {
                return isLocal;
            }
        }
        private Traverse Traverse;

        /// <summary>
        /// Make a Song using Local Files
        /// </summary>
        /// <param name="bmapPath"></param>
        /// <param name="category"></param>
        public SongData(string bmapPath, int category)
        {
            var text = File.ReadAllText(bmapPath);
            DirectoryPath = Path.GetDirectoryName(bmapPath);

            Name = BeatmapHelper.GetBeatmapProp(text, "Title", bmapPath);
            Artist = BeatmapHelper.GetBeatmapProp(text, "Artist", bmapPath);
            Creator = BeatmapHelper.GetBeatmapProp(text, "Creator", bmapPath);

            InternalName = $"CUSTOM__{defaultIndex.Categories[category]}__{Name}";
            var audio = BeatmapHelper.GetBeatmapProp(text, "AudioFilename", bmapPath);

            // realPath fixes some issues with old beatmaps, don't remove
            var realPath = audio.Contains("/") ? audio.Substring(audio.LastIndexOf("/") + 1, audio.Length - (audio.LastIndexOf("/") + 1)) : audio;
            AudioPath = $"{DirectoryPath}\\{realPath}";

            InitLocalSong();
        }

        public SongData(BeatmapData bmapData)
        {
            // Sync up song stuff
            Name = bmapData.SongName;
            InternalName = bmapData.InternalName;
            Category = bmapData.Category;

            Artist = bmapData.Artist;
            Creator = bmapData.Creator;

            

            if (bmapData.IsLocal)
            {
                DirectoryPath = bmapData.DirectoryPath;
                AudioPath = bmapData.AudioPath;
                if (bmapData.CoverPath != null)
                    CoverPath = bmapData.CoverPath;
                InitLocalSong(bmapData);
                //TryAddToThisSong(bmapData);
            }
            else
            {
                BeatmapDatas.Add(bmapData);
            }

            
        }

        private void InitLocalSong(BeatmapData bmap = null)
        {
            Song = new CustomSong(InternalName, this);
            Song.stageScene = "TrainStationRhythm";
            Traverse = Traverse.Create(Song);
            Traverse.Field("visibleInArcade").SetValue(true);
            Traverse.Field("_category").SetValue(Category.InternalCategory);
            Traverse.Field("category").SetValue(Category.Index);

            // new stuff
            //Traverse.Field("IsCustomSong").SetValue(true);
            Traverse.Field("coverArtArtist").SetValue(string.Empty);

            AccessTools.PropertySetter(typeof(Song), nameof(Song.IsCustomSong))
                    .Invoke(Song, new object[] { true });
            AccessTools.PropertySetter(typeof(Song), nameof(Song.CustomPath))
                    .Invoke(Song, new object[] { bmap.DirectoryPath });
            AccessTools.PropertySetter(typeof(Song), nameof(Song.CustomAudioFile))
                    .Invoke(Song, new object[] { bmap.AudioPath });

            Traverse.Field("previewStartTime").SetValue(bmap.Tags.PreviewTime);
            //Traverse.Method("set_IsCustomSong").Field("value").SetValue(true);
            //Traverse.Property("set_CustomPath").Field("value").SetValue(bmap.DirectoryPath);
            //Traverse.Property("set_CustomAudioFile").Field("value").SetValue(bmap.AudioPath);
            //Traverse.Property("CustomPath").SetValue(bmap.DirectoryPath);
            //Traverse.Property("CustomAudioFile").SetValue(bmap.AudioPath);

            // not new sstuff
            Traverse.Field("_difficulties").SetValue(new List<string>());
            Traverse.Field("beatmaps").SetValue(new List<BeatmapInfo>());
            Traverse.Field("_beatmaps").SetValue(new Dictionary<string, BeatmapInfo>());

            bmap.SongBackRef = Song;

            isLocal = true;
            AddToThisSong(bmap);
        }

        public bool TryAddToThisSong(BeatmapData bmap)
        {
            if (InternalDifficulties.Contains(bmap.InternalDifficulty))
                return false;
            AddToThisSong(bmap);
            return true;
        }

        private void AddToThisSong(BeatmapData bmapData)
        {
            try
            {
                BeatmapDatas.Add(bmapData);
                if (isLocal)
                {
                    var _difficulties = Traverse.Field("_difficulties").GetValue<List<string>>();
                    var beatmaps = Traverse.Field("beatmaps").GetValue<List<BeatmapInfo>>();
                    var _beatmaps = Traverse.Field("_beatmaps").GetValue<Dictionary<string, BeatmapInfo>>();

                    _difficulties.Add(bmapData.InternalDifficulty);
                    beatmaps.Add(bmapData.BeatmapPointer);
                    _beatmaps.Add(bmapData.InternalDifficulty, bmapData.BeatmapPointer);

                    bmapData.SongBackRef = Song;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public class CustomSong : Song
    {
        public SongData Data { get; }
        public CustomSong(string name, SongData song) : base(name)
        {
            Data = song;

        }

        // Custom Images Stuff
        public void GetTexture()
        {
            if (!CustomBeatmaps.CanGetTexture)
                return;

            var traverse = Traverse.Create(this);

            if (Data.CoverPath != null && File.Exists($"{Data.DirectoryPath}\\{Data.CoverPath}"))
            {
                var texture = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes($"{Data.DirectoryPath}\\{Data.CoverPath}");
                texture.LoadImage(bytes);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                traverse.Field("coverArt").SetValue(sprite);
            }
        }
    }
}
