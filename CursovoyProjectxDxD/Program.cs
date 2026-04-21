using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CursovoyProjectxDxD.Commands;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD
{
    // Главный класс консольного приложения.
    internal class Program
    {
        // Синхронная точка входа нужна для .NET Framework.
        private static int Main(string[] args)
        {
            try
            {
                // Перенаправляем выполнение в асинхронный сценарий.
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // Любая критическая ошибка верхнего уровня печатается в консоль.
                Console.WriteLine("Критическая ошибка: " + ex.Message);
                return 1;
            }
        }

        // Основной сценарий запуска приложения.
        private static async Task<int> MainAsync(string[] args)
        {
            // Задаём читаемое название окна консоли.
            Console.Title = "vn-app";
            // Включаем UTF-8, чтобы русские строки отображались корректно.
            Console.OutputEncoding = Encoding.UTF8;

            // Создаём контейнер зависимостей приложения.
            ServiceProvider serviceProvider = ConfigureServices();
            // Получаем реестр команд из контейнера.
            CommandRegistry registry = serviceProvider.GetRequiredService<CommandRegistry>();
            // Регистрируем все команды CLI.
            RegisterCommands(registry);

            // Сначала просим пользователя пройти авторизацию через API.
            bool isAuthorized = await RunAuthorizationMenuAsync(serviceProvider);

            // Если пользователь решил выйти до входа, завершаем приложение.
            if (!isAuthorized)
            {
                Console.WriteLine("Работа приложения завершена.");
                return 0;
            }

            // После успешного входа очищаем экран от служебных форм авторизации.
            Console.Clear();
            // После авторизации приложение выполняет стартовые действия.
            Console.WriteLine("Интерактивная консоль vn запущена.");
            Console.WriteLine("Текущая версия приложения: " + AppVersionProvider.GetCurrentVersion());
            await PrintStartupUpdateInfoAsync(serviceProvider);
            Console.WriteLine();

            // После авторизации запускаем основную CLI-сессию.
            await RunCliAsync(serviceProvider, registry);
            return 0;
        }

        // Показывает стартовое меню входа и регистрации.
        private static async Task<bool> RunAuthorizationMenuAsync(ServiceProvider serviceProvider)
        {
            // Получаем сервис проверки логина и пароля.
            AuthService authService = serviceProvider.GetRequiredService<AuthService>();
            // Получаем сервис текущей пользовательской сессии.
            AuthSessionService sessionService = serviceProvider.GetRequiredService<AuthSessionService>();

            // Меню повторяется, пока пользователь не войдёт или не отменит запуск.
            while (true)
            {
                // Перед показом стартового auth-меню очищаем экран.
                Console.Clear();
                // Печатаем заголовок раздела авторизации.
                PrintHeader("Авторизация vn-app");
                Console.WriteLine("1 - Войти");
                Console.WriteLine("2 - Зарегистрироваться");
                Console.WriteLine("Esc - Выход");
                Console.WriteLine();
                Console.Write("Выберите действие: ");

                // Читаем одно нажатие клавиши.
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                // Если пользователь выбрал вход, запускаем форму входа.
                if (key.KeyChar == '1')
                {
                    if (await LoginUserAsync(authService, sessionService))
                    {
                        return true;
                    }

                    continue;
                }

                // Если пользователь выбрал регистрацию, запускаем форму регистрации.
                if (key.KeyChar == '2')
                {
                    await RegisterUserAsync(authService);
                    continue;
                }

                // Esc завершает приложение до запуска CLI.
                if (key.Key == ConsoleKey.Escape)
                {
                    return false;
                }

                // Для неизвестной клавиши показываем короткое сообщение.
                ShowMessage("Неизвестная команда.", false);
            }
        }

        // Выполняет вход пользователя.
        private static async Task<bool> LoginUserAsync(AuthService authService, AuthSessionService sessionService)
        {
            // Перед формой входа очищаем экран.
            Console.Clear();
            // Печатаем заголовок входа.
            PrintHeader("Вход");
            // Запрашиваем логин пользователя.
            Console.Write("Введите логин: ");
            string login = Console.ReadLine();
            // Запрашиваем пароль пользователя.
            Console.Write("Введите пароль: ");
            string password = Console.ReadLine();

            // Передаём введённые данные в сервис аутентификации.
            AuthResult result = await authService.AuthenticateAsync(login, password, CancellationToken.None);
            // Показываем итог операции.
            ShowMessage(result.Message, result.IsSuccess);

            // После успешного входа запоминаем пользователя и токен серверной сессии.
            if (result.IsSuccess && result.UserId.HasValue)
            {
                sessionService.SignIn(result.UserId.Value, result.Login);
                return true;
            }

            // При неудачном входе возвращаемся в меню авторизации.
            return false;
        }

        // Выполняет регистрацию нового пользователя.
        private static async Task RegisterUserAsync(AuthService authService)
        {
            // Перед формой регистрации очищаем экран.
            Console.Clear();
            // Печатаем заголовок регистрации.
            PrintHeader("Регистрация");
            // Запрашиваем логин.
            Console.Write("Введите логин: ");
            string login = Console.ReadLine();
            // Запрашиваем пароль.
            Console.Write("Введите пароль: ");
            string password = Console.ReadLine();

            // Регистрируем пользователя через API.
            AuthResult result = await authService.RegisterAsync(login, password, CancellationToken.None);
            // Показываем сообщение об успехе или ошибке.
            ShowMessage(result.Message, result.IsSuccess);
        }

        // Запускает основную интерактивную CLI-сессию.
        private static async Task RunCliAsync(ServiceProvider serviceProvider, CommandRegistry registry)
        {
            // Получаем текущую пользовательскую сессию.
            AuthSessionService sessionService = serviceProvider.GetRequiredService<AuthSessionService>();

            // Если пользователь вошёл, показываем его логин.
            if (sessionService.IsAuthenticated)
            {
                Console.WriteLine("Текущий пользователь: " + sessionService.CurrentLogin);
            }

            // Печатаем краткую подсказку по работе с CLI.
            Console.WriteLine("Введите команду. Для справки: help");
            // Печатаем подсказку по выходу из программы.
            Console.WriteLine("Для выхода: exit");
            Console.WriteLine();

            // Основной цикл чтения команд пользователя.
            while (true)
            {
                // Печатаем приглашение командной строки.
                Console.Write("vn> ");
                // Читаем строку ввода.
                string input = Console.ReadLine();

                // Пустой ввод просто пропускаем.
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Убираем лишние пробелы по краям.
                input = input.Trim();

                // Команды выхода обрабатываем отдельно.
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Выход из программы.");
                    break;
                }

                // Разбиваем строку на аргументы.
                string[] commandArgs = SplitCommandLine(input);
                // По аргументам определяем ключ команды.
                string commandKey = BuildCommandKey(commandArgs);
                // Подготавливаем переменную для найденной команды.
                ICommand command;

                // Если команда не найдена, выводим подсказку.
                if (!registry.TryGet(commandKey, out command))
                {
                    Console.WriteLine("Неизвестная команда.");
                    Console.WriteLine("Введите 'help' для просмотра списка команд.");
                    Console.WriteLine();
                    continue;
                }

                try
                {
                    // Формируем контекст текущего выполнения.
                    CommandContext context = new CommandContext(commandArgs, serviceProvider);
                    // Выполняем команду.
                    CommandResult result = await command.ExecuteAsync(context, CancellationToken.None);
                    // Печатаем сообщение результата команды.
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
        }

        // Показывает информацию о доступности обновления при старте.
        private static async Task PrintStartupUpdateInfoAsync(ServiceProvider serviceProvider)
        {
            try
            {
                // Получаем сервис чтения GitHub Releases.
                GitHubReleaseService releaseService = serviceProvider.GetRequiredService<GitHubReleaseService>();
                // Выполняем проверку новой версии.
                AppUpdateInfo updateInfo = await releaseService.CheckForUpdateAsync();

                // Если обновление найдено, сообщаем новую версию.
                if (updateInfo.IsAvailable)
                {
                    Console.WriteLine("Доступно обновление до версии " + updateInfo.LatestVersion + ".");
                }
                else
                {
                    // Иначе сообщаем, что версия уже актуальна.
                    Console.WriteLine("Обновление не требуется. Установлена актуальная версия.");
                }
            }
            catch (Exception ex)
            {
                // Ошибка проверки обновлений не должна ломать запуск CLI.
                Console.WriteLine("Не удалось проверить обновления: " + ex.Message);
            }
        }

        // Регистрирует сервисы приложения в DI-контейнере.
        private static ServiceProvider ConfigureServices()
        {
            // Создаём коллекцию сервисов.
            ServiceCollection services = new ServiceCollection();

            // Регистрируем реестр команд.
            services.AddSingleton<CommandRegistry>();
            // Регистрируем единый HttpClient.
            services.AddSingleton(new HttpClient());
            // Регистрируем сервис чтения релизов.
            services.AddSingleton<GitHubReleaseService>();
            // Регистрируем сервис запуска установщика.
            services.AddSingleton<InstallerLauncherService>();
            // Регистрируем фабрику соединений с PostgreSQL.
            services.AddSingleton<DatabaseConnectionFactory>();
            // Регистрируем сервис инициализации схемы БД.
            // Регистрируем сервис авторизации через PostgreSQL.
            services.AddSingleton<AuthService>();
            // Регистрируем сервис хранения текущей сессии.
            services.AddSingleton<AuthSessionService>();
            // Регистрируем сервис работы с заметками через PostgreSQL.
            services.AddSingleton<NoteService>();

            // Собираем и возвращаем готовый контейнер.
            return services.BuildServiceProvider();
        }

        // Регистрирует команды приложения.
        private static void RegisterCommands(CommandRegistry registry)
        {
            // Команда справки.
            registry.Register(new HelpCommand(registry));
            // Команда версии.
            registry.Register(new VersionCommand());
            // Команда добавления заметки.
            registry.Register(new NoteAddCommand());
            // Команда удаления заметки.
            registry.Register(new NoteDeleteCommand());
            // Команда проверки обновлений.
            registry.Register(new UpdateCheckCommand());
            // Команда запуска обновления.
            registry.Register(new UpdateApplyCommand());
        }

        // Строит ключ команды по массиву аргументов.
        private static string BuildCommandKey(string[] args)
        {
            // Пустой ввод сводим к help.
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
                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt add";
                }

                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("del", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt del";
                }

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

            // Для остального возвращаем строку как есть.
            return string.Join(" ", args).Trim();
        }

        // Разбирает строку команды на аргументы с поддержкой кавычек.
        private static string[] SplitCommandLine(string commandLine)
        {
            // Пустая строка даёт пустой массив.
            if (string.IsNullOrWhiteSpace(commandLine))
                return new string[0];

            // Создаём список итоговых аргументов.
            List<string> result = new List<string>();
            // Флаг показывает, находимся ли внутри кавычек.
            bool inQuotes = false;
            // Буфер текущего аргумента.
            StringBuilder current = new StringBuilder();

            // Посимвольно разбираем строку ввода.
            foreach (char ch in commandLine)
            {
                // Кавычка переключает режим чтения текста в кавычках.
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
                    // Любой другой символ добавляем в текущий аргумент.
                    current.Append(ch);
                }
            }

            // Добавляем последний аргумент после завершения цикла.
            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            // Возвращаем готовый массив аргументов.
            return result.ToArray();
        }

        // Печатает заголовок отдельного экрана авторизации.
        private static void PrintHeader(string title)
        {
            // Печатаем текст заголовка.
            Console.WriteLine(title);
            // Печатаем визуальный разделитель.
            Console.WriteLine(new string('-', 32));
            Console.WriteLine();
        }

        // Показывает цветное сообщение пользователю.
        private static void ShowMessage(string message, bool isSuccess)
        {
            // Подбираем цвет по результату операции.
            Console.ForegroundColor = isSuccess ? ConsoleColor.Green : ConsoleColor.Red;
            // Печатаем сообщение.
            Console.WriteLine(message);
            // Возвращаем стандартный цвет консоли.
            Console.ResetColor();
            Console.WriteLine();
            // Даём пользователю время прочитать ответ.
            System.Threading.Thread.Sleep(900);
        }
    }
}
