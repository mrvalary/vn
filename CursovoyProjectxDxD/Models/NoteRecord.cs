using System;

namespace CursovoyProjectxDxD.Models
{
    // Модель одной заметки, считанной из PostgreSQL.
    public sealed class NoteRecord
    {
        // Идентификатор заметки в таблице.
        public int Id { get; set; }

        // Идентификатор автора заметки в таблице users.
        public int UserId { get; set; }

        // Текст заметки.
        public string Text { get; set; }

        // Логин автора заметки.
        public string AuthorLogin { get; set; }

        // Дата и время создания заметки.
        public DateTime CreatedAt { get; set; }
    }
}
