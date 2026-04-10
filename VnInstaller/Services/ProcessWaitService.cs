using System.Diagnostics;
using System.Threading;

namespace VnInstaller.Services
{
    // Сервис ожидания завершения основного процесса приложения.
    public sealed class ProcessWaitService
    {
        // Логгер этапа ожидания.
        private readonly FileLogger _logger;

        // Получаем логгер через конструктор.
        public ProcessWaitService(FileLogger logger)
        {
            _logger = logger;
        }

        // Ждёт завершения процесса по PID.
        public void WaitForExit(int? processId)
        {
            // Если PID не задан, ожидание не требуется.
            if (!processId.HasValue)
            {
                _logger.Info("Process id is not specified. Wait step skipped.");
                return;
            }

            // Пишем в лог, что начали ожидание.
            _logger.Info("Waiting for application process to exit. PID=" + processId.Value);

            // Делаем до 60 проверок с интервалом в одну секунду.
            for (int attempt = 0; attempt < 60; attempt++)
            {
                try
                {
                    // Пытаемся получить процесс по PID.
                    Process process = Process.GetProcessById(processId.Value);
                    // Если процесс уже завершился, выходим.
                    if (process.HasExited)
                    {
                        _logger.Info("Application process has exited.");
                        return;
                    }
                }
                catch
                {
                    // Если процесс уже не существует, считаем ожидание завершённым.
                    _logger.Info("Application process is no longer running.");
                    return;
                }

                // Даём процессу ещё секунду на завершение.
                Thread.Sleep(1000);
            }

            // Если время вышло, продолжаем сценарий и фиксируем это в логе.
            _logger.Info("Timeout while waiting for application process. Continue deployment.");
        }
    }
}
