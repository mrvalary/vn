using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Services
{
    // Сервис проверки последнего релиза приложения на GitHub.
    public sealed class GitHubReleaseService
    {
        // HTTP-клиент для обращения к GitHub API.
        private readonly HttpClient _httpClient;

        // Владелец репозитория.
        private const string Owner = "mrvalary";
        // Имя репозитория.
        private const string Repo = "vn";

        // Настраиваем клиент один раз при создании сервиса.
        public GitHubReleaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("vn-client", "1.0.0"));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        // Проверяет, доступна ли версия новее текущей.
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            string currentVersion = AppVersionProvider.GetCurrentVersion();
            string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            using (response)
            {
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);

                if (release == null)
                    throw new InvalidOperationException("GitHub release response is empty.");

                string latestVersion = NormalizeVersion(release.TagName);
                var asset = release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                if (asset == null)
                {
                    return new AppUpdateInfo
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        IsAvailable = false,
                        AssetName = string.Empty,
                        DownloadUrl = string.Empty
                    };
                }

                return new AppUpdateInfo
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    IsAvailable = CompareVersions(latestVersion, currentVersion) > 0,
                    AssetName = asset.Name,
                    DownloadUrl = asset.BrowserDownloadUrl
                };
            }
        }

        // Удаляет префикс v из тега релиза.
        private static string NormalizeVersion(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return tag.Substring(1);

            return tag ?? string.Empty;
        }

        // Сравнивает две версии как System.Version.
        private static int CompareVersions(string left, string right)
        {
            return Version.Parse(left).CompareTo(Version.Parse(right));
        }
    }
}
