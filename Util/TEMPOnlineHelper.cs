using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomBeatmaps.Util
{
    public static class TEMPOnlineHelper
    {
        public static CustomPackageServer[] LoadServerPackages(string folderPath, CCategory category, List<TEMPOnlinePackage> opkgs,
            Action<CustomPackage> onLoadPackage = null, Action<BeatmapException> onBeatmapFail = null)
        {
            folderPath = Path.GetFullPath(folderPath);

            var pkgs = new Dictionary<string, CustomPackageServer>();
            Func<Dictionary<string, CustomPackageServer>> getPkgs = () => { return pkgs; };


            // Get online Packages first (if we can)
            ScheduleHelper.SafeLog("step A (Loading Online)");
            foreach (var opkg in opkgs)
            {
                ScheduleHelper.SafeLog($"LOADING: {opkg.FilePath}");
                if (TryLoadOnlineServerPackage(opkg, out var potentialNewPackage, category, onBeatmapFail, getPkgs))
                {
                    try
                    {
                        pkgs.TryAdd(potentialNewPackage.ServerURL.Substring("packages/".Length), potentialNewPackage);
                        onLoadPackage?.Invoke(potentialNewPackage);
                    }
                    catch (Exception e)
                    {
                        CustomBeatmaps.Log.LogError(e);
                    }
                }
            }


            // Packages = .bmap files
            ScheduleHelper.SafeLog("step B (Loading Locally)");
            foreach (string subDir in Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories))
            {
                if (TryLoadLocalServerPackage(subDir, folderPath, out CustomPackageServer potentialNewPackage, category, false, onBeatmapFail, getPkgs))
                {
                    pkgs.TryAdd(potentialNewPackage.BaseDirectory, potentialNewPackage);
                    onLoadPackage?.Invoke(potentialNewPackage);
                }
            }

            ScheduleHelper.SafeLog($"LOADED {pkgs.Count} PACKAGES");
            ScheduleHelper.SafeLog($"####### FULL PACKAGES LIST: #######\n{string.Join("\n", pkgs.Values)}");

            return pkgs.Values.ToArray();
        }

        public static bool TryLoadLocalServerPackage(string packageFolder, string outerFolderPath, out CustomPackageServer package, CCategory category, bool recursive = false,
            Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<string, CustomPackageServer>> getPkgs = null)
        {
            package = new CustomPackageServer();

            packageFolder = Path.GetFullPath(packageFolder);
            outerFolderPath = Path.GetFullPath(outerFolderPath);

            // We can't do Path.GetRelativePath, Path.GetPathRoot, or string.Split so this works instead.
            string relative = Path.GetFullPath(packageFolder).Substring(outerFolderPath.Length + 1); // + 1 removes the start slash
            // We also only want the stub (lowest directory)
            string rootSubFolder = Path.Combine(outerFolderPath, StupidMissingTypesHelper.GetPathRoot(relative));


            package.BaseDirectory = rootSubFolder;
            package.Time = Directory.GetLastWriteTime(packageFolder);
            package.DownloadStatus = BeatmapDownloadStatus.Downloaded;

            ScheduleHelper.SafeLog($"{relative}\\");

            var songs = new Dictionary<string, SongData>();

            var subFiles = recursive ?
                Directory.EnumerateFiles(packageFolder, "*", SearchOption.AllDirectories) :
                Directory.EnumerateFiles(packageFolder, "*");

            if (subFiles.Where(s => s.ToLower().EndsWith(".bmap")).Any())
            {
                foreach (string packageCoreFile in subFiles.Where(s => s.ToLower().EndsWith(".bmap")))
                {
                    ScheduleHelper.SafeLog($"    {packageCoreFile.Substring(packageFolder.Length)}");
                    try
                    {
                        var pkgCore = SerializeHelper.LoadJSON<PackageCore>(packageCoreFile);
                        package.GUID = pkgCore.GUID;

                        for (var i = 0; i < pkgCore.Songs.Count; i++)
                        {
                            foreach (var song in pkgCore.Songs[i])
                            {
                                var bmapInfo = new BeatmapData(pkgCore.GUID, i, song.Key, $"{packageFolder}\\{song.Value}", category);

                                if (songs.TryGetValue(bmapInfo.InternalName, out _))
                                {
                                    if (!songs[bmapInfo.InternalName].TryAddToThisSong(bmapInfo))
                                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"FAILED TO ADD BEATMAP \"{bmapInfo.BeatmapPath}\" TO IT'S SONG"));
                                }
                                else
                                {
                                    songs.Add(bmapInfo.InternalName, new SongData(bmapInfo));
                                }
                            }
                        }

                        // We want to attach to an existing package (if we can)
                        if (getPkgs != null)
                        {
                            //if (getPkgs.Invoke().TryGetValue(package.BaseDirectory, out CustomPackageServer pkgFetch) && songs.Any())
                            int lastSlash = package.BaseDirectory.LastIndexOf("\\", StringComparison.Ordinal);
                            if (getPkgs.Invoke().TryGetValue(package.BaseDirectory.Substring(lastSlash+1), out CustomPackageServer pkgFetch) && songs.Any())
                            {
                                pkgFetch.SongDatas = songs.Values.ToList();
                                pkgFetch.DownloadStatus = BeatmapDownloadStatus.Downloaded;
                                pkgFetch.BaseDirectory = package.BaseDirectory;
                                package = new CustomPackageServer();
                                return false;
                            }
                        }


                        // Set using core data if it exists
                        if (pkgCore.Name != null)
                            package.Name = pkgCore.Name;
                        if (pkgCore.Mappers != null)
                            package.Mappers = pkgCore.Mappers;
                        if (pkgCore.Artists != null)
                            package.Artists = pkgCore.Artists;
                    }
                    catch (BeatmapException f)
                    {
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    BEATMAP FAIL: {f.Message}"));
                        onBeatmapFail?.Invoke(f);
                    }
                    catch (Exception f)
                    {
                        BeatmapException e = new BeatmapException("Invalid Package formatting", packageCoreFile);
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    BEATMAP FAIL: {e.Message}"));
                        ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError($"    Exception: {f}"));
                        onBeatmapFail?.Invoke(e);
                    }

                }

            }

            // This folder has some beatmaps!
            if (songs.Any())
            {
                package.SongDatas = songs.Values.ToList();
                return true;
            }

            // Package already exists
            package = new CustomPackageServer();
            return false;
        }

        public static bool TryLoadOnlineServerPackage(TEMPOnlinePackage oPkg, out CustomPackageServer package, CCategory category,
    Action<BeatmapException> onBeatmapFail = null, Func<Dictionary<string, CustomPackageServer>> GUIDs = null)
        {
            //package = new CustomPackageServer();

            // ???
            if (GUIDs != null && GUIDs.Invoke().ContainsKey(oPkg.FilePath))
            {
                package = new CustomPackageServer();
                onBeatmapFail.Invoke(new BeatmapException("Online Package is Duplicate???", oPkg.FilePath));
                return false;
            }

            var songs = new Dictionary<string, SongData>();
            package = new CustomPackageServer(Guid.NewGuid());
            package.GUID = Guid.NewGuid();
            package.ServerURL = oPkg.FilePath;
            //package.BaseDirectory = oPkg.ServerURL;
            package.Time = oPkg.UploadTime;
            package.DownloadStatus = BeatmapDownloadStatus.NotDownloaded;

            var offset = 0;
            foreach (var oBmap in oPkg.Beatmaps.Values)
            {
                var bmapInfo = new BeatmapData(oBmap, package.GUID, offset, category);
                if (songs.TryGetValue(bmapInfo.InternalName, out var song))
                {
                    
                    while (songs.TryGetValue(bmapInfo.InternalName, out song))
                    {
                        if (!song.TryAddToThisSong(bmapInfo))
                        {
                            bmapInfo.Offset++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    if (!songs.TryGetValue(bmapInfo.InternalName, out song))
                        songs.Add(bmapInfo.InternalName, new SongData(bmapInfo));

                    //ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogWarning($"FAILED TO ADD BEATMAP \"{bmapInfo.BeatmapPath}\" TO IT'S SONG"));
                }
                else
                {
                    songs.Add(bmapInfo.InternalName, new SongData(bmapInfo));
                }
            }

            // This folder has some beatmaps!
            if (songs.Any())
            {
                //ScheduleHelper.SafeLog("Loading ONLINE");
                package.SongDatas = songs.Values.ToList();
                return true;
            }

            // Package already exists locally
            package = new CustomPackageServer();
            return false;
        }


        public static async Task<TEMPOnlinePackage[]> FetchOnlinePackageList(string url)
        {
            try
            {
                var pkgList = await FetchHelper.GetJSON<TEMPOnlinePackageList>(url);
                return pkgList.Packages;
            }
            catch
            {
                return [];
            }
        }

        private struct TEMPOnlinePackageList
        {
            [JsonProperty("packages")]
            public TEMPOnlinePackage[] Packages;
        }


        public struct TEMPOnlinePackage
        {
            [JsonProperty("filePath")]
            public string FilePath;
            [JsonProperty("time")]
            public DateTime UploadTime;
            [JsonProperty("beatmaps")]
            public Dictionary<string, TEMPOnlineBeatmap> Beatmaps;
        }

        public struct TEMPOnlineBeatmap
        {
            [JsonProperty("name")]
            public string SongName;
            [JsonProperty("artist")]
            public string Artist;
            [JsonProperty("creator")]
            public string Creator;
            [JsonProperty("difficulty")]
            public string Difficulty;
            [JsonProperty("audioFileName")]
            public string AudioFileName;
        }
    }
}
