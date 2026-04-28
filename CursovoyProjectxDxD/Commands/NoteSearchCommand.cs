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
    /// Команда поиска заметок текущего пользователя по тексту.
    /// </summary>
    public sealed class NoteSearchCommand : ICommand
    {
        /// <summary>
        /// Имя команды, которое вводится в CLI.
        /// </summary>
        public string Name => "nt search";

        /// <summary>
        /// Описание команды для help.
        /// </summary>
        public string Description => "Поиск заметок: nt search <текст>";

        /// <summary>
        /// Выполняет поиск заметок по фрагменту текста.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис заметок из DI-контейнера.
            NoteService noteService = context.GetRequiredService<NoteService>();

            // После nt search должен идти текст, который пользователь хочет найти.
            if (context.Args == null || context.Args.Length < 3)
            {
                return CommandResult.Fail("Использование: nt search <текст>");
            }

            // Собираем поисковый запрос из всех слов после nt search.
            string query = string.Join(" ", context.Args, 2, context.Args.Length - 2);

            // Ищем только в заметках текущего пользователя.
            IReadOnlyList<NoteRecord> notes = await noteService.SearchNotesAsync(query, cancellationToken);

            // Если совпадений нет, сообщаем об этом без ошибки.
            if (notes.Count == 0)
            {
                return CommandResult.Ok("По запросу \"" + query + "\" заметки не найдены.");
            }

            // Формируем многострочный результат поиска.
            StringBuilder builder = new StringBuilder();

            // Заголовок показывает, по какому тексту был поиск.
            builder.AppendLine("Найденные заметки по запросу \"" + query + "\":");
            builder.AppendLine();

            // Выводим найденные заметки в том же формате, что и nt list.
            foreach (NoteRecord note in notes)
            {
                builder.AppendLine("#" + note.Id + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine(note.Text);
                builder.AppendLine();
            }

            // Возвращаем текст результата в общий обработчик команд.
            return CommandResult.Ok(builder.ToString().TrimEnd());
        }
    }
}
