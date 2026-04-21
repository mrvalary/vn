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
    // Команда stat управляет наблюдаемыми устройствами и их метриками нагрузки.
    public sealed class StatCommand : ICommand
    {
        // Имя команды в реестре. Все подкоманды разбираются внутри этого класса.
        public string Name => "stat";

        // Описание команды для help.
        public string Description => "Статистика нагрузки устройств: CPU, RAM, HDD";

        // Выполняет одну из подкоманд stat.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис мониторинга из DI.
            SystemMonitoringService monitoringService = context.GetRequiredService<SystemMonitoringService>();
            // Получаем сервис логов, чтобы фиксировать важные действия со статистикой.
            SecurityLogService securityLogService = context.GetRequiredService<SecurityLogService>();
            // Берем аргументы команды.
            string[] args = context.Args;

            // Без подкоманды показываем краткую справку.
            if (args.Length < 2)
            {
                return CommandResult.Fail(GetUsage());
            }

            // stat device ... управляет списком устройств.
            if (args[1].Equals("device", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteDeviceCommandAsync(args, monitoringService, securityLogService, cancellationToken);
            }

            // stat collect <device> снимает текущие показатели и сохраняет их в историю.
            if (args[1].Equals("collect", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteCollectCommandAsync(args, monitoringService, securityLogService, cancellationToken);
            }

            // stat show <device> [count] показывает последние снимки нагрузки.
            if (args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteShowCommandAsync(args, monitoringService, cancellationToken);
            }

            // Любая неизвестная подкоманда получает понятную подсказку.
            return CommandResult.Fail(GetUsage());
        }

        // Выполняет подкоманды stat device add/list/del.
        private static async Task<CommandResult> ExecuteDeviceCommandAsync(
            string[] args,
            SystemMonitoringService monitoringService,
            SecurityLogService securityLogService,
            CancellationToken cancellationToken)
        {
            // Для stat device нужна еще одна часть: add, list или del.
            if (args.Length < 3)
            {
                return CommandResult.Fail(GetUsage());
            }

            // stat device add <name> [address] добавляет устройство.
            if (args[2].Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    return CommandResult.Fail("Использование: stat device add <name> [address]");
                }

                string name = args[3];
                string address = args.Length >= 5 ? args[4] : null;
                string description = args.Length >= 6 ? string.Join(" ", args, 5, args.Length - 5) : null;

                await monitoringService.AddDeviceAsync(name, address, description, cancellationToken);
                await securityLogService.WriteCurrentUserEventAsync("stat_device_add", "Добавлено устройство для мониторинга.", name, cancellationToken);

                return CommandResult.Ok("Устройство добавлено: " + name);
            }

            // stat device list показывает список наблюдаемых устройств.
            if (args[2].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyList<MonitoredDevice> devices = await monitoringService.ListDevicesAsync(cancellationToken);

                if (devices.Count == 0)
                {
                    return CommandResult.Ok("Устройства для мониторинга пока не добавлены.");
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Устройства для мониторинга:");

                foreach (MonitoredDevice device in devices)
                {
                    builder.Append("- ");
                    builder.Append(device.Name);

                    if (!string.IsNullOrWhiteSpace(device.Address))
                    {
                        builder.Append(" (");
                        builder.Append(device.Address);
                        builder.Append(")");
                    }

                    if (!string.IsNullOrWhiteSpace(device.Description))
                    {
                        builder.Append(" - ");
                        builder.Append(device.Description);
                    }

                    builder.AppendLine();
                }

                return CommandResult.Ok(builder.ToString());
            }

            // stat device del <name> удаляет устройство.
            if (args[2].Equals("del", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    return CommandResult.Fail("Использование: stat device del <name>");
                }

                string name = args[3];
                bool deleted = await monitoringService.DeleteDeviceAsync(name, cancellationToken);

                if (!deleted)
                {
                    return CommandResult.Fail("Устройство не найдено: " + name);
                }

                await securityLogService.WriteCurrentUserEventAsync("stat_device_del", "Удалено устройство мониторинга.", name, cancellationToken);
                return CommandResult.Ok("Устройство удалено: " + name);
            }

            // Если третья часть неизвестна, показываем справку.
            return CommandResult.Fail(GetUsage());
        }

        // Выполняет сбор локальных показателей CPU/RAM/HDD.
        private static async Task<CommandResult> ExecuteCollectCommandAsync(
            string[] args,
            SystemMonitoringService monitoringService,
            SecurityLogService securityLogService,
            CancellationToken cancellationToken)
        {
            // Имя устройства обязательно, потому что в БД может быть несколько интересующих устройств.
            if (args.Length < 3)
            {
                return CommandResult.Fail("Использование: stat collect <device>");
            }

            // Берем имя устройства из команды.
            string deviceName = args[2];
            // Снимаем показатели с текущего ПК и сохраняем их в БД.
            SystemMetricRecord metric = await monitoringService.CollectLocalMetricsAsync(deviceName, cancellationToken);
            // Пишем действие в журнал безопасности.
            await securityLogService.WriteCurrentUserEventAsync("stat_collect", "Снят снимок нагрузки устройства.", deviceName, cancellationToken);

            // Формируем понятный вывод для консоли.
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Снимок нагрузки сохранен.");
            builder.AppendLine("Устройство: " + metric.DeviceName);
            builder.AppendLine("CPU: " + FormatPercent(metric.CpuPercent));
            builder.AppendLine("RAM: " + FormatPercent(metric.RamPercent));
            builder.AppendLine("HDD: " + FormatPercent(metric.HddPercent));
            builder.AppendLine("Время: " + metric.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            return CommandResult.Ok(builder.ToString());
        }

        // Выполняет просмотр истории нагрузки.
        private static async Task<CommandResult> ExecuteShowCommandAsync(
            string[] args,
            SystemMonitoringService monitoringService,
            CancellationToken cancellationToken)
        {
            // Имя устройства обязательно.
            if (args.Length < 3)
            {
                return CommandResult.Fail("Использование: stat show <device> [count]");
            }

            // Первый аргумент после show - имя устройства.
            string deviceName = args[2];
            // Второй необязательный аргумент - сколько последних записей показать.
            int limit = args.Length >= 4 ? ParseLimit(args[3]) : 10;
            // Получаем историю из БД.
            IReadOnlyList<SystemMetricRecord> metrics = await monitoringService.ListMetricsAsync(deviceName, limit, cancellationToken);

            if (metrics.Count == 0)
            {
                return CommandResult.Ok("Для устройства '" + deviceName + "' пока нет сохраненных снимков нагрузки.");
            }

            // Формируем многострочный ответ.
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("История нагрузки устройства: " + deviceName);

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

            return CommandResult.Ok(builder.ToString());
        }

        // Парсит лимит истории без падения на неправильном вводе.
        private static int ParseLimit(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 10;
        }

        // Форматирует процент для вывода в консоль.
        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.00") + "%";
        }

        // Общая справка по команде stat.
        private static string GetUsage()
        {
            return
                "Команды статистики:\n" +
                "stat device add <name> [address] [description] - добавить устройство\n" +
                "stat device list - показать устройства\n" +
                "stat device del <name> - удалить устройство\n" +
                "stat collect <device> - снять CPU/RAM/HDD с текущего ПК и сохранить\n" +
                "stat show <device> [count] - показать историю нагрузки";
        }
    }
}
