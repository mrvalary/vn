using System.Diagnostics;
using System.Threading;

namespace VnInstaller.Services
{
    public sealed class ProcessWaitService
    {
        private readonly FileLogger _logger;

        public ProcessWaitService(FileLogger logger)
        {
            _logger = logger;
        }

        public void WaitForExit(int? processId)
        {
            if (!processId.HasValue)
            {
                _logger.Info("Process id is not specified. Wait step skipped.");
                return;
            }

            _logger.Info("Waiting for application process to exit. PID=" + processId.Value);

            for (int attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    Process process = Process.GetProcessById(processId.Value);
                    if (process.HasExited)
                    {
                        _logger.Info("Application process has exited.");
                        return;
                    }
                }
                catch
                {
                    _logger.Info("Application process is no longer running.");
                    return;
                }

                Thread.Sleep(1000);
            }

            _logger.Info("Timeout while waiting for application process. Continue deployment.");
        }
    }
}
