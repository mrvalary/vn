using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace VnWatcher
{
    internal static class Program
    {
        #region Constants

        private const string ConnectionStringName = "WatcherDb";
        private const string YamlConfigFileName = "watcher.yml";

        #endregion

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
                Console.WriteLine("Watcher stopped with error:");
                Console.WriteLine(ex);
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

            // Настройки агента читаются один раз при запуске.
            WatcherSettings settings = WatcherSettings.Load();
            Console.WriteLine("VnWatcher started.");
            Console.WriteLine("Device key: " + settings.DeviceKey);
            Console.WriteLine("Interval: " + settings.IntervalSeconds + " sec");
            Console.WriteLine();

            // Агент работает постоянно и отправляет метрики до остановки процесса.
            while (!_stopRequested)
            {
                MetricSnapshot snapshot = MetricCollector.Collect();
                bool sent = false;

                try
                {
                    await SendMetricAsync(settings, snapshot);
                    sent = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Metric send failed: " + ex.Message);
                }

                Console.WriteLine(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    " | CPU " + FormatPercent(snapshot.CpuPercent) +
                    " | RAM " + FormatPercent(snapshot.RamPercent) +
                    " | HDD " + FormatPercent(snapshot.HddPercent) +
                    " | Sent " + sent);

                await DelayAsync(settings.IntervalSeconds, CancellationToken.None);
            }

            Console.WriteLine("VnWatcher stopped.");
            return 0;
        }

        #endregion

        #region Database

        /// <summary>
        /// Отправляет один снимок метрик в базу данных.
        /// </summary>
        /// <param name="settings">Настройки watcher-агента.</param>
        /// <param name="snapshot">Снимок нагрузки CPU, RAM и HDD.</param>
        /// <returns>Задача отправки метрики.</returns>
        private static async Task SendMetricAsync(WatcherSettings settings, MetricSnapshot snapshot)
        {
            // Функция в БД сама создает устройство, если агент прислал новый deviceKey.
            const string sql = "SELECT save_device_metric(@deviceKey, @deviceName, @cpuPercent, @ramPercent, @hddPercent);";

            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.DatabaseTimeoutSeconds)))
            using (NpgsqlConnection connection = new NpgsqlConnection(settings.ConnectionString))
            {
                await connection.OpenAsync(timeout.Token);

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.CommandTimeout = settings.DatabaseTimeoutSeconds;
                    command.Parameters.AddWithValue("deviceKey", settings.DeviceKey);
                    command.Parameters.AddWithValue("deviceName", settings.DeviceName);
                    command.Parameters.AddWithValue("cpuPercent", snapshot.CpuPercent);
                    command.Parameters.AddWithValue("ramPercent", snapshot.RamPercent);
                    command.Parameters.AddWithValue("hddPercent", snapshot.HddPercent);
                    await command.ExecuteNonQueryAsync(timeout.Token);
                }
            }
        }

        #endregion

        #region Runtime Helpers

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

        #endregion

        #region Settings

        /// <summary>
        /// Настройки watcher-агента из App.config и watcher.yml.
        /// </summary>
        private sealed class WatcherSettings
        {
            /// <summary>
            /// Строка подключения к базе данных для роли watcher.
            /// </summary>
            public string ConnectionString { get; private set; }

            /// <summary>
            /// Уникальный ключ устройства, по которому метрики сохраняются в БД.
            /// </summary>
            public string DeviceKey { get; private set; }

            /// <summary>
            /// Отображаемое имя устройства в мониторинге.
            /// </summary>
            public string DeviceName { get; private set; }

            /// <summary>
            /// Интервал между отправками метрик в секундах.
            /// </summary>
            public int IntervalSeconds { get; private set; }

            /// <summary>
            /// Таймаут обращения к базе данных в секундах.
            /// </summary>
            public int DatabaseTimeoutSeconds { get; private set; }

            /// <summary>
            /// Загружает настройки watcher-агента из конфигурационных файлов.
            /// </summary>
            /// <returns>Готовые настройки для запуска агента.</returns>
            public static WatcherSettings Load()
            {
                // Строка подключения остается в App.config, потому что это чувствительная настройка.
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];
                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new InvalidOperationException("Connection string '" + ConnectionStringName + "' is not configured.");
                }

                // Все обычные настройки агента берутся из watcher.yml.
                SimpleYamlConfig yaml = SimpleYamlConfig.Load(YamlConfigFileName);
                string machineName = Environment.MachineName;
                string deviceKey = yaml.GetValue("watcher", "deviceKey", machineName);
                string deviceName = yaml.GetValue("watcher", "deviceName", machineName);

                // По заданию агент отправляет статистику сам раз в минуту.
                int intervalSeconds = yaml.GetPositiveInt("watcher", "intervalSeconds", 60);
                int databaseTimeoutSeconds = yaml.GetPositiveInt("watcher", "databaseTimeoutSeconds", 10);

                return new WatcherSettings
                {
                    ConnectionString = settings.ConnectionString,
                    DeviceKey = deviceKey,
                    DeviceName = deviceName,
                    IntervalSeconds = intervalSeconds,
                    DatabaseTimeoutSeconds = databaseTimeoutSeconds
                };
            }

        }

        #endregion

        #region YAML Config

        /// <summary>
        /// Минимальный YAML-парсер для безопасных настроек watcher.yml.
        /// </summary>
        private sealed class SimpleYamlConfig
        {
            #region Fields

            // Секции YAML хранятся как словарь "секция -> ключ -> значение".
            private readonly Dictionary<string, Dictionary<string, string>> _sections =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            #endregion

            #region Load

            /// <summary>
            /// Загружает YAML-файл из папки приложения или текущей рабочей папки.
            /// </summary>
            /// <param name="fileName">Имя YAML-файла.</param>
            /// <returns>Прочитанная конфигурация или пустая конфигурация, если файл отсутствует.</returns>
            public static SimpleYamlConfig Load(string fileName)
            {
                SimpleYamlConfig config = new SimpleYamlConfig();
                string path = ResolvePath(fileName);

                // Отсутствующий YAML не считается ошибкой: агент продолжит работу с дефолтами.
                if (!File.Exists(path))
                {
                    return config;
                }

                string currentSection = string.Empty;
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    // Поддерживаем комментарии через # и пустые строки.
                    string line = StripComment(rawLine).TrimEnd();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Строка без отступа и с двоеточием в конце открывает новую секцию.
                    if (!char.IsWhiteSpace(rawLine, 0) && line.EndsWith(":", StringComparison.Ordinal))
                    {
                        currentSection = line.Substring(0, line.Length - 1).Trim();
                        if (!config._sections.ContainsKey(currentSection))
                        {
                            config._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }

                        continue;
                    }

                    int delimiterIndex = line.IndexOf(':');
                    if (delimiterIndex <= 0 || string.IsNullOrWhiteSpace(currentSection))
                    {
                        continue;
                    }

                    // Внутри секции ожидается простой формат key: value.
                    string key = line.Substring(0, delimiterIndex).Trim();
                    string value = line.Substring(delimiterIndex + 1).Trim();
                    config._sections[currentSection][key] = Unquote(value);
                }

                return config;
            }

            #endregion

            #region Accessors

            /// <summary>
            /// Возвращает строковое значение настройки из указанной секции.
            /// </summary>
            /// <param name="section">Название секции YAML.</param>
            /// <param name="key">Название ключа внутри секции.</param>
            /// <param name="fallback">Значение по умолчанию.</param>
            /// <returns>Значение из YAML или fallback, если ключ не найден.</returns>
            public string GetValue(string section, string key, string fallback)
            {
                Dictionary<string, string> values;
                string value;
                if (_sections.TryGetValue(section, out values) &&
                    values.TryGetValue(key, out value) &&
                    !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }

                return fallback;
            }

            /// <summary>
            /// Возвращает положительное целое значение настройки.
            /// </summary>
            /// <param name="section">Название секции YAML.</param>
            /// <param name="key">Название ключа внутри секции.</param>
            /// <param name="fallback">Значение по умолчанию.</param>
            /// <returns>Положительное число из YAML или fallback.</returns>
            public int GetPositiveInt(string section, string key, int fallback)
            {
                int value;
                return int.TryParse(GetValue(section, key, fallback.ToString()), out value) && value > 0
                    ? value
                    : fallback;
            }

            #endregion

            #region Helpers

            /// <summary>
            /// Определяет путь к YAML-файлу рядом с exe или в текущей рабочей папке.
            /// </summary>
            /// <param name="fileName">Имя YAML-файла.</param>
            /// <returns>Полный путь к файлу конфигурации.</returns>
            private static string ResolvePath(string fileName)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string basePath = Path.Combine(baseDirectory, fileName);
                return File.Exists(basePath) ? basePath : Path.Combine(Environment.CurrentDirectory, fileName);
            }

            /// <summary>
            /// Удаляет YAML-комментарий из строки.
            /// </summary>
            /// <param name="line">Исходная строка.</param>
            /// <returns>Строка без части после символа #.</returns>
            private static string StripComment(string line)
            {
                int commentIndex = line.IndexOf('#');
                return commentIndex < 0 ? line : line.Substring(0, commentIndex);
            }

            /// <summary>
            /// Убирает одинарные или двойные кавычки вокруг значения.
            /// </summary>
            /// <param name="value">Значение из YAML.</param>
            /// <returns>Значение без внешних кавычек.</returns>
            private static string Unquote(string value)
            {
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[value.Length - 1] == '"') ||
                     (value[0] == '\'' && value[value.Length - 1] == '\'')))
                {
                    return value.Substring(1, value.Length - 2);
                }

                return value;
            }

            #endregion
        }

        #endregion

        #region Metrics

        /// <summary>
        /// Снимок текущей нагрузки устройства.
        /// </summary>
        private sealed class MetricSnapshot
        {
            /// <summary>
            /// Текущая загрузка процессора в процентах.
            /// </summary>
            public decimal CpuPercent { get; set; }

            /// <summary>
            /// Текущая занятость оперативной памяти в процентах.
            /// </summary>
            public decimal RamPercent { get; set; }

            /// <summary>
            /// Текущая занятость локальных дисков в процентах.
            /// </summary>
            public decimal HddPercent { get; set; }
        }

        /// <summary>
        /// Сборщик системных метрик устройства.
        /// </summary>
        private static class MetricCollector
        {
            /// <summary>
            /// Собирает текущие значения CPU, RAM и HDD.
            /// </summary>
            /// <returns>Снимок нагрузки устройства.</returns>
            public static MetricSnapshot Collect()
            {
                return new MetricSnapshot
                {
                    CpuPercent = GetCpuPercent(),
                    RamPercent = GetRamPercent(),
                    HddPercent = GetHddPercent()
                };
            }

            /// <summary>
            /// Считает загрузку CPU по разнице системных счетчиков Windows.
            /// </summary>
            /// <returns>Загрузка CPU в процентах.</returns>
            private static decimal GetCpuPercent()
            {
                try
                {
                    // GetSystemTimes работает быстро и не зависит от счетчиков производительности Windows.
                    if (!TryReadCpuTimes(out ulong idleStart, out ulong kernelStart, out ulong userStart))
                    {
                        return 0;
                    }

                    Thread.Sleep(500);

                    if (!TryReadCpuTimes(out ulong idleEnd, out ulong kernelEnd, out ulong userEnd))
                    {
                        return 0;
                    }

                    ulong idleDelta = idleEnd - idleStart;
                    ulong kernelDelta = kernelEnd - kernelStart;
                    ulong userDelta = userEnd - userStart;
                    ulong totalDelta = kernelDelta + userDelta;

                    return totalDelta == 0
                        ? 0
                        : ClampPercent((decimal)(totalDelta - idleDelta) * 100 / totalDelta);
                }
                catch
                {
                    return 0;
                }
            }

            /// <summary>
            /// Считает занятость физической памяти через Windows API.
            /// </summary>
            /// <returns>Занятость RAM в процентах.</returns>
            private static decimal GetRamPercent()
            {
                MemoryStatusEx memoryStatus = new MemoryStatusEx();
                if (!GlobalMemoryStatusEx(memoryStatus) || memoryStatus.TotalPhys == 0)
                {
                    return 0;
                }

                ulong used = memoryStatus.TotalPhys - memoryStatus.AvailPhys;
                return ClampPercent((decimal)used * 100 / memoryStatus.TotalPhys);
            }

            /// <summary>
            /// Считает суммарную занятость всех готовых локальных дисков.
            /// </summary>
            /// <returns>Занятость HDD в процентах.</returns>
            private static decimal GetHddPercent()
            {
                try
                {
                    long totalBytes = 0;
                    long freeBytes = 0;

                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                        {
                            totalBytes += drive.TotalSize;
                            freeBytes += drive.AvailableFreeSpace;
                        }
                    }

                    return totalBytes <= 0 ? 0 : ClampPercent(((decimal)(totalBytes - freeBytes)) * 100 / totalBytes);
                }
                catch
                {
                    return 0;
                }
            }

            /// <summary>
            /// Ограничивает процент диапазоном от 0 до 100 и округляет значение.
            /// </summary>
            /// <param name="value">Исходное процентное значение.</param>
            /// <returns>Нормализованный процент.</returns>
            private static decimal ClampPercent(decimal value)
            {
                if (value < 0)
                {
                    return 0;
                }

                if (value > 100)
                {
                    return 100;
                }

                return Math.Round(value, 2);
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

            /// <summary>
            /// Читает системные счетчики времени CPU.
            /// </summary>
            /// <param name="idle">Время простоя CPU.</param>
            /// <param name="kernel">Время работы ядра.</param>
            /// <param name="user">Время пользовательских процессов.</param>
            /// <returns>true, если счетчики удалось прочитать.</returns>
            private static bool TryReadCpuTimes(out ulong idle, out ulong kernel, out ulong user)
            {
                FileTime idleTime;
                FileTime kernelTime;
                FileTime userTime;

                if (!GetSystemTimes(out idleTime, out kernelTime, out userTime))
                {
                    idle = 0;
                    kernel = 0;
                    user = 0;
                    return false;
                }

                idle = ToUInt64(idleTime);
                kernel = ToUInt64(kernelTime);
                user = ToUInt64(userTime);
                return true;
            }

            /// <summary>
            /// Преобразует структуру FILETIME в 64-битное число.
            /// </summary>
            /// <param name="value">Значение FILETIME.</param>
            /// <returns>64-битное представление FILETIME.</returns>
            private static ulong ToUInt64(FileTime value)
            {
                return ((ulong)value.HighDateTime << 32) | value.LowDateTime;
            }

            /// <summary>
            /// Структура FILETIME из Windows API.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct FileTime
            {
                public uint LowDateTime;
                public uint HighDateTime;
            }

            /// <summary>
            /// Структура MEMORYSTATUSEX из Windows API.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            private sealed class MemoryStatusEx
            {
                public uint Length;
                public uint MemoryLoad;
                public ulong TotalPhys;
                public ulong AvailPhys;
                public ulong TotalPageFile;
                public ulong AvailPageFile;
                public ulong TotalVirtual;
                public ulong AvailVirtual;
                public ulong AvailExtendedVirtual;

                /// <summary>
                /// Создает структуру и заполняет поле Length, обязательное для GlobalMemoryStatusEx.
                /// </summary>
                public MemoryStatusEx()
                {
                    Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
                }
            }
        }

        #endregion
    }
}
