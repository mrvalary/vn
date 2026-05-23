using System.Diagnostics;
using System.Threading;

namespace VnInstaller.Services
{
    /// <summary>
    /// Сервис ожидания завершения основного процесса приложения.
    /// </summary>
    public sealed class ProcessWaitService
    {
        // Логгер нужен для записи этапов ожидания.
        private readonly FileLogger _logger;

        /// <summary>
        /// Создает сервис ожидания процесса.
        /// </summary>
        /// <param name="logger">Логгер установщика.</param>
        public ProcessWaitService(FileLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ждёт завершения процесса по PID.
        /// </summary>
        /// <param name="processId">PID процесса приложения.</param>
        public void WaitForExit(int? processId)
        {
            // Если PID не передан, установщик запущен вручную и ждать нечего.
            if (!processId.HasValue)
            {
                _logger.Info("Process id is not specified. Wait step skipped.");
                return;
            }

            // Фиксируем начало ожидания в логе.
            _logger.Info("Waiting for application process to exit. PID=" + processId.Value);

            // Проверяем процесс до 60 раз с интервалом в секунду.
            for (int attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    // Пытаемся получить процесс по PID.
                    Process process = Process.GetProcessById(processId.Value);
                    // Если процесс уже завершён, дальше ждать не нужно.
                    if (process.HasExited)
                    {
                        _logger.Info("Application process has exited.");
                        return;
                    }
                }
                catch
                {
                    // Если процесс уже не найден, считаем ожидание завершённым.
                    _logger.Info("Application process is no longer running.");
                    return;
                }

                // Даём процессу ещё секунду на завершение.
                Thread.Sleep(1000);
            }

            // Если таймаут истёк, продолжаем сценарий и пишем это в лог.
            _logger.Info("Timeout while waiting for application process. Continue deployment.");
        }
    }
}
