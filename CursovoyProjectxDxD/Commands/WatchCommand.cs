using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команды просмотра и редактирования данных, которые присылают VnWatcher-агенты.
    /// </summary>
    public sealed class WatchCommand : ICommand
    {
        #region Metadata

        /// <summary>
        /// Имя команды в консоли.
        /// </summary>
        public string Name => "watch";

        /// <summary>
        /// Краткое описание команды для реестра команд.
        /// </summary>
        public string Description => "Мониторинг устройств через VnWatcher";

        #endregion

        #region Execute

        /// <summary>
        /// Выполняет подкоманду мониторинга с проверкой прав администратора или статиста.
        /// </summary>
        /// <param name="context">Контекст выполнения команды.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат выполнения выбранной подкоманды watch.</returns>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();
            if (!sessionService.CanManageMonitoring())
            {
                return CommandResult.Fail("Команда watch доступна только админу или статисту.");
            }

            if (context.Args == null || context.Args.Length < 2)
            {
                return CommandResult.Fail(GetUsage());
            }

            MonitoringService monitoringService = context.GetRequiredService<MonitoringService>();

            if (context.Args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteListCommandAsync(context.Args, monitoringService, cancellationToken);
            }

            if (context.Args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteShowCommandAsync(context.Args, monitoringService, cancellationToken);
            }

            if (context.Args[1].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                WatcherLauncherService watcherLauncher = context.GetRequiredService<WatcherLauncherService>();
                return ExecuteStatusCommand(context.Args, watcherLauncher);
            }

            return CommandResult.Fail(GetUsage());
        }

        #endregion

        #region Device Commands

        /// <summary>
        /// Выполняет команду просмотра списка устройств мониторинга.
        /// </summary>
        /// <param name="args">Аргументы команды watch.</param>
        /// <param name="monitoringService">Сервис работы с устройствами мониторинга.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат выполнения команды списка устройств.</returns>
        private static async Task<CommandResult> ExecuteListCommandAsync(
            string[] args,
            MonitoringService monitoringService,
            CancellationToken cancellationToken)
        {
            if (args.Length != 2)
            {
                return CommandResult.Fail("Использование: watch list");
            }

            IReadOnlyList<MonitoredDevice> devices = await monitoringService.ListDevicesAsync(cancellationToken);
            if (devices.Count == 0)
            {
                return CommandResult.Ok("Устройства мониторинга пока не добавлены.");
            }

            return CommandResult.Ok(FormatDevices(devices));
        }

        #endregion

        #region Agent Commands

        /// <summary>
        /// Показывает, запущен ли watcher-агент рядом с основным приложением.
        /// </summary>
        /// <param name="args">Аргументы команды watch.</param>
        /// <param name="watcherLauncher">Сервис проверки состояния watcher-агента.</param>
        /// <returns>Текстовый статус watcher-агента.</returns>
        private static CommandResult ExecuteStatusCommand(string[] args, WatcherLauncherService watcherLauncher)
        {
            if (args.Length != 2)
            {
                return CommandResult.Fail("Использование: watch status");
            }

            WatcherStatus status = watcherLauncher.GetStatus();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Watcher-агент:");
            builder.AppendLine("Статус: " + (status.IsRunning ? "запущен" : "не запущен"));
            builder.AppendLine("Файл: " + status.ExecutablePath);

            return CommandResult.Ok(builder.ToString().TrimEnd());
        }

        #endregion

        #region Metric Commands

        /// <summary>
        /// Показывает последние метрики CPU, RAM и HDD по устройству.
        /// </summary>
        /// <param name="args">Аргументы команды watch.</param>
        /// <param name="monitoringService">Сервис чтения метрик мониторинга.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат команды со списком метрик.</returns>
        private static async Task<CommandResult> ExecuteShowCommandAsync(
            string[] args,
            MonitoringService monitoringService,
            CancellationToken cancellationToken)
        {
            if (args.Length < 3)
            {
                return CommandResult.Fail("Использование: watch show <имя ПК> [количество]");
            }

            string deviceKey = args[2];
            int limit = args.Length >= 4 ? ParseLimit(args[3]) : 10;
            IReadOnlyList<SystemMetricRecord> metrics = await monitoringService.ListMetricsAsync(deviceKey, limit, cancellationToken);

            if (metrics.Count == 0)
            {
                return CommandResult.Ok("Для устройства '" + deviceKey + "' пока нет метрик.");
            }

            return CommandResult.Ok(FormatMetrics(metrics));
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Форматирует список устройств мониторинга для вывода в консоль.
        /// </summary>
        /// <param name="devices">Устройства мониторинга.</param>
        /// <returns>Готовый текстовый список устройств.</returns>
        private static string FormatDevices(IReadOnlyList<MonitoredDevice> devices)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Устройства мониторинга:");
            builder.AppendLine();

            foreach (MonitoredDevice device in devices)
            {
                builder.Append(device.DeviceKey);
                builder.Append(" | ");
                builder.Append(device.Name);
                builder.Append(" | ");
                builder.Append(device.LastSeenAt.HasValue ? device.LastSeenAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "нет данных");

                if (!string.IsNullOrWhiteSpace(device.Address))
                {
                    builder.Append(" | ");
                    builder.Append(device.Address);
                }

                if (!string.IsNullOrWhiteSpace(device.Description))
                {
                    builder.Append(" | ");
                    builder.Append(device.Description);
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Форматирует историю метрик устройства для вывода в консоль.
        /// </summary>
        /// <param name="metrics">Последние метрики устройства.</param>
        /// <returns>Готовая текстовая таблица метрик.</returns>
        private static string FormatMetrics(IReadOnlyList<SystemMetricRecord> metrics)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("История нагрузки: " + metrics[0].DeviceName + " (" + metrics[0].DeviceKey + ")");
            builder.AppendLine();

            foreach (SystemMetricRecord metric in metrics)
            {
                builder.Append(metric.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.Append(" | CPU ");
                builder.Append(FormatPercent(metric.CpuPercent));
                builder.Append(" | RAM ");
                builder.Append(FormatPercent(metric.RamPercent));
                builder.Append(" | HDD ");
                builder.Append(FormatPercent(metric.HddPercent));
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Разбирает ограничение количества строк истории.
        /// </summary>
        /// <param name="value">Строковое значение лимита.</param>
        /// <returns>Числовой лимит или значение по умолчанию.</returns>
        private static int ParseLimit(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 10;
        }

        /// <summary>
        /// Форматирует процентное значение для консольного вывода.
        /// </summary>
        /// <param name="value">Процентное значение.</param>
        /// <returns>Строка процента с двумя знаками после запятой.</returns>
        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.00") + "%";
        }

        /// <summary>
        /// Возвращает краткую подсказку по синтаксису команды watch.
        /// </summary>
        /// <returns>Текст использования команды watch.</returns>
        private static string GetUsage()
        {
            return
                "Команды мониторинга:\n" +
                "watch list\n" +
                "watch status\n" +
                "watch show <имя ПК> [количество]\n";
        }

        #endregion
    }
}
