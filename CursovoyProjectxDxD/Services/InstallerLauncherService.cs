using System;
using System.Diagnostics;
using System.IO;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Сервис запуска внешнего установщика.
    /// </summary>
    public sealed class InstallerLauncherService
    {
        // Имя exe-файла установщика.
        private const string InstallerExeName = "vn-installer.exe";
        // Имя файла с PID обновляемого процесса.
        private const string ProcessIdFileName = "vn-app.pid";

        /// <summary>
        /// Запускает установщик из временной папки в единственном режиме без аргументов.
        /// </summary>
        public bool Launch()
        {
            // Получаем путь к текущему exe основного приложения.
            string appExePath = Process.GetCurrentProcess().MainModule.FileName;
            // Определяем каталог, где лежат файлы приложения и установщика.
            string sourceDirectory = Path.GetDirectoryName(appExePath);
            // Строим путь к оригинальному vn-installer.exe рядом с приложением.
            string sourceInstallerPath = Path.Combine(sourceDirectory, InstallerExeName);

            // Если установщик не найден, запуск невозможен.
            if (!File.Exists(sourceInstallerPath))
            {
                return false;
            }

            // Считываем PID текущего процесса, чтобы установщик ждал именно этот экземпляр.
            int currentProcessId = Process.GetCurrentProcess().Id;
            // Подготавливаем временную папку запуска и записываем туда PID.
            string runtimeDirectory = PrepareRuntimeDirectory(sourceDirectory, currentProcessId);
            // Получаем путь к временной копии установщика.
            string runtimeInstallerPath = Path.Combine(runtimeDirectory, InstallerExeName);

            // Запускаем временную копию установщика.
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = runtimeInstallerPath,
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = true
            });

            return process != null;
        }

        /// <summary>
        /// Подготавливает каталог runtime во временной папке.
        /// </summary>
        private static string PrepareRuntimeDirectory(string sourceDirectory, int processId)
        {
            // Временная папка нужна, чтобы установщик не держал локи на свои файлы в каталоге приложения.
            string runtimeDirectory = Path.Combine(Path.GetTempPath(), "vn-installer-runtime");

            // Перед каждым запуском очищаем старую временную папку.
            if (Directory.Exists(runtimeDirectory))
            {
                Directory.Delete(runtimeDirectory, true);
            }

            // Создаём чистую директорию runtime.
            Directory.CreateDirectory(runtimeDirectory);
            // Копируем в неё exe, config и dll установщика.
            CopyInstallerRuntime(sourceDirectory, runtimeDirectory);
            // Сохраняем PID обновляемого процесса в служебный файл.
            WriteProcessIdFile(runtimeDirectory, processId);
            return runtimeDirectory;
        }

        /// <summary>
        /// Копирует нужные файлы во временную директорию.
        /// </summary>
        private static void CopyInstallerRuntime(string sourceDirectory, string runtimeDirectory)
        {
            // Берём только файлы верхнего уровня рядом с приложением.
            string[] runtimeFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly);

            // Перебираем найденные файлы.
            for (int i = 0; i < runtimeFiles.Length; i++)
            {
                // Получаем расширение текущего файла.
                string extension = Path.GetExtension(runtimeFiles[i]);
                // Получаем имя файла без пути.
                string fileName = Path.GetFileName(runtimeFiles[i]);

                // Пропускаем всё, что не нужно для запуска установщика.
                if (!string.Equals(fileName, InstallerExeName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "vn-installer.exe.config", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "vn-installer.pdb", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Формируем путь назначения во временной папке.
                string targetPath = Path.Combine(runtimeDirectory, fileName);
                // Копируем файл с перезаписью.
                File.Copy(runtimeFiles[i], targetPath, true);
            }
        }

        /// <summary>
        /// Записывает PID текущего приложения в служебный файл для установщика.
        /// </summary>
        private static void WriteProcessIdFile(string runtimeDirectory, int processId)
        {
            // Путь к служебному файлу во временной папке.
            string processIdFilePath = Path.Combine(runtimeDirectory, ProcessIdFileName);
            // Сохраняем PID как обычный текст.
            File.WriteAllText(processIdFilePath, processId.ToString());
        }
    }
}
