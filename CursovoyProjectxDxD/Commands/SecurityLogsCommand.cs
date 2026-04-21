using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда просмотра журнала безопасности.
    public sealed class SecurityLogsCommand : ICommand
    {
        // Каноническое имя команды.
        public string Name => "sec logs";

        // Описание команды для help.
        public string Description => "Просмотр логов безопасности: sec logs [количество]";

        // Показывает последние события безопасности.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сессию для проверки роли.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();

            // Команда доступна только админу и статисту.
            if (!sessionService.CanViewSecurityLogs())
            {
                return CommandResult.Fail("Логи безопасности доступны только админу и статисту.");
            }

            // По умолчанию показываем последние 20 записей.
            int limit = 20;

            // Пользователь может указать количество строк.
            if (context.Args != null && context.Args.Length >= 3)
            {
                if (!int.TryParse(context.Args[2], out limit))
                {
                    return CommandResult.Fail("Количество записей должно быть числом.");
                }
            }

            // Получаем сервис журнала.
            SecurityLogService logService = context.GetRequiredService<SecurityLogService>();

            // Читаем последние события.
            IReadOnlyList<SecurityLogRecord> logs = await logService.ListLogsAsync(limit, cancellationToken);
            if (logs.Count == 0)
            {
                return CommandResult.Ok("Журнал безопасности пуст.");
            }

            // Формируем многострочный вывод.
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Логи безопасности:");
            builder.AppendLine();

            foreach (SecurityLogRecord log in logs)
            {
                builder.AppendLine("#" + log.Id + " | " + log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " | " + log.EventType);
                builder.AppendLine("Пользователь: " + (string.IsNullOrWhiteSpace(log.ActorLogin) ? "-" : log.ActorLogin));
                builder.AppendLine("Цель: " + (string.IsNullOrWhiteSpace(log.Target) ? "-" : log.Target));
                builder.AppendLine("Описание: " + log.Message);
                builder.AppendLine();
            }

            // Возвращаем готовый текст.
            return CommandResult.Ok(builder.ToString().TrimEnd());
        }
    }
}
