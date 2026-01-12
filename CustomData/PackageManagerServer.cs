using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using CustomBeatmaps.Util.CustomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CustomBeatmaps.Util.TEMPOnlineHelper;
using Directory = Pri.LongPath.Directory;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerServer : PackageManagerGeneric<CustomPackageServer>
    {
        protected List<TEMPOnlinePackage> _onlinePackages = new();
        public List<TEMPOnlinePackage> OnlinePackages => _onlinePackages;
        protected virtual string onlinePkgSource => CustomBeatmaps.BackendConfig.ServerPackageList;

        public PackageManagerServer(Action<BeatmapException> onLoadException) : base(onLoadException)
        {
        }

        public override void ReloadAll()
        {
            if (_folder == null)
                return;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                lock (_packages)
                {
                    InitialLoadState.Loading = true;
                    InitialLoadState.Loaded = 0;
                    InitialLoadState.Total = PackageHelper.EstimatePackageCount(_folder);
                    ScheduleHelper.SafeLog($"RELOADING ALL PACKAGES FROM {_folder}");

                    _packages.Clear();
                    lock (_watchers)
                    {
                        KillAllWatchers();
                        _watchers.Add(FileWatchHelper.WatchFolder(_folder, false, OnFileChange));
                    }
                    // Don't fetch for beta update
                    //_onlinePackages = new(); // Online not finished :(
                    _onlinePackages = TEMPOnlineHelper.FetchOnlinePackageList(onlinePkgSource).Result.ToList();
                    //var packages = PackageServerHelper.LoadServerPackages(_folder, _category, OnlinePackages, loadedPackage =>
                    var packages = TEMPOnlineHelper.LoadServerPackages(_folder, _category, OnlinePackages, loadedPackage =>
                    {
                        InitialLoadState.Loaded++;
                    }, _onLoadException).ToList();
                    ScheduleHelper.SafeLog($"(step 2)");
                    _packages.AddRange(packages);
                    lock (_downloadedFolders)
                    {
                        _downloadedFolders.Clear();
                        foreach (var package in _packages)
                        {
                            if (package.DownloadStatus != BeatmapDownloadStatus.Downloaded)
                                continue;
                            _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                            lock (_watchers)
                                _watchers.Add(FileWatchHelper.WatchFolder(package.BaseDirectory, true, OnFileChange));
                        }
                    }
                    InitialLoadState.Loading = false;
                }

                ArcadeHelper.ReloadArcadeList();
                ScheduleHelper.SafeInvoke(() =>
                    {
                        Songs.ForEach(s =>
                        {
                            if (s.Local)
                                s.Song.GetTexture();
                        });
                    });
                
            }).Start();
        }

        public void UpdatePackageTest(string folderPath)
        {
            UpdatePackage(folderPath);
        }

        protected override void UpdatePackage(string folderPath)
        {
            try
            {
                // Remove old package if there was one
                lock (_packages)
                {
                    var toRemove = _packages.FirstOrDefault(p => p.BaseDirectory == folderPath);
                    if (toRemove != null)
                    {
                        RemovePackage(folderPath);
                    }
                }

                if (!Directory.Exists(folderPath))
                {
                    // Reload here as a failsafe
                    PackageUpdated?.Invoke();
                    ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                    return;
                }

                // Weird fix for also getting the top folder
                List<string> dirs = Directory.EnumerateDirectories(folderPath, "*.*", SearchOption.AllDirectories).ToList();
                dirs.Add(folderPath);

                foreach (string subDir in dirs)
                {
                    //if (PackageServerHelper.TryLoadLocalServerPackage(subDir, _folder, out CustomPackageServer package, _category, false,
                    //_onLoadException, () => { return Packages.Where(p => p.DownloadStatus == BeatmapDownloadStatus.Downloaded).ToDictionary(p => p.GUID); } ))
                    if (TEMPOnlineHelper.TryLoadLocalServerPackage(subDir, _folder, out CustomPackageServer package, _category, false,
                    _onLoadException, () => { return Packages.Where(p => p.DownloadStatus == BeatmapDownloadStatus.Downloaded).ToDictionary(p =>
                    {
                        if (p.ServerURL != null)
                            return p.ServerURL;
                        return p.BaseDirectory;
                    }); }))
                    {
                        ScheduleHelper.SafeInvoke(() => package.SongDatas.ForEach(s => s.Song.GetTexture()));
                        ScheduleHelper.SafeLog($"UPDATING PACKAGE: {subDir}");
                        lock (_packages)
                        {

                            // Use online data if we can find it
                            //if (OnlinePackages.Any(o => o.GUID == package.GUID))
                            if (OnlinePackages.Any(o => package.BaseDirectory.Contains(o.FilePath.Substring("packages/".Length))))
                            {
                                //var opkg = OnlinePackages.First(o => o.GUID == package.GUID);
                                var opkg = OnlinePackages.First(o => package.BaseDirectory.Contains(o.FilePath.Substring("packages/".Length)));
                                //package.ServerURL = opkg.ServerURL;
                                package.ServerURL = opkg.FilePath;
                                package.Time = opkg.UploadTime;

                                //var toReplace = _packages.FindIndex(o => o.GUID == package.GUID);
                                var toReplace = _packages.FindIndex(o => o.ServerURL == package.ServerURL);
                                _packages[toReplace] = package;
                            }
                            // Add like normal otherwise
                            else
                            {
                                _packages.Add(package);
                            }
                            
                            //_packages.Add(package);

                            lock (_downloadedFolders)
                            {
                                if (package.DownloadStatus == BeatmapDownloadStatus.Downloaded)
                                    _downloadedFolders.Add(Path.GetFullPath(package.BaseDirectory));
                            }
                        }
                        PackageUpdated?.Invoke();
                        ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                    }
                    else
                    {
                        ScheduleHelper.SafeLog($"CANNOT find package: {subDir}");
                    }
                }
                
            } catch (Exception e)
            {
                ScheduleHelper.SafeInvoke(() => CustomBeatmaps.Log.LogError(e));
            }
            
        }

        protected override void RemovePackage(string folderPath)
        {
            lock (_packages)
            {
                string fullPath = Path.GetFullPath(folderPath);
                int toRemove = _packages.FindIndex(check => check.BaseDirectory == fullPath);
                if (toRemove != -1)
                {
                    var p = _packages[toRemove];
                    _packages.RemoveAt(toRemove);
                    lock (_downloadedFolders)
                        _downloadedFolders.Remove(folderPath);
                    //lock (_watchers)
                    //    _watchers.Remove(FileWatchHelper.WatchFolder(fullPath, true, OnFileChange));

                    //if (OnlinePackages.Exists(o => o.GUID == p.GUID) &&
                    //    PackageServerHelper.TryLoadOnlineServerPackage(OnlinePackages.First(o => o.GUID == p.GUID), out var package, _category, _onLoadException))
                    if (OnlinePackages.Exists(o => o.FilePath == p.ServerURL) &&
                        TEMPOnlineHelper.TryLoadOnlineServerPackage(OnlinePackages.First(o => o.FilePath == p.ServerURL),
                        out var package, _category, _onLoadException))
                    {
                        _packages.Add(package);
                    }

                    ScheduleHelper.SafeLog($"REMOVED PACKAGE: {folderPath}");
                    PackageUpdated?.Invoke();
                    ScheduleHelper.SafeInvoke(() => ArcadeHelper.ReloadArcadeList());
                }
                else
                {
                    ScheduleHelper.SafeLog($"CANNOT find package to remove: {folderPath}");
                }
            }
        }

        public override bool PackageExists(string folder)
        {
            lock (_downloadedFolders)
            {
                string targetFullPath = Path.GetFullPath(folder);
                return _downloadedFolders.Contains(targetFullPath);
            }
        }

        protected override void OnFileChange(FileSystemEventArgs evt)
        {
            string changedFilePath = Path.GetFullPath(evt.FullPath);
            // The root folder within the packages folder we consider to be a "package"
            string basePackageFolder = Path.GetFullPath(Path.Combine(_folder, StupidMissingTypesHelper.GetPathRoot(changedFilePath.Substring(_folder.Length + 1))));

            //
            try
            {
                lock (_watchers)
                {
                    var w = _watchers.First(w => w.Path == basePackageFolder);
                    w.Dispose();
                    _watchers.Remove(w);
                }
            }
            catch (Exception e)
            {
                CustomBeatmaps.Log.LogError(e);
            }
            //*/

            ScheduleHelper.SafeLog($"Base Package Folder IN SERVER: {basePackageFolder}");

            // Special case: Root package folder is deleted, we delete a package.
            if (evt.ChangeType == WatcherChangeTypes.Deleted && basePackageFolder == changedFilePath)
            {
                ScheduleHelper.SafeLog($"Server Package DELETE: {basePackageFolder}");
                RemovePackage(basePackageFolder);
                _dontLoad.Add(basePackageFolder);
                return;
            }

            ScheduleHelper.SafeLog($"Server Package Change: {evt.ChangeType}: {basePackageFolder} ");

            lock (_loadQueue)
            {
                // We should refresh queued packages in bulk.
                bool isFirst = _loadQueue.Count == 0;
                if (!_loadQueue.Contains(basePackageFolder))
                {
                    _loadQueue.Enqueue(basePackageFolder);
                }

                if (isFirst)
                {
                    // Wait for potential other loads to come in
                    Task.Run(async () =>
                    {
                        await Task.Delay(400);
                        RefreshQueuedPackages();
                    });
                }
            }

        }

    }
}
