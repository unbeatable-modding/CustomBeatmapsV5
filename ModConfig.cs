using System.Collections.Generic;
using Pri.LongPath;
using UnityEngine;

namespace CustomBeatmaps
{
    /// <summary>
    /// Base config file for entire mod
    /// </summary>
    public class ModConfig
    {
        // NOTE TO SELF: fix the configs fucked up auto generation
        
        // :sunglasses:
        public bool DarkMode = true;
        /// <summary>
        /// Directory for user (local) packages
        /// </summary>
        public string[] UserPackagesDir = MakeUserPackagesDir();
        /// <summary>
        /// Directory for server/downloaded packages
        /// </summary>
        public string ServerPackagesDir = TryFindOtherGame("CustomBeatmapsV3-Data/SERVER_PACKAGES", out var dir) ? dir : "CustomBeatmapsV4-Data/SERVER_PACKAGES";
        /// <summary>
        /// Songs directory for your OSU install for the mod to access 
        /// </summary>& test
        public string OsuSongsOverrideDirectory = null;
        /// <summary>
        /// Directory (relative to UNBEATABLE) where your OSU file packages will export
        /// </summary>
        public string OsuExportDirectory = ".";
        /// <summary>
        /// Temporary folder used to load + play a user submission
        /// </summary>
        public string TemporarySubmissionPackageFolder = "CustomBeatmapsV4-Data/.SUBMISSION_PACKAGE.temp";
        /// <summary>
        /// The local user "key" for high score submissions
        /// </summary>
        public string UserUniqueIdFile = TryFindOtherGame("CustomBeatmapsV3-Data/.USER_ID", out var file) ? file : "CustomBeatmapsV4-Data/.USER_ID";
        /// <summary>
        /// A line separated list of all beatmaps we've tried playing
        /// </summary>
        public string PlayedBeatmapList = "CustomBeatmapsV4-Data/.played_beatmaps";
        public bool ShowHiddenStuff = false;


        /// <param name="path"> Path relative to White Label directory </param>
        /// <param name="getDir"> Directory (if it exists) </param>
        private static bool TryFindOtherGame(string path, out string getDir)
        {
            var test = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            var dataDir = test.Substring(0, test.LastIndexOf('/'));
            // Get the directory of the custom songs
            string[] gameDirs = [
                $"{dataDir}/UNBEATABLE [white label]/{path}",
                $"{dataDir}/UNBEATABLE Demo/{path}"
                ];
            
            foreach (string dir in gameDirs)
            {
                if (Directory.Exists(dir) || File.Exists(dir))
                {
                    getDir = dir;
                    return true;
                }
            }
            getDir = null;
            return false;
        }

        private static string[] MakeUserPackagesDir()
        {
            List<string> array = ["USER_PACKAGES"];
            if (TryFindOtherGame("USER_PACKAGES", out var dir))
                array.Add(dir);
            return array.ToArray();
        }

    }
}
