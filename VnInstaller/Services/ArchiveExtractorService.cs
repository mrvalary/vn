using System.IO;
using System.IO.Compression;

namespace VnInstaller.Services
{
    /// <summary>
    /// Сервис распаковки zip-архива релиза.
    /// </summary>
    public sealed class ArchiveExtractorService
    {
        // Логгер нужен для записи этапов распаковки.
        private readonly FileLogger _logger;

        /// <summary>
        /// Создает сервис распаковки архива.
        /// </summary>
        /// <param name="logger">Логгер установщика.</param>
        public ArchiveExtractorService(FileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Распаковывает архив и возвращает путь к папке с файлами.
        /// </summary>
        /// <param name="archivePath">Путь к zip-архиву релиза.</param>
        /// <param name="version">Версия релиза для формирования временной папки.</param>
        /// <returns>Путь к папке с распакованными файлами.</returns>
        public string Extract(string archivePath, string version)
        {
            // Формируем временную папку под конкретную версию.
            string versionDirectory = Path.Combine(Path.GetTempPath(), "vn-installer", version);
            // Внутри неё создаём папку extracted.
            string extractDirectory = Path.Combine(versionDirectory, "extracted");

            // Если такая папка уже есть, удаляем старое содержимое.
            if (Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, true);
            }

            // Создаём чистую директорию распаковки.
            Directory.CreateDirectory(extractDirectory);

            // Пишем в лог начало операции.
            _logger.Info("Extracting archive: " + archivePath);
            // Распаковываем zip-файл.
            ZipFile.ExtractToDirectory(archivePath, extractDirectory);
            // Пишем в лог результат операции.
            _logger.Info("Archive extracted to: " + extractDirectory);

            // Возвращаем путь к распакованным файлам.
            return extractDirectory;
        }
    }
}
