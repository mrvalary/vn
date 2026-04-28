using System;
using System.Diagnostics;
using System.IO;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Проверяет и запускает watcher-агент рядом с основным приложением.
    /// </summary>
    public sealed class WatcherLauncherService
    {
        #region Constants

        // Имя exe-файла watcher-агента, который копируется в папку основного приложения после сборки.
        private const string WatcherExeName = "vn-watcher.exe";

        #endregion

        #region Public API

        /// <summary>
        /// Запускает watcher-агент, если процесс ещё не работает.
        /// </summary>
        /// <returns>Результат проверки и запуска watcher-а.</returns>
        public WatcherLaunchResult EnsureStarted()
        {
            if (IsWatcherRunning())
            {
                return WatcherLaunchResult.AlreadyRunning();
            }

            string watcherPath = GetWatcherPath();
            if (!File.Exists(watcherPath))
            {
                return WatcherLaunchResult.NotFound(watcherPath);
            }

            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = watcherPath,
                WorkingDirectory = Path.GetDirectoryName(watcherPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            return process == null
                ? WatcherLaunchResult.Failed("процесс watcher-а не был создан.")
                : WatcherLaunchResult.Started();
        }

        /// <summary>
        /// Возвращает текущее состояние watcher-агента без попытки запуска процесса.
        /// </summary>
        /// <returns>Статус watcher-агента и путь к ожидаемому exe-файлу.</returns>
        public WatcherStatus GetStatus()
        {
            return new WatcherStatus(IsWatcherRunning(), GetWatcherPath());
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Проверяет, есть ли уже запущенный процесс watcher-а.
        /// </summary>
        /// <returns>true, если watcher уже работает.</returns>
        private static bool IsWatcherRunning()
        {
            string processName = Path.GetFileNameWithoutExtension(WatcherExeName);
            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// Возвращает путь к watcher-агенту рядом с exe основного приложения.
        /// </summary>
        /// <returns>Полный путь к vn-watcher.exe.</returns>
        private static string GetWatcherPath()
        {
            string appExePath = Process.GetCurrentProcess().MainModule.FileName;
            string appDirectory = Path.GetDirectoryName(appExePath);
            return Path.Combine(appDirectory, WatcherExeName);
        }

        #endregion
    }

    /// <summary>
    /// Результат попытки запуска watcher-агента.
    /// </summary>
    public sealed class WatcherLaunchResult
    {
        #region Properties

        /// <summary>
        /// true, если watcher уже работал или успешно запущен.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Сообщение для вывода в консоль.
        /// </summary>
        public string Message { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Создаёт результат проверки watcher-агента.
        /// </summary>
        /// <param name="isSuccess">Признак успешного состояния.</param>
        /// <param name="message">Сообщение для пользователя.</param>
        private WatcherLaunchResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        #endregion

        #region Factories

        /// <summary>
        /// Возвращает результат, когда watcher уже работал.
        /// </summary>
        public static WatcherLaunchResult AlreadyRunning()
        {
            return new WatcherLaunchResult(true, "Watcher-агент уже запущен.");
        }

        /// <summary>
        /// Возвращает результат успешного запуска watcher-а.
        /// </summary>
        public static WatcherLaunchResult Started()
        {
            return new WatcherLaunchResult(true, "Watcher-агент запущен автоматически.");
        }

        /// <summary>
        /// Возвращает результат, когда exe watcher-а не найден.
        /// </summary>
        /// <param name="path">Ожидаемый путь к exe watcher-а.</param>
        public static WatcherLaunchResult NotFound(string path)
        {
            return new WatcherLaunchResult(false, "Watcher-агент не найден: " + path);
        }

        /// <summary>
        /// Возвращает результат неудачного запуска watcher-а.
        /// </summary>
        /// <param name="reason">Причина ошибки.</param>
        public static WatcherLaunchResult Failed(string reason)
        {
            return new WatcherLaunchResult(false, "Не удалось запустить watcher-агент: " + reason);
        }

        #endregion
    }

    /// <summary>
    /// Состояние watcher-агента для команды watch status.
    /// </summary>
    public sealed class WatcherStatus
    {
        #region Construction

        /// <summary>
        /// Создает снимок состояния watcher-агента.
        /// </summary>
        /// <param name="isRunning">true, если процесс watcher-а уже запущен.</param>
        /// <param name="executablePath">Ожидаемый путь к vn-watcher.exe.</param>
        public WatcherStatus(bool isRunning, string executablePath)
        {
            IsRunning = isRunning;
            ExecutablePath = executablePath;
        }

        #endregion

        #region Properties

        /// <summary>
        /// true, если watcher-агент сейчас работает отдельным процессом.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Путь, по которому основное приложение ищет vn-watcher.exe.
        /// </summary>
        public string ExecutablePath { get; private set; }

        #endregion
    }
}
