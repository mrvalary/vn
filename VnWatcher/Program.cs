using System;
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

        #endregion

        #region State

        private static bool _stopRequested;

        #endregion

        #region Entry Point

        private static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Watcher stopped with error:");
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            Console.Title = "vn-watcher";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += OnCancelKeyPress;

            WatcherSettings settings = WatcherSettings.Load(args);
            Console.WriteLine("VnWatcher started.");
            Console.WriteLine("Device key: " + settings.DeviceKey);
            Console.WriteLine("Interval: " + settings.IntervalSeconds + " sec");
            Console.WriteLine("Collect only: " + settings.CollectOnly);
            Console.WriteLine();

            do
            {
                MetricSnapshot snapshot = MetricCollector.Collect();
                bool sent = false;
                if (!settings.CollectOnly)
                {
                    try
                    {
                        await SendMetricAsync(settings, snapshot);
                        sent = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Metric send failed: " + ex.Message);
                        if (settings.RunOnce)
                        {
                            return 1;
                        }
                    }
                }

                Console.WriteLine(
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    " | CPU " + FormatPercent(snapshot.CpuPercent) +
                    " | RAM " + FormatPercent(snapshot.RamPercent) +
                    " | HDD " + FormatPercent(snapshot.HddPercent) +
                    " | Sent " + sent);

                if (settings.RunOnce)
                {
                    break;
                }

                await DelayAsync(settings.IntervalSeconds, CancellationToken.None);
            }
            while (!_stopRequested);

            Console.WriteLine("VnWatcher stopped.");
            return 0;
        }

        #endregion

        #region Database

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

        private static async Task DelayAsync(int intervalSeconds, CancellationToken cancellationToken)
        {
            int remaining = Math.Max(intervalSeconds, 1) * 10;
            while (remaining > 0 && !_stopRequested)
            {
                await Task.Delay(100, cancellationToken);
                remaining--;
            }
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _stopRequested = true;
        }

        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.00") + "%";
        }

        #endregion

        #region Settings

        private sealed class WatcherSettings
        {
            public string ConnectionString { get; private set; }
            public string DeviceKey { get; private set; }
            public string DeviceName { get; private set; }
            public int IntervalSeconds { get; private set; }
            public int DatabaseTimeoutSeconds { get; private set; }
            public bool RunOnce { get; private set; }
            public bool CollectOnly { get; private set; }

            public static WatcherSettings Load(string[] args)
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];
                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new InvalidOperationException("Connection string '" + ConnectionStringName + "' is not configured.");
                }

                string machineName = Environment.MachineName;
                string deviceKey = ReadAppSetting("DeviceKey", machineName);
                string deviceName = ReadAppSetting("DeviceName", machineName);

                // По заданию агент отправляет статистику сам раз в минуту.
                int intervalSeconds;
                if (!int.TryParse(ReadAppSetting("IntervalSeconds", "60"), out intervalSeconds) || intervalSeconds <= 0)
                {
                    intervalSeconds = 60;
                }

                int databaseTimeoutSeconds;
                if (!int.TryParse(ReadAppSetting("DatabaseTimeoutSeconds", "10"), out databaseTimeoutSeconds) || databaseTimeoutSeconds <= 0)
                {
                    databaseTimeoutSeconds = 10;
                }

                bool collectOnly = HasArgument(args, "--collect-only") || HasArgument(args, "--no-send");

                bool runOnce;
                bool.TryParse(ReadAppSetting("RunOnce", "false"), out runOnce);
                runOnce = runOnce || collectOnly || HasArgument(args, "--once");

                return new WatcherSettings
                {
                    ConnectionString = settings.ConnectionString,
                    DeviceKey = deviceKey,
                    DeviceName = deviceName,
                    IntervalSeconds = intervalSeconds,
                    DatabaseTimeoutSeconds = databaseTimeoutSeconds,
                    RunOnce = runOnce,
                    CollectOnly = collectOnly
                };
            }

            private static string ReadAppSetting(string key, string fallback)
            {
                string value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            }

            private static bool HasArgument(string[] args, string expected)
            {
                if (args == null)
                {
                    return false;
                }

                foreach (string arg in args)
                {
                    if (string.Equals(arg, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion

        #region Metrics

        private sealed class MetricSnapshot
        {
            public decimal CpuPercent { get; set; }
            public decimal RamPercent { get; set; }
            public decimal HddPercent { get; set; }
        }

        private static class MetricCollector
        {
            public static MetricSnapshot Collect()
            {
                return new MetricSnapshot
                {
                    CpuPercent = GetCpuPercent(),
                    RamPercent = GetRamPercent(),
                    HddPercent = GetHddPercent()
                };
            }

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

            private static ulong ToUInt64(FileTime value)
            {
                return ((ulong)value.HighDateTime << 32) | value.LowDateTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct FileTime
            {
                public uint LowDateTime;
                public uint HighDateTime;
            }

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

                public MemoryStatusEx()
                {
                    Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
                }
            }
        }

        #endregion
    }
}
