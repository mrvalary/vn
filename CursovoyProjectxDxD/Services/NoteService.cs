using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Сервис работы с заметками через PostgreSQL.
    /// </summary>
    public sealed class NoteService
    {
        // SQL добавления заметки текущего пользователя.
        private const string InsertNoteSql =
            "INSERT INTO notes (user_id, note_text) VALUES (@userId, @text) RETURNING id, created_at;";

        // SQL удаления заметки только у текущего пользователя.
        private const string DeleteOwnNoteSql =
            "DELETE FROM notes WHERE id = @id AND user_id = @userId;";

        // SQL получения всех заметок текущего пользователя.
        private const string ListOwnNotesSql =
            "SELECT n.id, n.user_id, n.note_text, u.login, n.created_at " +
            "FROM notes n " +
            "JOIN users u ON u.id = n.user_id " +
            "WHERE n.user_id = @userId " +
            "ORDER BY n.created_at DESC, n.id DESC;";

        // SQL поиска заметок текущего пользователя по тексту.
        private const string SearchOwnNotesSql =
            "SELECT n.id, n.user_id, n.note_text, u.login, n.created_at " +
            "FROM notes n " +
            "JOIN users u ON u.id = n.user_id " +
            "WHERE n.user_id = @userId AND LOWER(n.note_text) LIKE @pattern " +
            "ORDER BY n.created_at DESC, n.id DESC;";

        // SQL редактирования своей заметки.
        private const string UpdateOwnNoteSql =
            "UPDATE notes SET note_text = @text WHERE id = @id AND user_id = @userId;";

        // SQL просмотра заметок пользователя по логину для администратора.
        private const string ListNotesByLoginSql =
            "SELECT n.id, n.user_id, n.note_text, u.login, n.created_at " +
            "FROM notes n " +
            "JOIN users u ON u.id = n.user_id " +
            "WHERE u.login = @login " +
            "ORDER BY n.created_at DESC, n.id DESC;";

        // SQL просмотра одной заметки по id для администратора.
        private const string GetNoteByIdSql =
            "SELECT n.id, n.user_id, n.note_text, u.login, n.created_at " +
            "FROM notes n " +
            "JOIN users u ON u.id = n.user_id " +
            "WHERE n.id = @id;";

        // SQL редактирования любой заметки администратором.
        private const string UpdateAnyNoteSql =
            "UPDATE notes SET note_text = @text WHERE id = @id;";

        // Фабрика открывает соединения с PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Сервис текущей сессии нужен для user_id и проверки роли.
        private readonly AuthSessionService _sessionService;

        /// <summary>
        /// Получаем зависимости через DI.
        /// </summary>
        public NoteService(DatabaseConnectionFactory connectionFactory, AuthSessionService sessionService)
        {
            // Сохраняем фабрику соединений.
            _connectionFactory = connectionFactory;
            // Сохраняем текущую сессию.
            _sessionService = sessionService;
        }

        /// <summary>
        /// Добавляет новую заметку текущего пользователя.
        /// </summary>
        public async Task<NoteRecord> AddNoteAsync(string text, CancellationToken cancellationToken)
        {
            // Пользователь должен быть авторизован.
            EnsureAuthenticated();
            // Текст заметки должен быть непустым.
            EnsureValidText(text);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаём команду вставки заметки.
                using (NpgsqlCommand command = new NpgsqlCommand(InsertNoteSql, connection))
                {
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);
                    command.Parameters.AddWithValue("text", text.Trim());

                    // Читаем id и дату, которые вернула база.
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

            // Если RETURNING ничего не вернул, считаем сохранение неуспешным.
            throw new InvalidOperationException("Не удалось сохранить заметку в базе данных.");
        }

        /// <summary>
        /// Удаляет заметку текущего пользователя по id.
        /// </summary>
        public async Task<bool> DeleteNoteAsync(int noteId, CancellationToken cancellationToken)
        {
            // Пользователь должен быть авторизован.
            EnsureAuthenticated();
            // Id заметки должен быть положительным.
            EnsureValidNoteId(noteId);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Удаляем только заметку текущего пользователя.
                using (NpgsqlCommand command = new NpgsqlCommand(DeleteOwnNoteSql, connection))
                {
                    command.Parameters.AddWithValue("id", noteId);
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);

                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        /// <summary>
        /// Возвращает все заметки текущего пользователя.
        /// </summary>
        public async Task<IReadOnlyList<NoteRecord>> ListNotesAsync(CancellationToken cancellationToken)
        {
            // Пользователь должен быть авторизован.
            EnsureAuthenticated();

            // Читаем заметки текущего пользователя.
            return await ReadNotesWithCurrentUserAsync(ListOwnNotesSql, null, cancellationToken);
        }

        /// <summary>
        /// Ищет заметки текущего пользователя по фрагменту текста.
        /// </summary>
        public async Task<IReadOnlyList<NoteRecord>> SearchNotesAsync(string query, CancellationToken cancellationToken)
        {
            // Пользователь должен быть авторизован.
            EnsureAuthenticated();

            // Поисковый запрос должен быть непустым.
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new InvalidOperationException("Поисковый запрос не может быть пустым.");
            }

            // LOWER + LIKE даёт поиск без учёта регистра.
            string pattern = "%" + query.Trim().ToLowerInvariant() + "%";

            // Читаем найденные заметки текущего пользователя.
            return await ReadNotesWithCurrentUserAsync(SearchOwnNotesSql, pattern, cancellationToken);
        }

        /// <summary>
        /// Редактирует заметку текущего пользователя.
        /// </summary>
        public async Task<bool> UpdateOwnNoteAsync(int noteId, string text, CancellationToken cancellationToken)
        {
            // Пользователь должен быть авторизован.
            EnsureAuthenticated();
            // Id заметки должен быть положительным.
            EnsureValidNoteId(noteId);
            // Новый текст должен быть непустым.
            EnsureValidText(text);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Обновляем только заметку текущего пользователя.
                using (NpgsqlCommand command = new NpgsqlCommand(UpdateOwnNoteSql, connection))
                {
                    command.Parameters.AddWithValue("id", noteId);
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);
                    command.Parameters.AddWithValue("text", text.Trim());

                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        /// <summary>
        /// Возвращает заметки указанного пользователя для администратора.
        /// </summary>
        public async Task<IReadOnlyList<NoteRecord>> ListNotesByLoginForAdminAsync(string login, CancellationToken cancellationToken)
        {
            // Метод доступен только администратору.
            EnsureAdmin();

            // Логин пользователя должен быть непустым.
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new InvalidOperationException("Логин пользователя не может быть пустым.");
            }

            // Список результатов.
            List<NoteRecord> notes = new List<NoteRecord>();

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Читаем заметки по логину автора.
                using (NpgsqlCommand command = new NpgsqlCommand(ListNotesByLoginSql, connection))
                {
                    command.Parameters.AddWithValue("login", login.Trim());

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            notes.Add(ReadNoteRecord(reader));
                        }
                    }
                }
            }

            return notes;
        }

        /// <summary>
        /// Возвращает любую заметку по id для администратора.
        /// </summary>
        public async Task<NoteRecord> GetNoteByIdForAdminAsync(int noteId, CancellationToken cancellationToken)
        {
            // Метод доступен только администратору.
            EnsureAdmin();
            // Id заметки должен быть положительным.
            EnsureValidNoteId(noteId);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Читаем одну заметку по id.
                using (NpgsqlCommand command = new NpgsqlCommand(GetNoteByIdSql, connection))
                {
                    command.Parameters.AddWithValue("id", noteId);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            return null;
                        }

                        return ReadNoteRecord(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Редактирует любую заметку от имени администратора.
        /// </summary>
        public async Task<bool> UpdateAnyNoteForAdminAsync(int noteId, string text, CancellationToken cancellationToken)
        {
            // Метод доступен только администратору.
            EnsureAdmin();
            // Id заметки должен быть положительным.
            EnsureValidNoteId(noteId);
            // Новый текст должен быть непустым.
            EnsureValidText(text);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Обновляем заметку без ограничения по автору.
                using (NpgsqlCommand command = new NpgsqlCommand(UpdateAnyNoteSql, connection))
                {
                    command.Parameters.AddWithValue("id", noteId);
                    command.Parameters.AddWithValue("text", text.Trim());

                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        /// <summary>
        /// Читает заметки текущего пользователя.
        /// </summary>
        private async Task<IReadOnlyList<NoteRecord>> ReadNotesWithCurrentUserAsync(string sql, string pattern, CancellationToken cancellationToken)
        {
            // Список результатов.
            List<NoteRecord> notes = new List<NoteRecord>();

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаём команду чтения.
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("userId", _sessionService.CurrentUserId.Value);

                    // pattern нужен только для поиска.
                    if (pattern != null)
                    {
                        command.Parameters.AddWithValue("pattern", pattern);
                    }

                    // Читаем строки результата.
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            notes.Add(ReadNoteRecord(reader));
                        }
                    }
                }
            }

            return notes;
        }

        /// <summary>
        /// Проверяет, что пользователь вошёл в систему.
        /// </summary>
        private void EnsureAuthenticated()
        {
            if (!_sessionService.CurrentUserId.HasValue)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }
        }

        /// <summary>
        /// Проверяет, что текущий пользователь является администратором.
        /// </summary>
        private void EnsureAdmin()
        {
            // Сначала должна быть активная сессия.
            EnsureAuthenticated();

            // Только роль admin может работать с чужими заметками.
            if (!_sessionService.IsAdmin())
            {
                throw new InvalidOperationException("Команда доступна только администратору.");
            }
        }

        /// <summary>
        /// Проверяет корректность id заметки.
        /// </summary>
        private static void EnsureValidNoteId(int noteId)
        {
            if (noteId <= 0)
            {
                throw new InvalidOperationException("Идентификатор заметки должен быть положительным числом.");
            }
        }

        /// <summary>
        /// Проверяет текст заметки.
        /// </summary>
        private static void EnsureValidText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Текст заметки не может быть пустым.");
            }
        }

        /// <summary>
        /// Преобразует строку NpgsqlDataReader в модель заметки.
        /// </summary>
        private static NoteRecord ReadNoteRecord(NpgsqlDataReader reader)
        {
            return new NoteRecord
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Text = reader.GetString(2),
                AuthorLogin = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }
    }
}
