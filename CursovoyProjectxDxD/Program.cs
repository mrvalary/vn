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
    /// <summary>
    /// Главный класс консольного приложения.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Синхронная точка входа приложения для .NET Framework.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        /// <returns>Код завершения приложения.</returns>
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

        /// <summary>
        /// Выполняет основной сценарий запуска приложения.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        /// <returns>Код завершения приложения.</returns>
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

            if (!await EnsureDatabaseConnectionAsync(serviceProvider))
            {
                return 1;
            }

            // Watcher-агент должен быть запущен вместе с основным приложением, а не после ручной команды.
            WatcherLaunchResult watcherLaunchResult = EnsureWatcherStarted(serviceProvider);

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
            Console.WriteLine(watcherLaunchResult.Message);
            await PrintStartupUpdateInfoAsync(serviceProvider);
            Console.WriteLine();

            // После авторизации запускаем основную CLI-сессию.
            await RunCliAsync(serviceProvider, registry);
            return 0;
        }

        /// <summary>
        /// Проверяет, что приложение может подключиться к базе до показа формы входа.
        /// </summary>
        /// <param name="serviceProvider">Контейнер сервисов приложения.</param>
        /// <returns>true, если подключение к базе успешно.</returns>
        private static async Task<bool> EnsureDatabaseConnectionAsync(ServiceProvider serviceProvider)
        {
            try
            {
                DatabaseConnectionFactory connectionFactory = serviceProvider.GetRequiredService<DatabaseConnectionFactory>();
                using (Npgsql.NpgsqlConnection connection = await connectionFactory.CreateOpenBootstrapConnectionAsync(CancellationToken.None))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось подключиться к базе данных: " + DatabaseConnectionFactory.FormatDatabaseError(ex));
                return false;
            }
        }

        /// <summary>
        /// Показывает стартовое меню входа и регистрации.
        /// </summary>
        /// <param name="serviceProvider">Контейнер сервисов приложения.</param>
        /// <returns>true, если пользователь успешно авторизовался.</returns>
        private static async Task<bool> RunAuthorizationMenuAsync(ServiceProvider serviceProvider)
        {
            // Получаем сервис проверки логина и пароля.
            AuthService authService = serviceProvider.GetRequiredService<AuthService>();
            // Получаем сервис текущей пользовательской сессии.
            AuthSessionService sessionService = serviceProvider.GetRequiredService<AuthSessionService>();
            // Получаем сервис журнала безопасности.
            SecurityLogService securityLogService = serviceProvider.GetRequiredService<SecurityLogService>();

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
                    if (await LoginUserAsync(authService, sessionService, securityLogService))
                    {
                        return true;
                    }

                    continue;
                }

                // Если пользователь выбрал регистрацию, запускаем форму регистрации.
                if (key.KeyChar == '2')
                {
                    await RegisterUserAsync(authService, securityLogService);
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

        /// <summary>
        /// Проверяет запуск watcher-агента и запускает его, если он ещё не работает.
        /// </summary>
        /// <param name="serviceProvider">Контейнер сервисов приложения.</param>
        /// <returns>Результат проверки и запуска watcher-а.</returns>
        private static WatcherLaunchResult EnsureWatcherStarted(ServiceProvider serviceProvider)
        {
            try
            {
                WatcherLauncherService watcherLauncher = serviceProvider.GetRequiredService<WatcherLauncherService>();
                return watcherLauncher.EnsureStarted();
            }
            catch (Exception ex)
            {
                return WatcherLaunchResult.Failed("не удалось проверить watcher-агент: " + ex.Message);
            }
        }

        /// <summary>
        /// Выполняет вход пользователя.
        /// </summary>
        /// <param name="authService">Сервис проверки логина и пароля.</param>
        /// <param name="sessionService">Сервис текущей пользовательской сессии.</param>
        /// <param name="securityLogService">Сервис журнала безопасности.</param>
        /// <returns>true, если вход выполнен успешно.</returns>
        private static async Task<bool> LoginUserAsync(AuthService authService, AuthSessionService sessionService, SecurityLogService securityLogService)
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
                sessionService.SignIn(result.UserId.Value, result.Login, result.RoleName);
                await securityLogService.WriteCurrentUserEventAsync("login_success", "Пользователь вошёл в систему.", result.Login, CancellationToken.None);
                return true;
            }

            // Неудачный вход тоже пишем в журнал безопасности.
            await securityLogService.WriteAnonymousEventAsync(login, "login_failed", result.Message, login, CancellationToken.None);

            // При неудачном входе возвращаемся в меню авторизации.
            return false;
        }

        /// <summary>
        /// Выполняет регистрацию нового пользователя.
        /// </summary>
        /// <param name="authService">Сервис регистрации пользователя.</param>
        /// <param name="securityLogService">Сервис журнала безопасности.</param>
        /// <returns>Задача регистрации пользователя.</returns>
        private static async Task RegisterUserAsync(AuthService authService, SecurityLogService securityLogService)
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
            // Регистрацию и ошибку регистрации фиксируем в журнале безопасности.
            await securityLogService.WriteAnonymousEventAsync(login, result.IsSuccess ? "register_success" : "register_failed", result.Message, login, CancellationToken.None);
            // Показываем сообщение об успехе или ошибке.
            ShowMessage(result.Message, result.IsSuccess);
        }

        /// <summary>
        /// Запускает основную интерактивную CLI-сессию.
        /// </summary>
        /// <param name="serviceProvider">Контейнер сервисов приложения.</param>
        /// <param name="registry">Реестр доступных команд.</param>
        /// <returns>Задача выполнения CLI-сессии.</returns>
        private static async Task RunCliAsync(ServiceProvider serviceProvider, CommandRegistry registry)
        {
            // Получаем текущую пользовательскую сессию.
            AuthSessionService sessionService = serviceProvider.GetRequiredService<AuthSessionService>();

            // Если пользователь вошёл, показываем его логин.
            if (sessionService.IsAuthenticated)
            {
                Console.WriteLine("Текущий пользователь: " + sessionService.CurrentLogin);
                Console.WriteLine("Роль: " + sessionService.CurrentRoleDisplayName);
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
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
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

                    // Если команда auth logout очистила сессию, возвращаем пользователя в меню входа.
                    if (!sessionService.IsAuthenticated)
                    {
                        Console.WriteLine("Для продолжения работы войдите в систему снова.");
                        await Task.Delay(900);

                        bool isAuthorized = await RunAuthorizationMenuAsync(serviceProvider);
                        if (!isAuthorized)
                        {
                            Console.WriteLine("Работа приложения завершена.");
                            break;
                        }

                        Console.Clear();
                        Console.WriteLine("Вы снова вошли в систему как: " + sessionService.CurrentLogin);
                        Console.WriteLine("Роль: " + sessionService.CurrentRoleDisplayName);
                        Console.WriteLine("Введите команду. Для справки: help");
                        Console.WriteLine("Для выхода: exit");
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    // Ошибка одной команды не должна завершать всё приложение.
                    Console.WriteLine("Ошибка выполнения команды: " + ex.Message);
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Показывает информацию о доступности обновления при старте.
        /// </summary>
        /// <param name="serviceProvider">Контейнер сервисов приложения.</param>
        /// <returns>Задача проверки обновления.</returns>
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
                    if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseName))
                    {
                        Console.WriteLine("Релиз: " + updateInfo.ReleaseName);
                    }
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

        /// <summary>
        /// Регистрирует сервисы приложения в DI-контейнере.
        /// </summary>
        /// <returns>Готовый контейнер сервисов.</returns>
        private static ServiceProvider ConfigureServices()
        {
            // Создаём коллекцию сервисов.
            ServiceCollection services = new ServiceCollection();

            // Регистрируем реестр команд.
            services.AddSingleton<CommandRegistry>();
            // Регистрируем единый HttpClient.
            services.AddSingleton(new HttpClient());
            // Регистрируем YAML-настройки без секретов и строк подключения.
            services.AddSingleton(YamlAppSettings.Load());
            // Регистрируем сервис чтения релизов.
            services.AddSingleton<GitHubReleaseService>();
            // Регистрируем сервис запуска установщика.
            services.AddSingleton<InstallerLauncherService>();
            // Регистрируем сервис автозапуска watcher-агента.
            services.AddSingleton<WatcherLauncherService>();
            // Регистрируем фабрику соединений с PostgreSQL.
            services.AddSingleton<DatabaseConnectionFactory>();
            // Регистрируем сервис инициализации схемы БД.
            // Регистрируем сервис авторизации через PostgreSQL.
            services.AddSingleton<AuthService>();
            // Регистрируем сервис хранения текущей сессии.
            services.AddSingleton<AuthSessionService>();
            // Регистрируем сервис работы с заметками через PostgreSQL.
            services.AddSingleton<NoteService>();
            // Регистрируем сервис журнала безопасности.
            services.AddSingleton<SecurityLogService>();
            // Регистрируем сервис просмотра метрик Watcher.
            services.AddSingleton<MonitoringService>();
            // Собираем и возвращаем готовый контейнер.
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Регистрирует команды приложения.
        /// </summary>
        /// <param name="registry">Реестр команд приложения.</param>
        private static void RegisterCommands(CommandRegistry registry)
        {
            // Команда справки.
            registry.Register(new HelpCommand(registry));
            // Команда очистки консоли.
            registry.Register(new ClearCommand());
            // Команда версии.
            registry.Register(new VersionCommand());
            // Команда добавления заметки.
            registry.Register(new NoteAddCommand());
            // Команда удаления заметки.
            registry.Register(new NoteDeleteCommand());
            // Команда редактирования своей заметки.
            registry.Register(new NoteEditCommand());
            // Команда просмотра списка заметок.
            registry.Register(new NoteListCommand());
            // Команда быстрого просмотра последних заметок.
            registry.Register(new NoteRecentCommand());
            // Команда поиска заметок по тексту.
            registry.Register(new NoteSearchCommand());
            // Команда выхода из текущего аккаунта.
            registry.Register(new AuthLogoutCommand());
            // Админские команды управления пользователями.
            registry.Register(new AdminUserCommand());
            // Админские команды просмотра и редактирования любых заметок.
            registry.Register(new AdminNoteCommand());
            // Команда просмотра журнала безопасности.
            registry.Register(new SecurityLogsCommand());
            // Команды просмотра и управления устройствами Watcher.
            registry.Register(new WatchCommand());
            // Команда проверки обновлений.
            registry.Register(new UpdateCheckCommand());
            // Команда запуска обновления.
            registry.Register(new UpdateApplyCommand());
        }

        /// <summary>
        /// Строит ключ команды по массиву аргументов.
        /// </summary>
        /// <param name="args">Аргументы команды.</param>
        /// <returns>Ключ команды для поиска в реестре.</returns>
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

                if (args[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                    return "clear";

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

                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("edit", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt edit";
                }

                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt list";
                }

                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("recent", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt recent";
                }

                if (args[0].Equals("nt", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("search", StringComparison.OrdinalIgnoreCase))
                {
                    return "nt search";
                }

                if (args[0].Equals("auth", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("logout", StringComparison.OrdinalIgnoreCase))
                {
                    return "auth logout";
                }

                if (args[0].Equals("admin", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    return "admin user";
                }

                if (args[0].Equals("admin", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("nt", StringComparison.OrdinalIgnoreCase))
                {
                    return "admin nt";
                }

                if (args[0].Equals("sec", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("logs", StringComparison.OrdinalIgnoreCase))
                {
                    return "sec logs";
                }

                if (args[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
                {
                    return "watch";
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

        /// <summary>
        /// Разбирает строку команды на аргументы с поддержкой кавычек.
        /// </summary>
        /// <param name="commandLine">Строка команды.</param>
        /// <returns>Массив аргументов команды.</returns>
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

        /// <summary>
        /// Печатает заголовок отдельного экрана авторизации.
        /// </summary>
        /// <param name="title">Текст заголовка.</param>
        private static void PrintHeader(string title)
        {
            // Печатаем текст заголовка.
            Console.WriteLine(title);
            // Печатаем визуальный разделитель.
            Console.WriteLine(new string('-', 32));
            Console.WriteLine();
        }

        /// <summary>
        /// Показывает цветное сообщение пользователю.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="isSuccess">Признак успешного результата.</param>
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
