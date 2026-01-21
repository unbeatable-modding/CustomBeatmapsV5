using System;
using System.Collections.Generic;

namespace CustomBeatmaps.CustomData
{
    public struct PackageCore
    {
        /// <summary>
        /// Name of the Package shown to the user (can be null)
        /// </summary>
        public string Name;

        /// <summary>
        /// Name of the Mappers shown to the user (can be null)
        /// </summary>
        public string Mappers;

        /// <summary>
        /// Name of the Artists shown to the user (can be null)
        /// </summary>
        public string Artists;

        /// <summary>
        /// The Package's internal GUID
        /// </summary>
        public Guid GUID;

        public List<SortedDictionary<InternalDifficulty, string>> Songs; // terrible
    }
    
    public enum InternalDifficulty
    {
        Beginner = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3,
        UNBEATABLE = 4,
        Star = 5
    }

}
