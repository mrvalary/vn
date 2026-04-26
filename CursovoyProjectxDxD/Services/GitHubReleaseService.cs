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
    /// <summary>
    /// Сервис проверки последнего релиза приложения на GitHub.
    /// </summary>
    public sealed class GitHubReleaseService
    {
        #region Fields

        // HTTP-клиент для обращения к GitHub API.
        private readonly HttpClient _httpClient;
        // YAML-настройки хранят только безопасные параметры обновления.
        private readonly YamlAppSettings _settings;

        #endregion

        #region Constructor

        /// <summary>
        /// Создает сервис проверки релизов и настраивает заголовки GitHub API.
        /// </summary>
        /// <param name="httpClient">HTTP-клиент для запросов к GitHub.</param>
        /// <param name="settings">Безопасные YAML-настройки обновлений.</param>
        public GitHubReleaseService(HttpClient httpClient, YamlAppSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("vn-client", "1.0.0"));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Проверяет, доступна ли версия приложения новее текущей.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены HTTP-запроса.</param>
        /// <returns>Информация о текущей версии, последнем релизе и архиве обновления.</returns>
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            string currentVersion = AppVersionProvider.GetCurrentVersion();
            // owner/repo берутся из vn.yml, чтобы репозиторий можно было сменить без пересборки.
            string url = "https://api.github.com/repos/" + _settings.UpdateOwner + "/" + _settings.UpdateRepo + "/releases/latest";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            using (response)
            {
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);

                if (release == null)
                {
                    throw new InvalidOperationException("GitHub release response is empty.");
                }

                string latestVersion = NormalizeVersion(release.TagName);
                // Тип архива тоже вынесен в YAML, но по умолчанию остается .zip.
                var asset = release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(_settings.UpdateAssetExtension, StringComparison.OrdinalIgnoreCase));

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

        #endregion

        #region Version Helpers

        /// <summary>
        /// Удаляет префикс v из тега релиза.
        /// </summary>
        /// <param name="tag">Тег релиза GitHub.</param>
        /// <returns>Версия без префикса v.</returns>
        private static string NormalizeVersion(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return tag.Substring(1);
            }

            return tag ?? string.Empty;
        }

        /// <summary>
        /// Сравнивает две версии как System.Version.
        /// </summary>
        /// <param name="left">Первая версия.</param>
        /// <param name="right">Вторая версия.</param>
        /// <returns>Положительное число, если left новее right; ноль при равенстве; отрицательное число, если left старее.</returns>
        private static int CompareVersions(string left, string right)
        {
            return Version.Parse(left).CompareTo(Version.Parse(right));
        }

        #endregion
    }
}
