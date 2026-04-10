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
    // Сервис чтения latest release для установщика.
    public sealed class GitHubReleaseService
    {
        // HTTP-клиент для обращений к GitHub API.
        private readonly HttpClient _httpClient;

        // Владелец репозитория.
        private const string Owner = "mrvalary";
        // Имя репозитория.
        private const string Repo = "vn";

        // Настраиваем клиент один раз при создании сервиса.
        public GitHubReleaseService(HttpClient httpClient)
        {
            // Сохраняем экземпляр клиента.
            _httpClient = httpClient;

            // Если User-Agent ещё не задан, добавляем его.
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("vn-installer", "1.0.0"));
            }

            // Если Accept-заголовок ещё не задан, добавляем его.
            if (!_httpClient.DefaultRequestHeaders.Accept.Any())
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            }
        }

        // Получает последний релиз и выбирает zip-архив приложения.
        public async Task<AppUpdateInfo> GetLatestReleaseAsync(CancellationToken cancellationToken)
        {
            // Формируем URL latest release.
            string url = "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";
            // Выполняем GET-запрос.
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            using (response)
            {
                // Ошибки HTTP поднимаем как исключения.
                response.EnsureSuccessStatusCode();

                // Читаем JSON ответа.
                string json = await response.Content.ReadAsStringAsync();
                // Десериализуем JSON в DTO.
                GitHubReleaseDto release = JsonSerializer.Deserialize<GitHubReleaseDto>(json);
                // Пустой результат считаем ошибкой.
                if (release == null)
                {
                    throw new InvalidOperationException("GitHub release response is empty.");
                }

                // Ищем zip-архив среди ассетов релиза.
                GitHubAssetDto asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                // Если zip-файл не найден, сообщаем об ошибке.
                if (asset == null)
                {
                    throw new InvalidOperationException("В релизе отсутствует zip-архив.");
                }

                // Возвращаем данные, нужные для скачивания и установки.
                return new AppUpdateInfo
                {
                    LatestVersion = NormalizeVersion(release.TagName),
                    AssetName = asset.Name,
                    DownloadUrl = asset.BrowserDownloadUrl
                };
            }
        }

        // Приводит тег релиза к виду без префикса v.
        private static string NormalizeVersion(string tag)
        {
            // Если тег начинается с v, отрезаем его.
            if (!string.IsNullOrWhiteSpace(tag) && tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return tag.Substring(1);
            }

            // Иначе возвращаем тег как есть или пустую строку.
            return tag ?? string.Empty;
        }
    }
}
