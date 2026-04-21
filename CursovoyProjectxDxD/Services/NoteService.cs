using System;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    // Сервис работы с заметками напрямую через PostgreSQL.
    public sealed class NoteService
    {
        // SQL добавления заметки.
        private const string InsertNoteSql =
            "INSERT INTO notes (user_id, note_text) VALUES (@userId, @text) RETURNING id, created_at;";

        // SQL удаления заметки только у текущего пользователя.
        private const string DeleteNoteSql =
            "DELETE FROM notes WHERE id = @id AND user_id = @userId;";

        // Фабрика соединений с PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Текущая пользовательская сессия.
        private readonly AuthSessionService _sessionService;

        // Конструктор получает зависимости через DI.
        public NoteService(DatabaseConnectionFactory connectionFactory, AuthSessionService sessionService)
        {
            // Сохраняем фабрику соединений.
            _connectionFactory = connectionFactory;
            // Сохраняем текущую сессию.
            _sessionService = sessionService;
        }

        // Добавляет новую заметку текущего пользователя.
        public async Task<NoteRecord> AddNoteAsync(string text, CancellationToken cancellationToken)
        {
            // Текст заметки не должен быть пустым.
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Текст заметки не может быть пустым.");
            }

            // Без авторизованного пользователя нельзя добавить заметку.
            if (!_sessionService.CurrentUserId.HasValue)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Готовим команду вставки новой заметки.
                using (NpgsqlCommand command = new NpgsqlCommand(InsertNoteSql, connection))
                {
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);
                    command.Parameters.AddWithValue("text", text.Trim());

                    // Читаем id и дату создания, которые вернула БД.
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return new NoteRecord
                            {
                                Id = reader.GetInt32(0),
                                UserId = _sessionService.CurrentUserId.Value,
                                Text = text.Trim(),
                                AuthorLogin = _sessionService.CurrentLogin,
                                CreatedAt = reader.GetDateTime(1)
                            };
                        }
                    }
                }
            }

            // Если БД не вернула созданную запись, считаем это ошибкой.
            throw new InvalidOperationException("Не удалось сохранить заметку в базе данных.");
        }

        // Удаляет заметку текущего пользователя по id.
        public async Task<bool> DeleteNoteAsync(int noteId, CancellationToken cancellationToken)
        {
            // Id заметки должен быть положительным.
            if (noteId <= 0)
            {
                throw new InvalidOperationException("Идентификатор заметки должен быть положительным числом.");
            }

            // Без авторизованного пользователя нельзя удалить заметку.
            if (!_sessionService.CurrentUserId.HasValue)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Готовим команду удаления.
                using (NpgsqlCommand command = new NpgsqlCommand(DeleteNoteSql, connection))
                {
                    command.Parameters.AddWithValue("id", noteId);
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);

                    // Если строка удалена, значит заметка существовала и принадлежала текущему пользователю.
                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }
    }
}
