using System.Diagnostics;
using System.IO;

namespace VnInstaller.Services
{
    /// <summary>
    /// Сервис запуска основного приложения после установки.
    /// </summary>
    public sealed class AppStarterService
    {
        // Логгер нужен для фиксации запуска.
        private readonly FileLogger _logger;

        /// <summary>
        /// Создает сервис запуска приложения.
        /// </summary>
        /// <param name="logger">Логгер установщика.</param>
        public AppStarterService(FileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Запускает основное приложение по указанному пути.
        /// </summary>
        /// <param name="appExePath">Полный путь к exe основного приложения.</param>
        public void Start(string appExePath)
        {
            // Определяем рабочую директорию процесса.
            string workingDirectory = Path.GetDirectoryName(appExePath);
            // Пишем в лог информацию о запуске.
            _logger.Info("Starting application: " + appExePath);

            // Стартуем приложение.
            Process.Start(new ProcessStartInfo
            {
                FileName = appExePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });
        }
    }
}
