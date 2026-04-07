using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Models
{
    public sealed class AppUpdateInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
