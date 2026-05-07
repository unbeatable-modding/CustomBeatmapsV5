using CustomBeatmaps.UI;
using CustomBeatmaps.Util;
using System;
using File = Pri.LongPath.File;

namespace CustomBeatmaps
{

    public class GameMemory
    {
        public int SelectedRoom = 0;
        public Tab SelectedTab = Tab.Online;
        public bool OpeningDisclaimerDisabled = false;
        // Extra modes for fun!
        public bool OneLifeMode = false;
        public bool FlipMode = false;

        public string lastSelectedSong;

        public string lastVersion
        {
            get
            {
                return (_lastVersion ?? "0.0.0");
            }
            set
            {
                _lastVersion = value;
            }
        }

        private string _lastVersion = null;

        public static GameMemory Load(string path)
        {
            if (File.Exists(path))
                return SerializeHelper.LoadJSON<GameMemory>(path);
            return new GameMemory();
        }

        public static void Save(string path, GameMemory gameMemory)
        {
            SerializeHelper.SaveJSON(path, gameMemory);
        }
    }
}