using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            EnsureDirectory(tempRoot);

            string versionFolder = Path.Combine(tempRoot, info.LatestVersion);
            EnsureDirectory(versionFolder);

            string zipPath = Path.Combine(versionFolder, info.AssetName);
            string extractPath = Path.Combine(versionFolder, "extracted");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            Console.WriteLine("Скачивание обновления...");
            Console.WriteLine("Версия: " + info.LatestVersion);
            Console.WriteLine("Файл: " + info.AssetName);

            using (var response = await _httpClient.GetAsync(info.DownloadUrl, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var fs = File.Create(zipPath))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            Console.WriteLine("Архив скачан: " + zipPath);
            Console.WriteLine("Распаковка обновления...");

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            Console.WriteLine("Файлы распакованы в: " + extractPath);

            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string appDirectory = Path.GetDirectoryName(currentExePath);
            string updaterExePath = Path.Combine(appDirectory, "vn-updater.exe");

            if (!File.Exists(updaterExePath))
            {
                Console.WriteLine("Файл vn-updater.exe не найден: " + updaterExePath);
                return false;
            }

            int currentProcessId = Process.GetCurrentProcess().Id;

            string arguments =
                "\"" + appDirectory + "\" " +
                "\"" + extractPath + "\" " +
                "\"" + currentExePath + "\" " +
                currentProcessId.ToString();

            Console.WriteLine("Запуск updater...");
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterExePath,
                Arguments = arguments,
                UseShellExecute = true
            });

            Console.WriteLine("Основное приложение будет закрыто для завершения обновления.");
            Environment.Exit(0);
            return true;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}