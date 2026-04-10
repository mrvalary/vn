using System;
using System.IO;

namespace VnInstaller.Core
{
    // Модель аргументов запуска установщика.
    public sealed class InstallerArguments
    {
        // Имя корневой папки установки по умолчанию.
        private const string DefaultInstallFolderName = "vn";
        // Имя папки приложения внутри каталога установки.
        private const string DefaultAppFolderName = "app";
        // Имя exe-файла основного приложения.
        private const string DefaultAppExeName = "CursovoyProjectxDxD.exe";

        // Команда запуска установщика.
        public string Command { get; private set; }
        // Целевая директория установки или обновления.
        public string TargetDirectory { get; private set; }
        // Полный путь к exe основного приложения.
        public string AppExePath { get; private set; }
        // PID основного приложения, если обновление инициировано из vn-app.
        public int? AppProcessId { get; private set; }

        // Закрытый конструктор ограничивает создание объекта внутренней логикой парсинга.
        private InstallerArguments()
        {
        }

        // Признак ручной установки без участия vn-app.
        public bool IsManualInstall
        {
            get { return string.Equals(Command, "install", StringComparison.OrdinalIgnoreCase); }
        }

        // Разбирает аргументы командной строки.
        public static bool TryParse(string[] args, out InstallerArguments result, out string error)
        {
            // По умолчанию результата нет.
            result = null;
            // По умолчанию текста ошибки нет.
            error = null;

            // Если аргументы не переданы, запускаем режим обычной установки.
            if (args == null || args.Length == 0)
            {
                result = CreateDefaultInstallArguments();
                return true;
            }

            // Явная команда install ведёт к тому же сценарию.
            if (string.Equals(args[0], "install", StringComparison.OrdinalIgnoreCase))
            {
                result = CreateDefaultInstallArguments();
                return true;
            }

            // Любая команда, кроме apply, считается некорректной.
            if (!string.Equals(args[0], "apply", StringComparison.OrdinalIgnoreCase))
            {
                error = "Поддерживаются команды install и apply.";
                return false;
            }

            // Переменная под путь установки.
            string targetDirectory = null;
            // Переменная под путь к exe приложения.
            string appExePath = null;
            // Переменная под PID приложения.
            int? processId = null;

            // Разбираем аргументы apply по одному.
            for (int i = 1; i < args.Length; i++)
            {
                string key = args[i];

                // Читаем значение --targetDir.
                if (string.Equals(key, "--targetDir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    targetDirectory = args[++i];
                    continue;
                }

                // Читаем значение --appExe.
                if (string.Equals(key, "--appExe", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    appExePath = args[++i];
                    continue;
                }

                // Читаем значение --pid.
                if (string.Equals(key, "--pid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    int parsedValue;
                    // PID должен быть целым числом.
                    if (!int.TryParse(args[++i], out parsedValue))
                    {
                        error = "Аргумент --pid должен быть целым числом.";
                        return false;
                    }

                    processId = parsedValue;
                    continue;
                }

                // Любой неизвестный аргумент считаем ошибкой.
                error = "Неизвестный аргумент: " + key;
                return false;
            }

            // Путь установки обязателен в режиме apply.
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                error = "Не указан --targetDir.";
                return false;
            }

            // Путь к exe обязателен в режиме apply.
            if (string.IsNullOrWhiteSpace(appExePath))
            {
                error = "Не указан --appExe.";
                return false;
            }

            // Создаём объект аргументов для режима обновления.
            result = new InstallerArguments
            {
                Command = "apply",
                TargetDirectory = Path.GetFullPath(targetDirectory),
                AppExePath = Path.GetFullPath(appExePath),
                AppProcessId = processId
            };

            return true;
        }

        // Возвращает полный путь к файлу лога.
        public string GetLogFilePath()
        {
            // По умолчанию лог лежит в каталоге установки.
            string baseDirectory = TargetDirectory;
            // Если каталог установки ещё не определён, используем директорию процесса.
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            // Возвращаем путь к vn-installer.log.
            return Path.Combine(baseDirectory, "vn-installer.log");
        }

        // Создаёт аргументы режима установки по умолчанию.
        private static InstallerArguments CreateDefaultInstallArguments()
        {
            // Берём каталог LocalAppData текущего пользователя.
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Формируем целевой путь установки.
            string targetDirectory = Path.Combine(localAppData, DefaultInstallFolderName, DefaultAppFolderName);

            // Возвращаем готовую модель аргументов.
            return new InstallerArguments
            {
                Command = "install",
                TargetDirectory = targetDirectory,
                AppExePath = Path.Combine(targetDirectory, DefaultAppExeName),
                AppProcessId = null
            };
        }
    }
}
