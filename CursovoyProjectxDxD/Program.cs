using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CursovoyProjectxDxD.Commands;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD
{
    // Главный класс основного приложения.
    internal class Program
    {
        // Синхронная точка входа нужна для .NET Framework.
        private static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка: " + ex.Message);
                return 1;
            }
        }

        // Основной сценарий запуска CLI.
        private static async Task<int> MainAsync(string[] args)
        {
            // Название окна помогает отличать основное приложение от установщика.
            Console.Title = "vn-app";

            // Создаём контейнер зависимостей приложения.
            ServiceProvider serviceProvider = ConfigureServices();
            // Получаем реестр команд из контейнера.
            CommandRegistry registry = serviceProvider.GetRequiredService<CommandRegistry>();
            // Наполняем реестр командами.
            RegisterCommands(registry);

            // Печатаем стартовое сообщение.
            Console.WriteLine("Интерактивная консоль vn запущена.");
            // Сразу показываем локальную версию приложения.
            Console.WriteLine("Текущая версия приложения: " + AppVersionProvider.GetCurrentVersion());
            // При старте дополнительно выполняем быструю проверку обновления.
            await PrintStartupUpdateInfoAsync(serviceProvider);
            // Показываем короткую подсказку по использованию CLI.
            Console.WriteLine("Введите команду. Для справки: help");
            Console.WriteLine("Для выхода: exit");
            Console.WriteLine();

            // Основной цикл продолжается, пока пользователь явно не введёт exit или quit.
            while (true)
            {
                // Печатаем приглашение ввода.
                Console.Write("vn> ");
                // Считываем строку пользователя.
                string input = Console.ReadLine();

                // Пустые строки просто игнорируем.
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Убираем пробелы по краям.
                input = input.Trim();

                // Команды выхода обрабатываются отдельно до поиска в реестре.
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Выход из программы.");
                    break;
                }

                // Разбиваем строку на массив аргументов.
                string[] commandArgs = SplitCommandLine(input);
                // По массиву аргументов определяем ключ команды.
                string commandKey = BuildCommandKey(commandArgs);

                // Переменная под найденную команду.
                ICommand command;
                // Пробуем найти обработчик команды в реестре.
                if (!registry.TryGet(commandKey, out command))
                {
                    Console.WriteLine("Неизвестная команда.");
                    Console.WriteLine("Введите 'help' для просмотра списка команд.");
                    Console.WriteLine();
                    continue;
                }

                try
                {
                    // Создаём контекст текущего выполнения.
                    CommandContext context = new CommandContext(commandArgs, serviceProvider);
                    // Выполняем команду.
                    CommandResult result = await command.ExecuteAsync(context);

                    // Печатаем сообщение команды.
                    Console.WriteLine(result.Message);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    // Ошибка одной команды не должна завершать всё приложение.
                    Console.WriteLine("Ошибка выполнения команды: " + ex.Message);
                    Console.WriteLine();
                }
            }

            return 0;
        }

        // Показывает информацию о доступности обновления при старте приложения.
        private static async Task PrintStartupUpdateInfoAsync(ServiceProvider serviceProvider)
        {
            try
            {
                // Получаем сервис работы с GitHub Releases.
                GitHubReleaseService releaseService = serviceProvider.GetRequiredService<GitHubReleaseService>();
                // Выполняем сетевую проверку релиза.
                AppUpdateInfo updateInfo = await releaseService.CheckForUpdateAsync();

                // Если новая версия найдена, сообщаем об этом пользователю.
                if (updateInfo.IsAvailable)
                {
                    Console.WriteLine("Доступно обновление до версии " + updateInfo.LatestVersion + ".");
                }
                else
                {
                    // Если новой версии нет, явно говорим, что всё актуально.
                    Console.WriteLine("Обновление не требуется. Установлена актуальная версия.");
                }
            }
            catch (Exception ex)
            {
                // Ошибка проверки обновления не должна мешать запуску CLI.
                Console.WriteLine("Не удалось проверить обновления: " + ex.Message);
            }
        }

        // Конфигурируем сервисы, доступные командам.
        private static ServiceProvider ConfigureServices()
        {
            // Создаём коллекцию регистраций сервисов.
            ServiceCollection services = new ServiceCollection();

            // Реестр команд живёт на протяжении всей сессии приложения.
            services.AddSingleton<CommandRegistry>();
            // Один HttpClient переиспользуется всеми сетевыми запросами приложения.
            services.AddSingleton(new HttpClient());
            // Сервис чтения релизов нужен командам update и стартовой проверке.
            services.AddSingleton<GitHubReleaseService>();
            // Сервис запуска внешнего установщика нужен для update apply.
            services.AddSingleton<InstallerLauncherService>();

            // Собираем итоговый контейнер зависимостей.
            return services.BuildServiceProvider();
        }

        // Заполняем реестр всеми командами приложения.
        private static void RegisterCommands(CommandRegistry registry)
        {
            // Команда справки.
            registry.Register(new HelpCommand(registry));
            // Команда вывода версии.
            registry.Register(new VersionCommand());
            // Команда проверки доступности обновления.
            registry.Register(new UpdateCheckCommand());
            // Команда запуска установщика обновления.
            registry.Register(new UpdateApplyCommand());
        }

        // Нормализуем массив аргументов в ключ команды.
        private static string BuildCommandKey(string[] args)
        {
            // Пустой ввод безопасно сводим к help.
            if (args == null || args.Length == 0)
                return "help";

            // Обрабатываем однословные команды.
            if (args.Length == 1)
            {
                if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                    return "help";

                if (args[0].Equals("version", StringComparison.OrdinalIgnoreCase))
                    return "version";
            }

            // Обрабатываем известные двухсловные команды.
            if (args.Length >= 2)
            {
                if (args[0].Equals("update", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("check", StringComparison.OrdinalIgnoreCase))
                {
                    return "update check";
                }

                if (args[0].Equals("update", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("apply", StringComparison.OrdinalIgnoreCase))
                {
                    return "update apply";
                }
            }

            // Для всего остального возвращаем строку как есть.
            return string.Join(" ", args).Trim();
        }

        // Разбираем строку команды на аргументы с поддержкой кавычек.
        private static string[] SplitCommandLine(string commandLine)
        {
            // Пустая строка даёт пустой массив.
            if (string.IsNullOrWhiteSpace(commandLine))
                return new string[0];

            // Список готовых аргументов.
            var result = new System.Collections.Generic.List<string>();
            // Флаг, показывающий, находимся ли мы внутри кавычек.
            bool inQuotes = false;
            // Буфер текущего аргумента.
            var current = new System.Text.StringBuilder();

            // Посимвольно разбираем строку ввода.
            foreach (char ch in commandLine)
            {
                // Кавычка переключает режим разбора, но не входит в аргумент.
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                // Пробел вне кавычек завершает текущий аргумент.
                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        // Сохраняем накопленный аргумент.
                        result.Add(current.ToString());
                        // Очищаем буфер под следующий.
                        current.Clear();
                    }
                }
                else
                {
                    // Обычный символ добавляем в текущий аргумент.
                    current.Append(ch);
                }
            }

            // Не забываем добавить последний аргумент после завершения цикла.
            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            // Возвращаем готовый массив аргументов.
            return result.ToArray();
        }
    }
}
