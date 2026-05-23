using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace VnWatcher
{
    /// <summary>
    /// Сборщик системных метрик устройства.
    /// </summary>
    internal static class MetricCollector
    {
        #region Public API

        /// <summary>
        /// Собирает текущие значения CPU, RAM и HDD.
        /// </summary>
        /// <returns>Снимок нагрузки устройства.</returns>
        public static MetricSnapshot Collect(string hddPath)
        {
            return new MetricSnapshot
            {
                CpuPercent = GetCpuPercent(),
                RamPercent = GetRamPercent(),
                HddPercent = GetHddPercent(hddPath)
            };
        }

        #endregion

        #region Metric Readers

        /// <summary>
        /// Считает загрузку CPU по разнице системных счетчиков Windows.
        /// </summary>
        /// <returns>Загрузка CPU в процентах.</returns>
        private static decimal GetCpuPercent()
        {
            try
            {
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
        /// Считает занятость выбранного диска или всех готовых локальных дисков.
        /// </summary>
        /// <param name="hddPath">Путь диска из watcher.yml. Пустое значение означает все локальные диски.</param>
        /// <returns>Занятость HDD в процентах.</returns>
        private static decimal GetHddPercent(string hddPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hddPath))
                {
                    return GetSelectedDrivePercent(hddPath);
                }

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

        #endregion

        #region Helpers

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

        /// <summary>
        /// Считает занятость конкретного диска, выбранного в watcher.yml.
        /// </summary>
        /// <param name="hddPath">Путь диска, например C:\ или D:\data.</param>
        /// <returns>Занятость выбранного диска в процентах.</returns>
        private static decimal GetSelectedDrivePercent(string hddPath)
        {
            string rootPath = Path.GetPathRoot(hddPath.Trim());
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return 0;
            }

            DriveInfo drive = new DriveInfo(rootPath);
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.TotalSize <= 0)
            {
                return 0;
            }

            return ClampPercent(((decimal)(drive.TotalSize - drive.AvailableFreeSpace)) * 100 / drive.TotalSize);
        }

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

        #endregion

        #region Native API

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

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

        #endregion
    }
}
