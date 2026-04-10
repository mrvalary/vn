using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CursovoyProjectxDxD.Commands;
using CursovoyProjectxDxD.Core;
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
                // Вся логика запуска вынесена в асинхронный метод.
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Необработанная ошибка верхнего уровня пишется в консоль.
                Console.WriteLine("Критическая ошибка: " + ex.Message);
                return 1;
            }
        }

        // Основной сценарий запуска CLI.
        private static async Task<int> MainAsync(string[] args)
        {
            // Создаём контейнер зависимостей приложения.
            ServiceProvider serviceProvider = ConfigureServices();
            // Достаём из контейнера реестр команд.
            CommandRegistry registry = serviceProvider.GetRequiredService<CommandRegistry>();

            // Регистрируем все доступные команды.
            RegisterCommands(registry);

            // Печатаем приветствие.
            Console.WriteLine("Интерактивная консоль vn запущена.");
            // Подсказываем базовую команду справки.
            Console.WriteLine("Введите команду. Для справки: help");
            // Подсказываем способ выхода.
            Console.WriteLine("Для выхода: exit");
            Console.WriteLine();

            // Основной цикл живёт до команды exit или quit.
            while (true)
            {
                // Показываем приглашение ввода.
                Console.Write("vn> ");
                // Считываем команду пользователя.
                string input = Console.ReadLine();

                // Пустой ввод пропускаем.
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Нормализуем строку.
                input = input.Trim();

                // Выход обрабатывается до поиска по реестру.
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Выход из программы.");
                    break;
                }

                // Разбиваем строку на аргументы.
                string[] commandArgs = SplitCommandLine(input);
                // Строим ключ для поиска команды.
                string commandKey = BuildCommandKey(commandArgs);

                // Переменная под найденную команду.
                ICommand command;
                // Ищем команду в реестре.
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
                    // Запускаем команду.
                    CommandResult result = await command.ExecuteAsync(context);

                    // Показываем ответ команды.
                    Console.WriteLine(result.Message);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    // Ошибка одной команды не должна завершать весь CLI.
                    Console.WriteLine("Ошибка выполнения команды: " + ex.Message);
                    Console.WriteLine();
                }
            }

            // Возвращаем код успешного завершения.
            return 0;
        }

        // Конфигурируем сервисы, доступные командам.
        private static ServiceProvider ConfigureServices()
        {
            // Создаём коллекцию регистраций сервисов.
            ServiceCollection services = new ServiceCollection();

            // Реестр команд нужен во время всей сессии приложения.
            services.AddSingleton<CommandRegistry>();

            // Один HttpClient переиспользуется для всех сетевых обращений.
            services.AddSingleton<HttpClient>(provider =>
            {
                HttpClient client = new HttpClient();
                return client;
            });

            // Сервис чтения релизов из GitHub.
            services.AddSingleton<GitHubReleaseService>();
            // Сервис запуска внешнего установщика.
            services.AddSingleton<InstallerLauncherService>();

            // Строим итоговый ServiceProvider.
            return services.BuildServiceProvider();
        }

        // Заполняем реестр всеми командами приложения.
        private static void RegisterCommands(CommandRegistry registry)
        {
            // Команда справки.
            registry.Register(new HelpCommand(registry));
            // Команда версии.
            registry.Register(new VersionCommand());
            // Команда проверки обновления.
            registry.Register(new UpdateCheckCommand());
            // Команда применения обновления.
            registry.Register(new UpdateApplyCommand());
        }

        // Нормализуем массив аргументов в ключ команды.
        private static string BuildCommandKey(string[] args)
        {
            // При пустом вводе безопасно возвращаем help.
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

            // Всё остальное возвращаем как есть.
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
            // Флаг режима внутри кавычек.
            bool inQuotes = false;
            // Буфер текущего аргумента.
            var current = new System.Text.StringBuilder();

            // Посимвольно разбираем командную строку.
            foreach (char ch in commandLine)
            {
                // Кавычка только меняет режим разбора.
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
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    // Любой другой символ попадает в текущий аргумент.
                    current.Append(ch);
                }
            }

            // Добавляем последний накопленный аргумент.
            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            // Возвращаем итоговый массив.
            return result.ToArray();
        }
    }
}
