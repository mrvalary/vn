using System.Diagnostics;
using System.IO;

namespace CursovoyProjectxDxD.Services
{
    public sealed class InstallerLauncherService
    {
        private const string InstallerExeName = "vn-installer.exe";

        public bool Launch(string targetDirectory, string appExePath, int appProcessId)
        {
            string installerPath = Path.Combine(targetDirectory, InstallerExeName);
            if (!File.Exists(installerPath))
            {
                return false;
            }

            // Основное приложение передаёт установщику только контекст запуска.
            // Вся логика обновления выполняется внутри VnInstaller.
            string arguments =
                "apply " +
                "--targetDir " + Quote(targetDirectory) + " " +
                "--appExe " + Quote(appExePath) + " " +
                "--pid " + appProcessId.ToString();

            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = arguments,
                WorkingDirectory = targetDirectory,
                UseShellExecute = true
            });

            return process != null;
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
