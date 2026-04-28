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
    /// Команда просмотра журнала безопасности.
    /// </summary>
    public sealed class SecurityLogsCommand : ICommand
    {
        /// <summary>
        /// Каноническое имя команды.
        /// </summary>
        public string Name => "sec logs";

        /// <summary>
        /// Описание команды для help.
        /// </summary>
        public string Description => "Просмотр логов безопасности: sec logs [количество]";

        /// <summary>
        /// Показывает последние события безопасности.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сессию для проверки роли.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();

            // Команда доступна только админу.
            if (!sessionService.CanViewSecurityLogs())
            {
                return CommandResult.Fail("Логи безопасности доступны только админу.");
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
                builder.AppendLine("#" + log.Id + " | " + log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " | " + FormatEventType(log.EventType));
                builder.AppendLine("Пользователь: " + (string.IsNullOrWhiteSpace(log.ActorLogin) ? "-" : log.ActorLogin));
                builder.AppendLine("Цель: " + (string.IsNullOrWhiteSpace(log.Target) ? "-" : log.Target));
                builder.AppendLine("Описание: " + log.Message);
                builder.AppendLine();
            }

            // Возвращаем готовый текст.
            return CommandResult.Ok(builder.ToString().TrimEnd());
        }

        /// <summary>
        /// Переводит технический код события в понятный русский текст для консоли.
        /// </summary>
        /// <param name="eventType">Код события из базы данных.</param>
        /// <returns>Русское название события.</returns>
        private static string FormatEventType(string eventType)
        {
            switch (eventType)
            {
                case "login_success":
                    return "успешный вход";
                case "login_failed":
                    return "ошибка входа";
                case "register_success":
                    return "успешная регистрация";
                case "register_failed":
                    return "ошибка регистрации";
                case "logout":
                    return "выход из системы";
                case "admin_user_create":
                    return "создание пользователя";
                case "admin_user_create_failed":
                    return "ошибка создания пользователя";
                case "admin_user_delete":
                    return "удаление пользователя";
                case "admin_user_delete_failed":
                    return "ошибка удаления пользователя";
                case "admin_user_block":
                    return "блокировка пользователя";
                case "admin_user_block_failed":
                    return "ошибка блокировки пользователя";
                case "admin_user_unblock":
                    return "разблокировка пользователя";
                case "admin_user_unblock_failed":
                    return "ошибка разблокировки пользователя";
                case "admin_user_info":
                    return "просмотр пользователя";
                case "admin_user_info_failed":
                    return "ошибка просмотра пользователя";
                case "admin_user_list":
                    return "просмотр списка пользователей";
                case "admin_user_notes":
                    return "просмотр заметок пользователя";
                case "admin_note_view":
                    return "просмотр заметки";
                case "admin_note_view_failed":
                    return "ошибка просмотра заметки";
                case "admin_note_edit":
                    return "редактирование заметки";
                case "admin_note_edit_failed":
                    return "ошибка редактирования заметки";
                case "watch_device_save":
                    return "сохранение устройства мониторинга";
                case "watch_device_delete":
                    return "удаление устройства мониторинга";
                case "watch_device_delete_failed":
                    return "ошибка удаления устройства мониторинга";
                default:
                    return "неизвестное событие";
            }
        }
    }
}
