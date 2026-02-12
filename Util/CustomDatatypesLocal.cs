using System;
using System.Collections.Generic;
using System.Linq;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Util
{
    public class CustomPackageLocal : CustomPackage
    {
        public CustomPackageLocal() : this(Guid.Empty) { }
        public CustomPackageLocal(Guid guid) : base(guid) { }

        public override string ToString()
        {
            //return $"{{{Path.GetFileName(FolderName)}: [{Beatmaps.Join()}]}}";
            return $"{{{Path.GetFileName(BaseDirectory)}: [\n  {SongDatas.ToArray().Select(song => 
            new 
            { 
                Song = song.Name,
                Difficulties = song.InternalDifficulties.Join()
            }).Join(delimiter: ",\n  ")}\n]}}";
        }
    }

    public class InitialLoadStateData
    {
        public bool Loading;
        public int Loaded;
        public int Total;
    }

    public struct TagData
    {
        public int Level;

        public string FlavorText;

        public float SongLength;

        //public Dictionary<string, bool> Attributes;
        public HashSet<string> Attributes;

        public int PreviewTime;

        public string CoverArtArtist;
    }

    
}