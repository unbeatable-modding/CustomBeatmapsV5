using CustomBeatmaps.CustomPackages;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomBeatmaps.CustomData
{
    public class PackageManagerSubmission : PackageManagerServer
    {
        // VERY similar to PackageManagerServer so might as well use all of it
        protected override string onlinePkgSource => CustomBeatmaps.BackendConfig.ServerSubmissionList;

        public PackageManagerSubmission(Action<BeatmapException> onLoadException) : base(onLoadException)
        {
        }
        
    }
}
