using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    public sealed class GitHubReleaseService
    {
        private readonly HttpClient _httpClient;

        private const string Owner = "mrvalary";
        private const string Repo = "vn";

        public GitHubReleaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("vn-installer", "1.0.0"));
            }

            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            }
        }

        public async Task<AppUpdateInfo> GetLatestReleaseAsync(CancellationToken cancellationToken)
        {
            string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            using (response)
            {
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                GitHubReleaseDto release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);
                if (release == null)
                {
                    throw new InvalidOperationException("GitHub release response is empty.");
                }

                GitHubAssetDto asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (asset == null)
                {
                    throw new InvalidOperationException("В релизе отсутствует zip-архив.");
                }

                return new AppUpdateInfo
                {
                    LatestVersion = NormalizeVersion(release.TagName),
                    AssetName = asset.Name,
                    DownloadUrl = asset.BrowserDownloadUrl
                };
            }
        }

        private static string NormalizeVersion(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return tag.Substring(1);
            }

            return tag ?? string.Empty;
        }
    }
}
