using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VnInstaller.Core;
using VnInstaller.Models;
using VnInstaller.Services;

namespace VnInstaller
{
    // Точка входа отдельного приложения-установщика.
    internal class Program
    {
        // Синхронная точка входа для .NET Framework.
        private static int Main(string[] args)
        {
            try
            {
                // Весь основной сценарий вынесен в асинхронный метод.
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Критическая ошибка верхнего уровня показывается пользователю.
                Console.WriteLine("Критическая ошибка установщика: " + ex.Message);
                WaitForUser();
                return 1;
            }
        }

        // Основной сценарий работы установщика.
        private static async Task<int> MainAsync(string[] args)
        {
            // Переменная для разобранных аргументов.
            InstallerArguments installerArguments;
            // Переменная для текста ошибки разбора.
            string parseError;

            // Проверяем корректность аргументов запуска.
            if (!InstallerArguments.TryParse(args, out installerArguments, out parseError))
            {
                Console.WriteLine(parseError);
                WaitForUser();
                return 1;
            }

            // Создаём контейнер сервисов установщика.
            ServiceProvider serviceProvider = ConfigureServices(installerArguments);
            // Получаем файловый логгер.
            FileLogger logger = serviceProvider.GetRequiredService<FileLogger>();
            // Получаем оркестратор установки.
            InstallerOrchestrator orchestrator = serviceProvider.GetRequiredService<InstallerOrchestrator>();

            try
            {
                // Для ручной установки показываем один текст.
                if (installerArguments.IsManualInstall)
                {
                    Console.WriteLine("Запущен установщик vn.");
                    Console.WriteLine("Будет установлена последняя релизная версия основного приложения.");
                }
                else
                {
                    // Для режима обновления показываем другой текст.
                    Console.WriteLine("Запущен установщик обновления vn.");
                    Console.WriteLine("Подготовка обновления...");
                }

                Console.WriteLine();

                // Пишем стартовую информацию в лог.
                logger.Info("VnInstaller started.");
                logger.Info("Command: " + installerArguments.Command);
                logger.Info("Target directory: " + installerArguments.TargetDirectory);
                logger.Info("App exe path: " + installerArguments.AppExePath);
                logger.Info("Process id: " + (installerArguments.AppProcessId.HasValue ? installerArguments.AppProcessId.Value.ToString() : "not specified"));

                // Выполняем полный сценарий установки или обновления.
                AppUpdateInfo updateInfo = await orchestrator.ExecuteAsync(installerArguments);

                // Пишем в лог успешное завершение.
                logger.Info("VnInstaller finished successfully.");

                Console.WriteLine();
                // Для ручной установки сообщаем путь и версию.
                if (installerArguments.IsManualInstall)
                {
                    Console.WriteLine("Установка успешно выполнена. Установлена версия " + updateInfo.LatestVersion + ".");
                    Console.WriteLine("Приложение установлено в: " + installerArguments.TargetDirectory);
                }
                else
                {
                    // Для обновления сообщаем путь и новую версию.
                    Console.WriteLine("Обновление успешно до версии " + updateInfo.LatestVersion + ".");
                    Console.WriteLine("Обновление установлено в: " + installerArguments.TargetDirectory);
                }

                // После установки основное приложение уже запущено.
                Console.WriteLine("Основное приложение уже запущено.");
                WaitForUser();
                return 0;
            }
            catch (Exception ex)
            {
                // Любая ошибка сценария записывается в лог.
                logger.Error("Installer failed.", ex);
                Console.WriteLine();
                Console.WriteLine("Ошибка установки: " + ex.Message);
                Console.WriteLine("Подробности записаны в лог: " + installerArguments.GetLogFilePath());
                Console.WriteLine("Целевая папка установки: " + installerArguments.TargetDirectory);
                WaitForUser();
                return 1;
            }
        }

        // Конфигурирует все сервисы установщика.
        private static ServiceProvider ConfigureServices(InstallerArguments installerArguments)
        {
            // Создаём коллекцию регистраций.
            ServiceCollection services = new ServiceCollection();

            // Регистрируем единый HttpClient.
            services.AddSingleton<HttpClient>(provider =>
            {
                return new HttpClient();
            });

            // Регистрируем логгер с конкретным путём к файлу лога.
            services.AddSingleton(new FileLogger(installerArguments.GetLogFilePath()));
            // Регистрируем сервис чтения релизов.
            services.AddSingleton<GitHubReleaseService>();
            // Регистрируем сервис скачивания архива.
            services.AddSingleton<ReleaseDownloadService>();
            // Регистрируем сервис распаковки архива.
            services.AddSingleton<ArchiveExtractorService>();
            // Регистрируем сервис ожидания завершения процесса.
            services.AddSingleton<ProcessWaitService>();
            // Регистрируем сервис развёртывания файлов.
            services.AddSingleton<FileDeploymentService>();
            // Регистрируем сервис запуска приложения.
            services.AddSingleton<AppStarterService>();
            // Регистрируем оркестратор всего сценария.
            services.AddSingleton<InstallerOrchestrator>();

            // Возвращаем готовый ServiceProvider.
            return services.BuildServiceProvider();
        }

        // Даёт пользователю время прочитать итоговый текст.
        private static void WaitForUser()
        {
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter, чтобы закрыть установщик.");
            Console.ReadLine();
        }
    }
}
