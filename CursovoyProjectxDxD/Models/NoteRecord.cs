using System;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Модель одной заметки, считанной из PostgreSQL.
    /// </summary>
    public sealed class NoteRecord
    {
        /// <summary>
        /// Идентификатор заметки в таблице.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор автора заметки в таблице users.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Текст заметки.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Логин автора заметки.
        /// </summary>
        public string AuthorLogin { get; set; }

        /// <summary>
        /// Дата и время создания заметки.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
