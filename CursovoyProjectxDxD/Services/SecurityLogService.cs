using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Сервис записи и просмотра журнала безопасности.
    /// </summary>
    public sealed class SecurityLogService
    {
        // SQL добавления события безопасности.
        private const string InsertLogSql =
            "INSERT INTO security_logs (actor_user_id, actor_login, event_type, message, target) " +
            "VALUES (@actorUserId, @actorLogin, @eventType, @message, @target);";

        // SQL просмотра последних событий безопасности.
        private const string ListLogsSql =
            "SELECT id, actor_user_id, actor_login, event_type, message, target, created_at " +
            "FROM security_logs " +
            "ORDER BY created_at DESC, id DESC " +
            "LIMIT @limit;";

        // Фабрика открывает подключения к PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Текущая сессия нужна для проверки прав просмотра логов.
        private readonly AuthSessionService _sessionService;

        /// <summary>
        /// Получаем зависимости через DI.
        /// </summary>
        public SecurityLogService(DatabaseConnectionFactory connectionFactory, AuthSessionService sessionService)
        {
            // Сохраняем фабрику подключений.
            _connectionFactory = connectionFactory;
            // Сохраняем текущую сессию.
            _sessionService = sessionService;
        }

        /// <summary>
        /// Записывает событие от текущего авторизованного пользователя.
        /// </summary>
        public async Task WriteCurrentUserEventAsync(string eventType, string message, string target, CancellationToken cancellationToken)
        {
            // Берём данные текущего пользователя из сессии.
            int? actorUserId = _sessionService.CurrentUserId;
            string actorLogin = _sessionService.CurrentLogin;

            // Пишем событие в общий метод.
            await WriteEventAsync(actorUserId, actorLogin, eventType, message, target, cancellationToken);
        }

        /// <summary>
        /// Записывает событие, когда пользователь ещё не авторизован или вход не удался.
        /// </summary>
        public async Task WriteAnonymousEventAsync(string actorLogin, string eventType, string message, string target, CancellationToken cancellationToken)
        {
            // Для анонимного события id пользователя неизвестен.
            await WriteEventAsync(null, actorLogin, eventType, message, target, cancellationToken);
        }

        /// <summary>
        /// Возвращает последние события безопасности.
        /// </summary>
        public async Task<IReadOnlyList<SecurityLogRecord>> ListLogsAsync(int limit, CancellationToken cancellationToken)
        {
            // Смотреть журнал могут только админ и статист.
            EnsureCanViewSecurityLogs();

            // Ограничиваем размер выдачи, чтобы случайно не вывести слишком много строк в консоль.
            if (limit <= 0)
            {
                limit = 20;
            }

            // Верхний предел защищает консоль от огромного вывода.
            if (limit > 200)
            {
                limit = 200;
            }

            // Список результатов.
            List<SecurityLogRecord> logs = new List<SecurityLogRecord>();

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Выполняем запрос последних событий.
                using (NpgsqlCommand command = new NpgsqlCommand(ListLogsSql, connection))
                {
                    command.Parameters.AddWithValue("limit", limit);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            logs.Add(ReadSecurityLog(reader));
                        }
                    }
                }
            }

            // Возвращаем готовый список.
            return logs;
        }

        /// <summary>
        /// Общий метод записи события безопасности.
        /// </summary>
        private async Task WriteEventAsync(int? actorUserId, string actorLogin, string eventType, string message, string target, CancellationToken cancellationToken)
        {
            try
            {
                // Открываем соединение с PostgreSQL.
                using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
                {
                    // Создаём команду вставки события.
                    using (NpgsqlCommand command = new NpgsqlCommand(InsertLogSql, connection))
                    {
                        command.Parameters.AddWithValue("actorUserId", actorUserId.HasValue ? (object)actorUserId.Value : DBNull.Value);
                        command.Parameters.AddWithValue("actorLogin", string.IsNullOrWhiteSpace(actorLogin) ? (object)DBNull.Value : actorLogin);
                        command.Parameters.AddWithValue("eventType", Normalize(eventType));
                        command.Parameters.AddWithValue("message", Normalize(message));
                        command.Parameters.AddWithValue("target", string.IsNullOrWhiteSpace(target) ? (object)DBNull.Value : target.Trim());

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
            catch
            {
                // Ошибка записи лога не должна ломать основное действие пользователя.
            }
        }

        /// <summary>
        /// Проверяет права просмотра журнала безопасности.
        /// </summary>
        private void EnsureCanViewSecurityLogs()
        {
            // Пользователь должен быть авторизован.
            if (!_sessionService.IsAuthenticated)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }

            // Просмотр доступен только админу и статисту.
            if (!_sessionService.CanViewSecurityLogs())
            {
                throw new InvalidOperationException("Логи безопасности доступны только админу и статисту.");
            }
        }

        /// <summary>
        /// Читает запись журнала из текущей строки reader.
        /// </summary>
        private static SecurityLogRecord ReadSecurityLog(NpgsqlDataReader reader)
        {
            return new SecurityLogRecord
            {
                Id = reader.GetInt32(0),
                ActorUserId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                ActorLogin = reader.IsDBNull(2) ? null : reader.GetString(2),
                EventType = reader.GetString(3),
                Message = reader.GetString(4),
                Target = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }

        /// <summary>
        /// Приводит текст к безопасному непустому значению.
        /// </summary>
        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
