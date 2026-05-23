using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    /// <summary>
    /// Сервис скачивания zip-архива релиза.
    /// </summary>
    public sealed class ReleaseDownloadService
    {
        // HTTP-клиент для скачивания файла.
        private readonly HttpClient _httpClient;
        // Логгер этапа скачивания.
        private readonly FileLogger _logger;

        /// <summary>
        /// Создает сервис скачивания архива релиза.
        /// </summary>
        /// <param name="httpClient">HTTP-клиент для скачивания файла.</param>
        /// <param name="logger">Логгер установщика.</param>
        public ReleaseDownloadService(HttpClient httpClient, FileLogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Скачивает архив релиза и возвращает путь к файлу.
        /// </summary>
        /// <param name="updateInfo">Информация о релизе и ссылке на архив.</param>
        /// <param name="cancellationToken">Токен отмены скачивания.</param>
        /// <returns>Путь к скачанному архиву.</returns>
        public async Task<string> DownloadAsync(AppUpdateInfo updateInfo, CancellationToken cancellationToken)
        {
            // Формируем временную папку под конкретную версию.
            string versionDirectory = Path.Combine(Path.GetTempPath(), "vn-installer", updateInfo.LatestVersion);
            // Если папки ещё нет, создаём её.
            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
            }

            // Формируем полный путь к zip-файлу.
            string zipPath = Path.Combine(versionDirectory, updateInfo.AssetName);
            // Пишем путь в лог.
            _logger.Info("Downloading release archive to: " + zipPath);

            // Запрашиваем файл по прямой ссылке.
            using (HttpResponseMessage response = await _httpClient.GetAsync(updateInfo.DownloadUrl, cancellationToken))
            {
                // Ошибочный ответ HTTP поднимаем как исключение.
                response.EnsureSuccessStatusCode();

                // Создаём файл на диске.
                using (FileStream fileStream = File.Create(zipPath))
                {
                    // Переносим содержимое ответа в файл.
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            // Фиксируем завершение скачивания.
            _logger.Info("Release archive downloaded.");
            // Возвращаем путь к архиву.
            return zipPath;
        }
    }
}
