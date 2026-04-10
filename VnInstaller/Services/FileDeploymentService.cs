using System;
using System.IO;
using System.Threading;

namespace VnInstaller.Services
{
    // Сервис копирования файлов релиза в целевую папку приложения.
    public sealed class FileDeploymentService
    {
        // Логгер нужен для фиксации этапов копирования.
        private readonly FileLogger _logger;

        // Получаем логгер через конструктор.
        public FileDeploymentService(FileLogger logger)
        {
            _logger = logger;
        }

        // Разворачивает распакованные файлы в целевую папку.
        public void Deploy(string sourceDirectory, string targetDirectory)
        {
            // Пишем старт операции в лог.
            _logger.Info("Deploying files from " + sourceDirectory + " to " + targetDirectory);

            // Сначала создаём всю структуру каталогов.
            string[] directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < directories.Length; i++)
            {
                // Получаем относительный путь каталога.
                string relativeDirectory = directories[i].Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Формируем путь каталога назначения.
                string targetSubDirectory = Path.Combine(targetDirectory, relativeDirectory);

                // Если каталога ещё нет, создаём его.
                if (!Directory.Exists(targetSubDirectory))
                {
                    Directory.CreateDirectory(targetSubDirectory);
                }
            }

            // После каталогов копируем все файлы.
            string[] files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                // Получаем имя файла.
                string fileName = Path.GetFileName(files[i]);
                // Некоторые файлы пропускаем по правилу исключения.
                if (ShouldSkip(fileName))
                {
                    _logger.Info("Skipped file: " + files[i]);
                    continue;
                }

                // Получаем относительный путь файла.
                string relativeFile = files[i].Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Формируем путь назначения.
                string targetFile = Path.Combine(targetDirectory, relativeFile);
                // Определяем каталог файла назначения.
                string targetFileDirectory = Path.GetDirectoryName(targetFile);

                // Создаём каталог назначения, если он отсутствует.
                if (!Directory.Exists(targetFileDirectory))
                {
                    Directory.CreateDirectory(targetFileDirectory);
                }

                // Копируем файл с повторными попытками.
                CopyFileWithRetry(files[i], targetFile);
            }

            // Пишем в лог завершение развёртывания.
            _logger.Info("Deployment completed.");
        }

        // Копирует файл с несколькими повторными попытками.
        private void CopyFileWithRetry(string sourceFile, string targetFile)
        {
            // Делаем до пяти попыток копирования.
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    // Пытаемся скопировать файл с заменой.
                    File.Copy(sourceFile, targetFile, true);
                    // При успехе фиксируем это в логе.
                    _logger.Info("Copied: " + sourceFile + " -> " + targetFile);
                    return;
                }
                catch (IOException ex)
                {
                    // Временная блокировка файла даёт право на повтор.
                    _logger.Info("Copy attempt " + attempt + " failed for " + targetFile + ". " + ex.Message);
                    Thread.Sleep(1000);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Ошибка доступа тоже иногда бывает временной.
                    _logger.Info("Copy attempt " + attempt + " failed for " + targetFile + ". " + ex.Message);
                    Thread.Sleep(1000);
                }
            }

            // После пяти попыток делаем последнюю без подавления ошибки.
            File.Copy(sourceFile, targetFile, true);
            // Если дошли сюда, копирование всё же удалось.
            _logger.Info("Copied after retries: " + sourceFile + " -> " + targetFile);
        }

        // Определяет, нужно ли пропустить файл.
        private static bool ShouldSkip(string fileName)
        {
            // Лог текущего запуска не должен затираться содержимым архива.
            return string.Equals(fileName, "vn-installer.log", StringComparison.OrdinalIgnoreCase);
        }
    }
}
