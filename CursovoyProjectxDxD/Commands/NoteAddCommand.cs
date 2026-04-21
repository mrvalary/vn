using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда добавления новой заметки через серверный API.
    public sealed class NoteAddCommand : ICommand
    {
        // Имя команды в CLI.
        public string Name => "nt add";

        // Краткое описание для справки.
        public string Description => "Добавление заметки: nt add \"текст заметки\"";

        // Выполняет добавление заметки.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис заметок.
            NoteService noteService = context.GetRequiredService<NoteService>();

            // После nt add должен идти хотя бы один аргумент с текстом заметки.
            if (context.Args == null || context.Args.Length < 3)
            {
                return CommandResult.Fail("Использование: nt add \"текст заметки\"");
            }

            // Собираем текст заметки из всех аргументов после nt add.
            string noteText = string.Join(" ", context.Args, 2, context.Args.Length - 2);

            // Отправляем заметку на сервер и получаем созданную запись.
            NoteRecord note = await noteService.AddNoteAsync(noteText, cancellationToken);

            // Возвращаем итоговое сообщение пользователю.
            return CommandResult.Ok(
                "Заметка добавлена. Id: " + note.Id +
                ", автор: " + note.AuthorLogin +
                ", создана: " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
