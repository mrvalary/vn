using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
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

            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Таймаут вынесен в vn.yml, чтобы зависший GitHub-запрос не блокировал запуск приложения слишком долго.
                timeout.CancelAfter(TimeSpan.FromSeconds(_settings.UpdateHttpTimeoutSeconds));

                var response = await _httpClient.GetAsync(url, timeout.Token);
                using (response)
                {
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);

                    if (release == null)
                    {
                        throw new InvalidOperationException("GitHub вернул пустой ответ о релизе.");
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
                            ReleaseName = release.Name,
                            ReleaseNotes = release.Body,
                            IsAvailable = false,
                            AssetName = string.Empty,
                            DownloadUrl = string.Empty
                        };
                    }

                    return new AppUpdateInfo
                    {
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        ReleaseName = release.Name,
                        ReleaseNotes = release.Body,
                        IsAvailable = CanCompareVersions(latestVersion, currentVersion) &&
                            CompareVersions(latestVersion, currentVersion) > 0,
                        AssetName = asset.Name,
                        DownloadUrl = asset.BrowserDownloadUrl
                    };
                }
            }
        }

        #endregion

        #region Version Helpers

        /// <summary>
        /// Извлекает номер версии из тега релиза.
        /// </summary>
        /// <param name="tag">Тег релиза GitHub.</param>
        /// <returns>Версия без текстовых приставок или исходная строка, если номер не найден.</returns>
        private static string NormalizeVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            // GitHub-тег может быть v1.3.0, 1.3.0 или даже "Release version 1.3.0".
            // Для сравнения берём первую похожую на версию числовую часть.
            Match match = Regex.Match(tag, @"\d+(\.\d+){1,3}");
            return match.Success ? match.Value : tag.Trim();
        }

        /// <summary>
        /// Сравнивает две версии как System.Version.
        /// </summary>
        /// <param name="left">Первая версия.</param>
        /// <param name="right">Вторая версия.</param>
        /// <returns>Положительное число, если left новее right; ноль при равенстве; отрицательное число, если left старее.</returns>
        private static int CompareVersions(string left, string right)
        {
            return ToVersion(left).CompareTo(ToVersion(right));
        }

        /// <summary>
        /// Проверяет, можно ли безопасно сравнить две строки версий.
        /// </summary>
        /// <param name="left">Первая версия.</param>
        /// <param name="right">Вторая версия.</param>
        /// <returns>true, если обе строки удалось привести к System.Version.</returns>
        private static bool CanCompareVersions(string left, string right)
        {
            return TryToVersion(left, out Version _) && TryToVersion(right, out Version _);
        }

        /// <summary>
        /// Преобразует строку версии в System.Version.
        /// </summary>
        /// <param name="value">Строковое значение версии.</param>
        /// <returns>Объект Version.</returns>
        private static Version ToVersion(string value)
        {
            Version version;
            if (!TryToVersion(value, out version))
            {
                return new Version(0, 0, 0);
            }

            return version;
        }

        /// <summary>
        /// Мягко разбирает версию и дополняет отсутствующие части нулями.
        /// </summary>
        /// <param name="value">Строка версии.</param>
        /// <param name="version">Разобранная версия.</param>
        /// <returns>true, если версия успешно разобрана.</returns>
        private static bool TryToVersion(string value, out Version version)
        {
            version = null;
            string normalized = NormalizeVersion(value);
            Match match = Regex.Match(normalized, @"^(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?$");
            if (!match.Success)
            {
                return false;
            }

            int major = int.Parse(match.Groups[1].Value);
            int minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int revision = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
            version = new Version(major, minor, build, revision);
            return true;
        }

        #endregion
    }
}
