using CursovoyProjectxDxD.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Services
{
    public sealed class UpdaterService
    {
        private readonly GitHubReleaseService _releaseService;
        private readonly HttpClient _httpClient;

        public UpdaterService(GitHubReleaseService releaseService, HttpClient httpClient)
        {
            _releaseService = releaseService;
            _httpClient = httpClient;
        }

        public async Task<bool> ApplyUpdateAsync(CancellationToken cancellationToken = default)
        {
            AppUpdateInfo info = await _releaseService.CheckForUpdateAsync(cancellationToken);

            if (!info.IsAvailable || string.IsNullOrWhiteSpace(info.DownloadUrl))
                return false;

            string tempDir = Path.Combine(Path.GetTempPath(), "vn-update");
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, info.AssetName);

            using (var response = await _httpClient.GetAsync(info.DownloadUrl, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var fs = File.Create(zipPath))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            // Пока MVP: просто скачали архив.
            // Следующий шаг — распаковка и запуск updater.exe.
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = tempDir,
                UseShellExecute = true
            });

            return true;
        }
    }
}
