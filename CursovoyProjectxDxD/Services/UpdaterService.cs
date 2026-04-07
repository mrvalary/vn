using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;

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

        public async Task<bool> ApplyUpdateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            AppUpdateInfo info = await _releaseService.CheckForUpdateAsync(cancellationToken);

            if (!info.IsAvailable)
            {
                Console.WriteLine("Обновление не требуется.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(info.DownloadUrl))
            {
                Console.WriteLine("Ссылка на обновление отсутствует.");
                return false;
            }

            string tempRoot = Path.Combine(Path.GetTempPath(), "vn-update");
            if (!Directory.Exists(tempRoot))
            {
                Directory.CreateDirectory(tempRoot);
            }

            string versionFolder = Path.Combine(tempRoot, info.LatestVersion);
            if (!Directory.Exists(versionFolder))
            {
                Directory.CreateDirectory(versionFolder);
            }

            string zipPath = Path.Combine(versionFolder, info.AssetName);

            Console.WriteLine("Скачивание обновления...");
            Console.WriteLine("Версия: " + info.LatestVersion);
            Console.WriteLine("Файл: " + info.AssetName);
            Console.WriteLine("Путь: " + zipPath);

            using (var response = await _httpClient.GetAsync(info.DownloadUrl, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var fs = File.Create(zipPath))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            Console.WriteLine("Обновление успешно скачано.");
            Console.WriteLine("Файл сохранён: " + zipPath);

            return true;
        }
    }
}