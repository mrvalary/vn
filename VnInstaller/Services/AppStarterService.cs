using System.Diagnostics;
using System.IO;

namespace VnInstaller.Services
{
    public sealed class AppStarterService
    {
        private readonly FileLogger _logger;

        public AppStarterService(FileLogger logger)
        {
            _logger = logger;
        }

        public void Start(string appExePath)
        {
            string workingDirectory = Path.GetDirectoryName(appExePath);
            _logger.Info("Starting application: " + appExePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = appExePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });
        }
    }
}
