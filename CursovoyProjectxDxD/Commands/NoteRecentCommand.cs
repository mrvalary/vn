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
    /// Команда быстрого просмотра последних заметок текущего пользователя.
    /// </summary>
    public sealed class NoteRecentCommand : ICommand
    {
        #region Constants

        // По умолчанию показываем короткую историю, чтобы команда не перегружала консоль.
        private const int DefaultLimit = 5;

        // Ограничение защищает консоль от слишком большого вывода при случайно введенном числе.
        private const int MaxLimit = 50;

        #endregion

        #region Metadata

        /// <summary>
        /// Имя команды, которое вводится в интерактивной консоли.
        /// </summary>
        public string Name => "nt recent";

        /// <summary>
        /// Краткое описание команды для карты команд.
        /// </summary>
        public string Description => "Последние заметки: nt recent [количество]";

        #endregion

        #region Execute

        /// <summary>
        /// Получает заметки текущего пользователя и выводит только последние записи.
        /// </summary>
        /// <param name="context">Контекст выполнения команды.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Список последних заметок или сообщение об ошибке синтаксиса.</returns>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context.Args == null || context.Args.Length < 2 || context.Args.Length > 3)
            {
                return CommandResult.Fail("Использование: nt recent [количество]");
            }

            int limit;
            if (!TryParseLimit(context.Args, out limit))
            {
                return CommandResult.Fail("Количество заметок должно быть положительным числом.");
            }

            NoteService noteService = context.GetRequiredService<NoteService>();
            IReadOnlyList<NoteRecord> notes = await noteService.ListNotesAsync(cancellationToken);

            if (notes.Count == 0)
            {
                return CommandResult.Ok("У вас пока нет заметок.");
            }

            return CommandResult.Ok(FormatRecentNotes(notes, limit));
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Разбирает необязательное количество заметок из аргументов команды.
        /// </summary>
        /// <param name="args">Аргументы команды nt recent.</param>
        /// <param name="limit">Итоговое количество заметок для вывода.</param>
        /// <returns>true, если число отсутствует или указано корректно.</returns>
        private static bool TryParseLimit(string[] args, out int limit)
        {
            limit = DefaultLimit;

            if (args.Length == 2)
            {
                return true;
            }

            int parsedLimit;
            if (!int.TryParse(args[2], out parsedLimit) || parsedLimit <= 0)
            {
                return false;
            }

            limit = Math.Min(parsedLimit, MaxLimit);
            return true;
        }

        /// <summary>
        /// Формирует компактный вывод последних заметок для консоли.
        /// </summary>
        /// <param name="notes">Заметки пользователя, уже отсортированные от новых к старым.</param>
        /// <param name="limit">Максимальное количество строк для вывода.</param>
        /// <returns>Готовый многострочный текст со списком заметок.</returns>
        private static string FormatRecentNotes(IReadOnlyList<NoteRecord> notes, int limit)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Последние заметки:");
            builder.AppendLine();

            int count = Math.Min(notes.Count, limit);
            for (int index = 0; index < count; index++)
            {
                NoteRecord note = notes[index];
                builder.AppendLine("#" + note.Id + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine(note.Text);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        #endregion
    }
}
