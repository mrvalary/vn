using System;
using System.Diagnostics;
using System.IO;

namespace CursovoyProjectxDxD.Services
{
    // Сервис запуска внешнего установщика.
    public sealed class InstallerLauncherService
    {
        // Имя exe-файла установщика.
        private const string InstallerExeName = "vn-installer.exe";

        // Запускает установщик из временной папки.
        public bool Launch(string targetDirectory, string appExePath, int appProcessId)
        {
            // Путь к оригинальному установщику рядом с приложением.
            string sourceInstallerPath = Path.Combine(targetDirectory, InstallerExeName);
            // Если установщик отсутствует, запуск невозможен.
            if (!File.Exists(sourceInstallerPath))
            {
                return false;
            }

            // Создаём временную рабочую копию установщика.
            string runtimeDirectory = PrepareRuntimeDirectory(targetDirectory);
            // Путь к exe во временной папке.
            string runtimeInstallerPath = Path.Combine(runtimeDirectory, InstallerExeName);

            // Формируем аргументы режима apply.
            string arguments =
                "apply " +
                "--targetDir " + Quote(targetDirectory) + " " +
                "--appExe " + Quote(appExePath) + " " +
                "--pid " + appProcessId.ToString();

            // Запускаем процесс установщика.
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = runtimeInstallerPath,
                Arguments = arguments,
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = true
            });

            // Возвращаем признак успешного старта.
            return process != null;
        }

        // Подготавливает каталог runtime во временной папке.
        private static string PrepareRuntimeDirectory(string sourceDirectory)
        {
            // Формируем путь к временной папке.
            string runtimeDirectory = Path.Combine(Path.GetTempPath(), "vn-installer-runtime");

            // Если папка уже существовала, удаляем её целиком.
            if (Directory.Exists(runtimeDirectory))
            {
                Directory.Delete(runtimeDirectory, true);
            }

            // Создаём чистую директорию.
            Directory.CreateDirectory(runtimeDirectory);

            // Копируем в неё установщик и его зависимости.
            CopyInstallerRuntime(sourceDirectory, runtimeDirectory);
            return runtimeDirectory;
        }

        // Копирует нужные файлы во временную директорию.
        private static void CopyInstallerRuntime(string sourceDirectory, string runtimeDirectory)
        {
            // Берём только файлы верхнего уровня.
            string[] runtimeFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly);

            // Перебираем все найденные файлы.
            for (int i = 0; i < runtimeFiles.Length; i++)
            {
                // Получаем расширение текущего файла.
                string extension = Path.GetExtension(runtimeFiles[i]);
                // Получаем имя файла без пути.
                string fileName = Path.GetFileName(runtimeFiles[i]);

                // Пропускаем всё, что не нужно для старта установщика.
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

        // Заключает строку в двойные кавычки.
        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
