using System.Diagnostics;
using System.IO;

namespace VnInstaller.Services
{
    // Сервис запуска основного приложения после установки.
    public sealed class AppStarterService
    {
        // Логгер нужен для фиксации запуска.
        private readonly FileLogger _logger;

        // Получаем логгер через конструктор.
        public AppStarterService(FileLogger logger)
        {
            _logger = logger;
        }

        // Запускает основное приложение по указанному пути.
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
