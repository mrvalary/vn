using CursovoyProjectxDxD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace CursovoyProjectxDxD.Services
{
    public sealed class GitHubReleaseService
    {
        private readonly HttpClient _httpClient;

        // Замени на свой репозиторий
        private const string Owner = "YOUR_GITHUB_LOGIN";
        private const string Repo = "vn";

        public GitHubReleaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("vn-client", "1.0.0"));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            string currentVersion = GetCurrentVersion();
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

        private static string GetCurrentVersion()
        {
            return typeof(GitHubReleaseService).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        }

        private static string NormalizeVersion(string tag)
        {
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return tag.Substring(1);

            return tag;
        }

        private static int CompareVersions(string left, string right)
        {
            return Version.Parse(left).CompareTo(Version.Parse(right));
        }
    }
}
