using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    public sealed class ReleaseDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly FileLogger _logger;

        public ReleaseDownloadService(HttpClient httpClient, FileLogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> DownloadAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken)
        {
            string versionDirectory = Path.Combine(Path.GetTempPath(), "vn-installer", updateInfo.LatestVersion);
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            string zipPath = Path.Combine(versionDirectory, updateInfo.AssetName);
            _logger.Info("Downloading release archive to: " + zipPath);

            using (HttpResponseMessage response = await _httpClient.GetAsync(updateInfo.DownloadUrl, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (FileStream fileStream = File.Create(zipPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            _logger.Info("Release archive downloaded.");
            return zipPath;
        }
    }
}
