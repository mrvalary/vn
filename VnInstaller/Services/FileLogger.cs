using System;
using System.IO;

namespace VnInstaller.Services
{
    public sealed class FileLogger
    {
        private readonly string _logFilePath;
        private readonly object _syncRoot = new object();

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Error(string message, Exception exception)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage = fullMessage + " " + exception;
            }

            Write("ERROR", fullMessage);
        }

        private void Write(string level, string message)
        {
            lock (_syncRoot)
            {
                string directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(
                    _logFilePath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message + Environment.NewLine);
            }
        }
    }
}
