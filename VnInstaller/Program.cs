using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VnInstaller.Models;
using VnInstaller.Services;

namespace VnInstaller
{
    /// <summary>
    /// Точка входа отдельного приложения-установщика.
    /// </summary>
    internal class Program
    {
        // Имя корневой папки установки.
        private const string InstallFolderName = "vn";
        // Имя папки приложения внутри каталога установки.
        private const string AppFolderName = "app";
        // Имя exe-файла основного приложения.
        private const string AppExeName = "CursovoyProjectxDxD.exe";
        // Имя служебного файла с PID обновляемого процесса.
        private const string ProcessIdFileName = "vn-app.pid";

        /// <summary>
        /// Синхронная точка входа установщика для .NET Framework.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        /// <returns>Код завершения установщика.</returns>
        private static int Main(string[] args)
        {
            try
            {
                return MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка установщика: " + ex.Message);
                WaitForUser();
                return 1;
            }
        }

        /// <summary>
        /// Выполняет основной сценарий работы установщика в единственном режиме.
        /// </summary>
        /// <returns>Код завершения установщика.</returns>
        private static async Task<int> MainAsync()
        {
            // Название окна нужно, чтобы отличать установщик от основного приложения.
            Console.Title = "vn-installer";

            // Вычисляем стандартную папку установки.
            string targetDirectory = GetTargetDirectory();
            // Вычисляем полный путь к exe приложения после установки.
            string appExePath = Path.Combine(targetDirectory, AppExeName);
            // Пытаемся считать PID обновляемого приложения из служебного файла.
            int? appProcessId = TryReadAppProcessId();
            // Формируем путь к логу установщика.
            string logFilePath = Path.Combine(targetDirectory, "vn-installer.log");

            // Создаём контейнер сервисов установщика.
            ServiceProvider serviceProvider = ConfigureServices(logFilePath);
            // Получаем логгер.
            FileLogger logger = serviceProvider.GetRequiredService<FileLogger>();
            // Получаем координатор сценария установки.
            InstallerOrchestrator orchestrator = serviceProvider.GetRequiredService<InstallerOrchestrator>();

            try
            {
                // Показываем пользователю стартовую информацию.
                Console.WriteLine("Запущен установщик vn.");
                Console.WriteLine("Будет установлена последняя релизная версия основного приложения.");
                Console.WriteLine();

                // Пишем стартовые данные в лог для диагностики.
                logger.Info("VnInstaller started.");
                logger.Info("Target directory: " + targetDirectory);
                logger.Info("App exe path: " + appExePath);
                logger.Info("Process id: " + (appProcessId.HasValue ? appProcessId.Value.ToString() : "not specified"));

                // Выполняем полный сценарий скачивания, распаковки, копирования и запуска.
                AppUpdateInfo updateInfo = await orchestrator.ExecuteAsync(targetDirectory, appExePath, appProcessId);

                // Фиксируем успешное завершение в логе.
                logger.Info("VnInstaller finished successfully.");

                Console.WriteLine();
                // Показываем установленную версию.
                Console.WriteLine("Установка успешно выполнена. Установлена версия " + updateInfo.LatestVersion + ".");
                // Показываем путь, куда установлены файлы.
                Console.WriteLine("Приложение установлено в: " + targetDirectory);
                // Сообщаем, что после установки приложение уже запущено.
                Console.WriteLine("Основное приложение уже запущено.");
                WaitForUser();
                return 0;
            }
            catch (Exception ex)
            {
                // Любая ошибка попадает в лог для последующей диагностики.
                logger.Error("Installer failed.", ex);
                Console.WriteLine();
                Console.WriteLine("Ошибка установки: " + ex.Message);
                Console.WriteLine("Подробности записаны в лог: " + logFilePath);
                Console.WriteLine("Целевая папка установки: " + targetDirectory);
                WaitForUser();
                return 1;
            }
        }

        /// <summary>
        /// Конфигурирует все сервисы установщика.
        /// </summary>
        /// <param name="logFilePath">Путь к файлу лога установщика.</param>
        /// <returns>Готовый контейнер сервисов установщика.</returns>
        private static ServiceProvider ConfigureServices(string logFilePath)
        {
            // Создаём коллекцию регистраций сервисов.
            ServiceCollection services = new ServiceCollection();

            // Один HttpClient используется для всех HTTP-запросов установщика.
            services.AddSingleton(new HttpClient());
            // Логгер создаётся сразу с готовым путём к файлу лога.
            services.AddSingleton(new FileLogger(logFilePath));
            // Сервис чтения информации о релизе.
            services.AddSingleton<GitHubReleaseService>();
            // Сервис скачивания zip-архива релиза.
            services.AddSingleton<ReleaseDownloadService>();
            // Сервис распаковки архива.
            services.AddSingleton<ArchiveExtractorService>();
            // Сервис ожидания завершения основного процесса.
            services.AddSingleton<ProcessWaitService>();
            // Сервис копирования файлов в каталог установки.
            services.AddSingleton<FileDeploymentService>();
            // Сервис запуска основного приложения после установки.
            services.AddSingleton<AppStarterService>();
            // Оркестратор всего сценария установки.
            services.AddSingleton<InstallerOrchestrator>();

            // Собираем и возвращаем контейнер сервисов.
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Возвращает стандартную папку установки приложения.
        /// </summary>
        /// <returns>Путь к целевой папке приложения.</returns>
        private static string GetTargetDirectory()
        {
            // Получаем LocalAppData текущего пользователя.
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Формируем итоговую папку установки.
            return Path.Combine(localAppData, InstallFolderName, AppFolderName);
        }

        /// <summary>
        /// Читает PID обновляемого процесса из служебного файла рядом с установщиком.
        /// </summary>
        /// <returns>PID процесса приложения или null, если PID отсутствует.</returns>
        private static int? TryReadAppProcessId()
        {
            // Служебный файл лежит рядом с временной копией установщика.
            string processIdFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProcessIdFileName);
            // Если файла нет, значит установщик запущен вручную и ждать нечего.
            if (!File.Exists(processIdFilePath))
            {
                return null;
            }

            // Читаем текст PID из файла.
            string content = File.ReadAllText(processIdFilePath).Trim();
            // Пытаемся преобразовать его в число.
            int processId;
            if (!int.TryParse(content, out processId))
            {
                return null;
            }

            return processId;
        }

        /// <summary>
        /// Даёт пользователю время прочитать итоговый текст.
        /// </summary>
        private static void WaitForUser()
        {
            // Пустая строка визуально отделяет основное сообщение.
            Console.WriteLine();
            // Не закрываем окно сразу, чтобы пользователь успел прочитать результат.
            Console.WriteLine("Нажмите Enter, чтобы закрыть установщик.");
            Console.ReadLine();
        }
    }
}
