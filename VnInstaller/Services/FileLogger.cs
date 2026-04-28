using System;
using System.IO;

namespace VnInstaller.Services
{
    /// <summary>
    /// Простой потокобезопасный файловый логгер.
    /// </summary>
    public sealed class FileLogger
    {
        // Путь к файлу лога.
        private readonly string _logFilePath;
        // Объект синхронизации для параллельных записей.
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Создает файловый логгер.
        /// </summary>
        /// <param name="logFilePath">Путь к файлу лога.</param>
        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        /// <summary>
        /// Пишет информационное сообщение.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        public void Info(string message)
        {
            Write("INFO", message);
        }

        /// <summary>
        /// Пишет сообщение об ошибке вместе с исключением.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="exception">Исключение, которое нужно записать в лог.</param>
        public void Error(string message, Exception exception)
        {
            // Начинаем со строки сообщения.
            string fullMessage = message;
            // Если исключение есть, дописываем его текст.
            if (exception != null)
            {
                fullMessage = fullMessage + " " + exception;
            }

            // Записываем итоговую строку как ERROR.
            Write("ERROR", fullMessage);
        }

        /// <summary>
        /// Базовый метод записи строки в лог.
        /// </summary>
        /// <param name="level">Уровень сообщения.</param>
        /// <param name="message">Текст сообщения.</param>
        private void Write(string level, string message)
        {
            // Запись защищена lock, чтобы строки не перемешивались.
            lock (_syncRoot)
            {
                // Определяем каталог файла лога.
                string directory = Path.GetDirectoryName(_logFilePath);
                // Если каталога нет, создаём его.
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Дозаписываем строку в файл.
                File.AppendAllText(
                    _logFilePath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message + Environment.NewLine);
            }
        }
    }
}
