using System;
using CustomBeatmaps.CustomPackages;

using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;
using Directory = Pri.LongPath.Directory;
using CustomBeatmaps.Util.CustomData;

namespace CustomBeatmaps.Util
{
    public static class EditorHelper
    {

        public static string CreateExportZipFile(CustomPackage pkg, string temporaryFolderLocation)
        {
            if (!Directory.Exists(temporaryFolderLocation))
                Directory.CreateDirectory(temporaryFolderLocation);

            // Zip
            string packageName = pkg.Name;

            string zipTarget = $"{temporaryFolderLocation}/{packageName}.zip";

            ZipHelper.CreateFromDirectory(pkg.BaseDirectory, zipTarget);

            return zipTarget;
        }
    }
}
