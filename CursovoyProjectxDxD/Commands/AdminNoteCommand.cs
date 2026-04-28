using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Группа админских команд просмотра и редактирования любых заметок.
    /// </summary>
    public sealed class AdminNoteCommand : ICommand
    {
        /// <summary>
        /// Каноническое имя команды.
        /// </summary>
        public string Name => "admin nt";

        /// <summary>
        /// Описание команды для help.
        /// </summary>
        public string Description => "Админ: просмотр и редактирование любой заметки";

        /// <summary>
        /// Выполняет выбранное действие над заметкой.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем текущую сессию.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();

            // Команда доступна только администратору.
            if (!sessionService.IsAdmin())
            {
                return CommandResult.Fail("Команда доступна только администратору.");
            }

            // После admin nt должно быть действие.
            if (context.Args == null || context.Args.Length < 3)
            {
                return CommandResult.Fail(GetUsage());
            }

            // Разбираем действие.
            string action = context.Args[2].ToLowerInvariant();

            // Поддерживаем полное слово view и короткий вариант v.
            if (action == "view")
                return await ViewNoteAsync(context, cancellationToken);

            // Редактирование заметки.
            if (action == "edit")
                return await EditNoteAsync(context, cancellationToken);

            // Неизвестное действие показывает подсказку.
            return CommandResult.Fail(GetUsage());
        }

        /// <summary>
        /// Показывает любую заметку по id.
        /// </summary>
        private static async Task<CommandResult> ViewNoteAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length != 4)
            {
                return CommandResult.Fail("Использование: admin nt view <id>");
            }

            // Парсим id.
            int noteId;
            if (!int.TryParse(context.Args[3], out noteId))
            {
                return CommandResult.Fail("Идентификатор заметки должен быть числом.");
            }

            // Получаем заметку.
            NoteService noteService = context.GetRequiredService<NoteService>();
            NoteRecord note = await noteService.GetNoteByIdForAdminAsync(noteId, cancellationToken);

            // Логируем просмотр заметки.
            await WriteAdminLogAsync(
                context,
                note == null ? "admin_note_view_failed" : "admin_note_view",
                note == null ? "Заметка не найдена." : "Просмотрена заметка.",
                noteId.ToString(),
                cancellationToken);

            // Если заметки нет, возвращаем ошибку.
            if (note == null)
            {
                return CommandResult.Fail("Заметка не найдена.");
            }

            // Возвращаем карточку заметки.
            return CommandResult.Ok(FormatNote(note));
        }

        /// <summary>
        /// Редактирует любую заметку по id.
        /// </summary>
        private static async Task<CommandResult> EditNoteAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length < 5)
            {
                return CommandResult.Fail("Использование: admin nt edit <id> <новый текст>");
            }

            // Парсим id.
            int noteId;
            if (!int.TryParse(context.Args[3], out noteId))
            {
                return CommandResult.Fail("Идентификатор заметки должен быть числом.");
            }

            // Собираем новый текст заметки.
            string text = string.Join(" ", context.Args, 4, context.Args.Length - 4);

            // Обновляем заметку.
            NoteService noteService = context.GetRequiredService<NoteService>();
            bool updated = await noteService.UpdateAnyNoteForAdminAsync(noteId, text, cancellationToken);

            // Логируем попытку редактирования.
            await WriteAdminLogAsync(
                context,
                updated ? "admin_note_edit" : "admin_note_edit_failed",
                updated ? "Заметка обновлена администратором." : "Заметка не найдена.",
                noteId.ToString(),
                cancellationToken);

            // Если обновить нечего, заметка не найдена.
            if (!updated)
            {
                return CommandResult.Fail("Заметка не найдена.");
            }

            // Возвращаем успешный результат.
            return CommandResult.Ok("Заметка #" + noteId + " обновлена администратором.");
        }

        /// <summary>
        /// Форматирует одну заметку.
        /// </summary>
        private static string FormatNote(NoteRecord note)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Заметка #" + note.Id);
            builder.AppendLine("Автор: " + note.AuthorLogin + " (id пользователя: " + note.UserId + ")");
            builder.AppendLine("Создана: " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine("Текст:");
            builder.AppendLine(note.Text);
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Возвращает справку по группе admin nt.
        /// </summary>
        private static string GetUsage()
        {
            return "Использование:\n" +
                   "admin nt view <id>\n" +
                   "admin nt edit <id> <новый текст>";
        }

        /// <summary>
        /// Записывает действие администратора в журнал безопасности.
        /// </summary>
        private static async Task WriteAdminLogAsync(CommandContext context, string eventType, string message, string target, CancellationToken cancellationToken)
        {
            SecurityLogService securityLogService = context.GetRequiredService<SecurityLogService>();
            await securityLogService.WriteCurrentUserEventAsync(eventType, message, target, cancellationToken);
        }
    }
}
