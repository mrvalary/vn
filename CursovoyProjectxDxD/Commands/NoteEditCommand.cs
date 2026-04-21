using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда редактирования своей заметки.
    public sealed class NoteEditCommand : ICommand
    {
        // Имя команды в интерактивной консоли.
        public string Name => "nt edit";

        // Описание команды для справки.
        public string Description => "Редактирование своей заметки: nt edit <id> <новый текст>";

        // Выполняет редактирование заметки текущего пользователя.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис заметок из DI.
            NoteService noteService = context.GetRequiredService<NoteService>();

            // После nt edit должны идти id и новый текст.
            if (context.Args == null || context.Args.Length < 4)
            {
                return CommandResult.Fail("Использование: nt edit <id> <новый текст>");
            }

            // Преобразуем id заметки в число.
            int noteId;
            if (!int.TryParse(context.Args[2], out noteId))
            {
                return CommandResult.Fail("Идентификатор заметки должен быть числом.");
            }

            // Собираем новый текст из всех аргументов после id.
            string text = string.Join(" ", context.Args, 3, context.Args.Length - 3);

            // Обновляем только свою заметку.
            bool updated = await noteService.UpdateOwnNoteAsync(noteId, text, cancellationToken);
            if (!updated)
            {
                return CommandResult.Fail("Заметка не найдена или не принадлежит текущему пользователю.");
            }

            // Возвращаем успешный результат.
            return CommandResult.Ok("Заметка #" + noteId + " обновлена.");
        }
    }
}
