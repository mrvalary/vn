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
            SecurityLogService securityLogService = context.GetRequiredService<SecurityLogService>();

            if (context.Args[1].Equals("add", StringComparison.OrdinalIgnoreCase) ||
                context.Args[1].Equals("list", StringComparison.OrdinalIgnoreCase) ||
                context.Args[1].Equals("del", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteDeviceCommandAsync(context.Args, monitoringService, securityLogService, cancellationToken);
            }

            if (context.Args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteShowCommandAsync(context.Args, monitoringService, cancellationToken);
            }

            return CommandResult.Fail(GetUsage());
        }

        #endregion

        #region Device Commands

        /// <summary>
        /// Выполняет команды добавления, просмотра списка и удаления устройств мониторинга.
        /// </summary>
        /// <param name="args">Аргументы команды watch.</param>
        /// <param name="monitoringService">Сервис работы с устройствами мониторинга.</param>
        /// <param name="securityLogService">Сервис записи событий безопасности.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат выполнения команды устройства.</returns>
        private static async Task<CommandResult> ExecuteDeviceCommandAsync(
            string[] args,
            MonitoringService monitoringService,
            SecurityLogService securityLogService,
            CancellationToken cancellationToken)
        {
            if (args[1].Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    return CommandResult.Fail("Использование: watch add <deviceKey> <name> [address] [description]");
                }

                string deviceKey = args[2];
                string name = args[3];
                string address = args.Length >= 5 ? args[4] : null;
                string description = args.Length >= 6 ? string.Join(" ", args, 5, args.Length - 5) : null;

                await monitoringService.SaveDeviceAsync(deviceKey, name, address, description, cancellationToken);
                await securityLogService.WriteCurrentUserEventAsync("watch_device_save", "Устройство мониторинга сохранено.", deviceKey, cancellationToken);

                return CommandResult.Ok("Устройство сохранено: " + deviceKey);
            }

            if (args[1].Equals("list", StringComparison.OrdinalIgnoreCase))
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

            if (args[1].Equals("del", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    return CommandResult.Fail("Использование: watch del <deviceKey>");
                }

                string deviceKey = args[2];
                bool deleted = await monitoringService.DeleteDeviceAsync(deviceKey, cancellationToken);
                await securityLogService.WriteCurrentUserEventAsync(
                    deleted ? "watch_device_delete" : "watch_device_delete_failed",
                    deleted ? "Устройство мониторинга удалено." : "Устройство мониторинга не найдено.",
                    deviceKey,
                    cancellationToken);

                return deleted
                    ? CommandResult.Ok("Устройство удалено: " + deviceKey)
                    : CommandResult.Fail("Устройство не найдено: " + deviceKey);
            }

            return CommandResult.Fail(GetUsage());
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
                return CommandResult.Fail("Использование: watch show <deviceKey> [count]");
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
                "watch show <deviceKey> [count]\n" +
                "watch add <deviceKey> <name> [address] [description]\n" +
                "watch del <deviceKey>";
        }

        #endregion
    }
}
