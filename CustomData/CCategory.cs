using CustomBeatmaps.Util;
using HarmonyLib;
using Rhythm;
using System.Collections.Generic;
using static Rhythm.BeatmapIndex;

namespace CustomBeatmaps.CustomData
{


    public readonly struct CCategory
    {
        /// <summary>
        /// Position of the InternalCategory
        /// </summary>
        public readonly int Index;
        
        /// <summary>
        /// Category in UNBEATABLE
        /// </summary>
        public readonly BeatmapIndex.Category InternalCategory => BeatmapIndex.defaultIndex.Categories[Index];

        /// <summary>
        /// Custom Category used by this mod (and creates a category in the game)
        /// </summary>
        /// <param name="name">Name of the Category</param>
        public CCategory(string name)
        {
            var traverse = Traverse.Create(BeatmapIndex.defaultIndex);
            var categories = traverse.Field("categories").GetValue<List<Category>>();
            var categorySongs = traverse.Field("_categorySongs").GetValue<Dictionary<Category, List<Song>>>();

            // Check if the custom category already exists
            if (!categories.Exists(c => c.Name == name))
            {
                
                // If not, add it to the list
                Index = categories.Count; // Count is always +1 relative to index so might as well set it before adding
                BeatmapIndex.Category categoryToAdd = new(name, "", Index);
                categories.Add(categoryToAdd);
                // Fun Fact: Categories do not display unless they have one song, however this is literally the only time the song list inside a category is ever checked
                categorySongs.TryAdd(categoryToAdd, new List<Song>([new Song("LoadBearingSongDoNotDeleteThisSeriously")])); 

                ScheduleHelper.SafeLog($"Added category {categoryToAdd.Name}");

            }
            // Category already exists, so just set the index
            else
            {
                Index = categories.FindIndex(c => c.Name == name);
            }

        }

    }
}
