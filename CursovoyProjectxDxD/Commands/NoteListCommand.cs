using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда вывода всех заметок текущего пользователя.
    public sealed class NoteListCommand : ICommand
    {
        // Имя команды, которое вводится в интерактивной консоли.
        public string Name => "nt list";

        // Описание автоматически попадает в команду help.
        public string Description => "Список ваших заметок: nt list";

        // Выполняет чтение заметок из PostgreSQL и готовит текстовый список для консоли.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // NoteService содержит всю работу с таблицей notes.
            NoteService noteService = context.GetRequiredService<NoteService>();

            // У nt list не должно быть дополнительных аргументов.
            if (context.Args == null || context.Args.Length != 2)
            {
                return CommandResult.Fail("Использование: nt list");
            }

            // Получаем только заметки текущего авторизованного пользователя.
            IReadOnlyList<NoteRecord> notes = await noteService.ListNotesAsync(cancellationToken);

            // Если записей нет, сразу возвращаем понятное сообщение.
            if (notes.Count == 0)
            {
                return CommandResult.Ok("У вас пока нет заметок.");
            }

            // StringBuilder удобен для формирования многострочного ответа.
            StringBuilder builder = new StringBuilder();

            // Заголовок помогает визуально отделить список от prompt консоли.
            builder.AppendLine("Ваши заметки:");
            builder.AppendLine();

            // Каждую заметку выводим с id, датой и текстом.
            foreach (NoteRecord note in notes)
            {
                builder.AppendLine("#" + note.Id + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine(note.Text);
                builder.AppendLine();
            }

            // Отдаём готовый текст в общий механизм печати результата команды.
            return CommandResult.Ok(builder.ToString().TrimEnd());
        }
    }
}
