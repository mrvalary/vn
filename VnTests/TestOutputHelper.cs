using System;
using System.IO;
using System.Text;

namespace VnTests
{
    /// <summary>
    /// Помогает выводить подробные результаты тестов в консоль тестового запуска и отдельный файл.
    /// </summary>
    internal static class TestOutputHelper
    {
        // Блокировка нужна, чтобы несколько тестов не записали строки в файл одновременно.
        private static readonly object SyncRoot = new object();

        // Флаг показывает, что файл отчета уже подготовлен для текущего запуска тестов.
        private static bool _isInitialized;

        #region Public Methods

        /// <summary>
        /// Выводит строку в стандартный вывод тестов и в текстовый файл отчета.
        /// </summary>
        /// <param name="text">Текст строки отчета.</param>
        public static void WriteLine(string text)
        {
            Console.WriteLine(text);

            lock (SyncRoot)
            {
                EnsureInitialized();
                File.AppendAllText(GetReportPath(), text + Environment.NewLine, Encoding.UTF8);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Подготавливает файл отчета перед первой записью.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            string reportPath = GetReportPath();
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(
                reportPath,
                "Подробный отчет выполнения unit-тестов VN" + Environment.NewLine +
                "Дата запуска: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine +
                new string('=', 60) + Environment.NewLine,
                Encoding.UTF8);

            _isInitialized = true;
        }

        /// <summary>
        /// Возвращает путь к файлу подробного отчета тестов.
        /// </summary>
        /// <returns>Путь к файлу vn-test-output.txt.</returns>
        private static string GetReportPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults", "vn-test-output.txt");
        }

        #endregion
    }
}
