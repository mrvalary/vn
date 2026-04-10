using System;
using System.IO;

namespace VnInstaller.Services
{
    // Простой потокобезопасный файловый логгер.
    public sealed class FileLogger
    {
        // Путь к файлу лога.
        private readonly string _logFilePath;
        // Объект синхронизации для параллельных записей.
        private readonly object _syncRoot = new object();

        // Сохраняем путь к файлу лога.
        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        // Пишет информационное сообщение.
        public void Info(string message)
        {
            Write("INFO", message);
        }

        // Пишет сообщение об ошибке вместе с исключением.
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

        // Базовый метод записи строки в лог.
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
