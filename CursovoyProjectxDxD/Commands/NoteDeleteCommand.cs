using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда удаления заметки по её идентификатору.
    /// </summary>
    public sealed class NoteDeleteCommand : ICommand
    {
        /// <summary>
        /// Имя команды в CLI.
        /// </summary>
        public string Name => "nt del";

        /// <summary>
        /// Краткое описание команды для help.
        /// </summary>
        public string Description => "Удаление заметки: nt del <id>";

        /// <summary>
        /// Выполняет удаление заметки из PostgreSQL.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис заметок.
            NoteService noteService = context.GetRequiredService<NoteService>();

            // После nt del должен идти id заметки.
            if (context.Args == null || context.Args.Length != 3)
            {
                return CommandResult.Fail("Использование: nt del <id>");
            }

            // Пробуем распарсить id в число.
            int noteId;
            if (!int.TryParse(context.Args[2], out noteId))
            {
                return CommandResult.Fail("Идентификатор заметки должен быть числом.");
            }

            // Удаляем заметку только у текущего пользователя.
            bool isDeleted = await noteService.DeleteNoteAsync(noteId, cancellationToken);

            // Если строка не удалена, заметки нет или она принадлежит другому пользователю.
            if (!isDeleted)
            {
                return CommandResult.Fail("Заметка не найдена или у вас нет прав на её удаление.");
            }

            // Иначе отдаём успешное сообщение пользователю.
            return CommandResult.Ok("Заметка с id " + noteId + " удалена.");
        }
    }
}
