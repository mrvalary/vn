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
            // Сохраняем клиент.
            _httpClient = httpClient;
            // GitHub требует User-Agent.
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("vn-client", "1.0.0"));
            // Запрашиваем стандартный JSON.
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        // Проверяет, доступна ли версия новее текущей.
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем локальную версию приложения.
            string currentVersion = GetCurrentVersion();
            // Формируем URL latest release.
            string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";

            // Делаем HTTP-запрос.
            var response = await _httpClient.GetAsync(url, cancellationToken);
            using (response)
            {
                // Ошибочный код ответа преобразуем в исключение.
                response.EnsureSuccessStatusCode();

                // Читаем JSON релиза.
                string json = await response.Content.ReadAsStringAsync();
                // Десериализуем JSON в DTO.
                var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);

                // Если JSON не разобрался, считаем ответ некорректным.
                if (release == null)
                    throw new InvalidOperationException("GitHub release response is empty.");

                // Нормализуем тег релиза к виду 1.2.3.
                string latestVersion = NormalizeVersion(release.TagName);

                // Ищем zip-архив среди файлов релиза.
                var asset = release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                // Если zip-архив не найден, обновление считаем недоступным.
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

                // Возвращаем итоговую модель проверки обновления.
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

        // Читает текущую версию из assembly.
        private static string GetCurrentVersion()
        {
            // Получаем объект Version.
            Version version = typeof(GitHubReleaseService).Assembly.GetName().Version;
            // Приводим к формату major.minor.build.
            return version != null ? version.ToString(3) : "1.0.0";
        }

        // Удаляет префикс v из тега релиза.
        private static string NormalizeVersion(string tag)
        {
            // Если тег начинается с v, убираем его.
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return tag.Substring(1);

            // Иначе возвращаем исходное значение.
            return tag;
        }

        // Сравнивает две версии как System.Version.
        private static int CompareVersions(string left, string right)
        {
            return Version.Parse(left).CompareTo(Version.Parse(right));
        }
    }
}
