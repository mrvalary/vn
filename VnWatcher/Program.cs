using System;
using System.Threading;
using System.Threading.Tasks;

namespace VnWatcher
{
    /// <summary>
    /// Консольный агент сбора и отправки метрик CPU, RAM и HDD.
    /// </summary>
    internal static class Program
    {
        #region State

        private static bool _stopRequested;

        #endregion

        #region Entry Point

        /// <summary>
        /// Точка входа watcher-агента.
        /// </summary>
        /// <returns>Код завершения процесса.</returns>
        private static int Main()
        {
            try
            {
                return MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Watcher-агент остановлен из-за ошибки:");
                Console.WriteLine("Подробности: " + ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Запускает постоянный цикл сбора и отправки метрик устройства.
        /// </summary>
        /// <returns>Код успешного завершения процесса.</returns>
        private static async Task<int> MainAsync()
        {
            Console.Title = "vn-watcher";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += OnCancelKeyPress;

            WatcherSettings settings = WatcherSettings.Load();
            MetricSender sender = new MetricSender(settings);

            PrintStartupInfo(settings);

            while (!_stopRequested)
            {
                MetricSnapshot snapshot = MetricCollector.Collect(settings.HddPath);
                bool sent = await TrySendMetricAsync(sender, snapshot);

                PrintMetric(snapshot, sent);
                await DelayAsync(settings.IntervalSeconds, CancellationToken.None);
            }

            Console.WriteLine("Watcher-агент остановлен.");
            return 0;
        }

        #endregion

        #region Runtime Helpers

        /// <summary>
        /// Печатает параметры запуска агента.
        /// </summary>
        /// <param name="settings">Настройки watcher-агента.</param>
        private static void PrintStartupInfo(WatcherSettings settings)
        {
            Console.WriteLine("Watcher-агент запущен.");
            Console.WriteLine("Имя компьютера: " + settings.ComputerName);
            Console.WriteLine("Интервал отправки: " + settings.IntervalSeconds + " сек.");
            Console.WriteLine("Диск для HDD-метрики: " + FormatHddPath(settings.HddPath));
            Console.WriteLine();
        }

        /// <summary>
        /// Отправляет метрику и преобразует ошибку отправки в сообщение консоли.
        /// </summary>
        /// <param name="sender">Отправитель метрик в PostgreSQL.</param>
        /// <param name="snapshot">Снимок текущей нагрузки.</param>
        /// <returns>true, если метрика успешно отправлена.</returns>
        private static async Task<bool> TrySendMetricAsync(MetricSender sender, MetricSnapshot snapshot)
        {
            try
            {
                await sender.SendAsync(snapshot);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось отправить метрику: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Печатает строку с текущими метриками и статусом отправки.
        /// </summary>
        /// <param name="snapshot">Снимок текущей нагрузки.</param>
        /// <param name="sent">Признак успешной отправки в БД.</param>
        private static void PrintMetric(MetricSnapshot snapshot, bool sent)
        {
            Console.WriteLine(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                " | CPU " + FormatPercent(snapshot.CpuPercent) +
                " | RAM " + FormatPercent(snapshot.RamPercent) +
                " | HDD " + FormatPercent(snapshot.HddPercent) +
                " | Отправка: " + FormatSendStatus(sent));
        }

        /// <summary>
        /// Ожидает следующий цикл отправки, но позволяет остановить агент раньше.
        /// </summary>
        /// <param name="intervalSeconds">Интервал ожидания в секундах.</param>
        /// <param name="cancellationToken">Токен отмены ожидания.</param>
        /// <returns>Задача ожидания.</returns>
        private static async Task DelayAsync(int intervalSeconds, CancellationToken cancellationToken)
        {
            int remaining = Math.Max(intervalSeconds, 1) * 10;
            while (remaining > 0 && !_stopRequested)
            {
                await Task.Delay(100, cancellationToken);
                remaining--;
            }
        }

        /// <summary>
        /// Обрабатывает Ctrl+C и переводит агент в режим штатной остановки.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события отмены консоли.</param>
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _stopRequested = true;
        }

        /// <summary>
        /// Форматирует процентное значение для консольного вывода.
        /// </summary>
        /// <param name="value">Процентное значение.</param>
        /// <returns>Строка процента с двумя знаками после запятой.</returns>
        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.00") + "%";
        }

        /// <summary>
        /// Форматирует признак отправки метрики без английских True/False.
        /// </summary>
        /// <param name="sent">Признак успешной отправки.</param>
        /// <returns>Русский статус отправки.</returns>
        private static string FormatSendStatus(bool sent)
        {
            return sent ? "успешно" : "ошибка";
        }

        /// <summary>
        /// Форматирует путь диска из watcher.yml для стартового сообщения.
        /// </summary>
        /// <param name="hddPath">Путь диска или пустая строка.</param>
        /// <returns>Понятное описание источника HDD-метрики.</returns>
        private static string FormatHddPath(string hddPath)
        {
            return string.IsNullOrWhiteSpace(hddPath) ? "все локальные диски" : hddPath;
        }

        #endregion
    }
}
