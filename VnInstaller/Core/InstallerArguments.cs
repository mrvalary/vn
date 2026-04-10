using System;
using System.IO;

namespace VnInstaller.Core
{
    public sealed class InstallerArguments
    {
        public string Command { get; private set; }
        public string TargetDirectory { get; private set; }
        public string AppExePath { get; private set; }
        public int? AppProcessId { get; private set; }

        private InstallerArguments()
        {
        }

        public static bool TryParse(string[] args, out InstallerArguments result, out string error)
        {
            result = null;
            error = null;

            if (args == null || args.Length == 0)
            {
                error = "Не указана команда. Используйте: apply --targetDir <path> --appExe <path> [--pid <id>]";
                return false;
            }

            if (!string.Equals(args[0], "apply", StringComparison.OrdinalIgnoreCase))
            {
                error = "Поддерживается только команда apply.";
                return false;
            }

            string targetDirectory = null;
            string appExePath = null;
            int? processId = null;

            for (int i = 1; i < args.Length; i++)
            {
                string key = args[i];

                if (string.Equals(key, "--targetDir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    targetDirectory = args[++i];
                    continue;
                }

                if (string.Equals(key, "--appExe", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    appExePath = args[++i];
                    continue;
                }

                if (string.Equals(key, "--pid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    int parsedValue;
                    if (!int.TryParse(args[++i], out parsedValue))
                    {
                        error = "Аргумент --pid должен быть целым числом.";
                        return false;
                    }

                    processId = parsedValue;
                    continue;
                }

                error = "Неизвестный аргумент: " + key;
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                error = "Не указан --targetDir.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(appExePath))
            {
                error = "Не указан --appExe.";
                return false;
            }

            result = new InstallerArguments
            {
                Command = "apply",
                TargetDirectory = Path.GetFullPath(targetDirectory),
                AppExePath = Path.GetFullPath(appExePath),
                AppProcessId = processId
            };

            return true;
        }

        public string GetLogFilePath()
        {
            string baseDirectory = TargetDirectory;
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(baseDirectory, "vn-installer.log");
        }
    }
}
