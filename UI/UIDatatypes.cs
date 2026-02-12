
using System.Collections.Generic;
using System.Linq;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using HarmonyLib;

namespace CustomBeatmaps.UI
{

    public enum Tab
    {
        //Online, Local, Submissions, Osu
        Online, Local, Edit
    }

    public enum SortMode
    {
        New, Title, Artist, Creator, Downloaded
    }

    public enum Difficulty
    {
        All,
        Beginner,
        Normal,
        Hard,
        Expert,
        Unbeatable,
        Star
    }

}