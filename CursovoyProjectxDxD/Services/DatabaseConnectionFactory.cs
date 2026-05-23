using System;
using System.Configuration;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Фабрика подключений к PostgreSQL.
    /// </summary>
    /// <remarks>
    /// До входа пользователя используется стартовая строка подключения из App.config.
    /// После успешной авторизации сервис хранит строку подключения роли пользователя
    /// и открывает рабочие соединения уже под этой ролью.
    /// </remarks>
    public sealed class DatabaseConnectionFactory
    {
        #region Constants

        // Имя стартовой строки подключения в App.config.
        private const string ConnectionStringName = "NotesDb";

        #endregion

        #region Fields

        // Синхронизация нужна, потому что строка подключения роли меняется после входа/выхода.
        private readonly object _syncRoot = new object();

        // Строка подключения роли текущего пользователя: user/admin/statistician.
        private string _runtimeConnectionString;

        #endregion

        #region Public API

        /// <summary>
        /// Открывает рабочее подключение к PostgreSQL.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены открытия подключения.</param>
        /// <returns>Открытое подключение под ролью пользователя или стартовой ролью, если пользователь ещё не вошёл.</returns>
        public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            // Для обычных операций после авторизации используется строка подключения роли пользователя.
            NpgsqlConnection connection = new NpgsqlConnection(GetRuntimeConnectionString());
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <summary>
        /// Открывает стартовое подключение из App.config.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены открытия подключения.</param>
        /// <returns>Открытое подключение под ролью vn_app_auth.</returns>
        public async Task<NpgsqlConnection> CreateOpenBootstrapConnectionAsync(CancellationToken cancellationToken)
        {
            // Bootstrap-подключение нужно до входа пользователя и для получения строки подключения роли.
            NpgsqlConnection connection = new NpgsqlConnection(GetConfiguredConnectionString());
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <summary>
        /// Сохраняет строку подключения роли после успешной авторизации.
        /// </summary>
        /// <param name="connectionString">Строка подключения, полученная из хранимой функции БД.</param>
        public void SetRuntimeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Хранимая процедура вернула пустую строку подключения роли.");
            }

            // Храним строку роли только после выравнивания адреса сервера по NotesDb из App.config.
            string alignedConnectionString = AlignRoleConnectionString(connectionString);

            lock (_syncRoot)
            {
                _runtimeConnectionString = alignedConnectionString;
            }
        }

        /// <summary>
        /// Очищает строку подключения роли при выходе или перед новой попыткой входа.
        /// </summary>
        public void ClearRuntimeConnectionString()
        {
            lock (_syncRoot)
            {
                _runtimeConnectionString = null;
            }
        }

        /// <summary>
        /// Приводит строку подключения роли к тому же серверу, что указан в App.config.
        /// </summary>
        /// <param name="roleConnectionString">Строка подключения роли из БД.</param>
        /// <returns>Строка роли с актуальными Host, Port и Database из NotesDb.</returns>
        public string AlignRoleConnectionString(string roleConnectionString)
        {
            if (string.IsNullOrWhiteSpace(roleConnectionString))
            {
                throw new InvalidOperationException("Хранимая процедура вернула пустую строку подключения роли.");
            }

            // В БД хранятся учётные данные роли, а адрес сервера берём из App.config.
            // Так приложение не ломается при смене IP сервера PostgreSQL.
            NpgsqlConnectionStringBuilder bootstrapBuilder =
                new NpgsqlConnectionStringBuilder(GetConfiguredConnectionString());
            NpgsqlConnectionStringBuilder roleBuilder =
                new NpgsqlConnectionStringBuilder(roleConnectionString);

            roleBuilder.Host = bootstrapBuilder.Host;
            roleBuilder.Port = bootstrapBuilder.Port;
            roleBuilder.Database = bootstrapBuilder.Database;

            return roleBuilder.ConnectionString;
        }

        /// <summary>
        /// Преобразует техническую ошибку Npgsql в понятное русское сообщение для консоли.
        /// </summary>
        /// <param name="exception">Исключение, возникшее при подключении или SQL-запросе.</param>
        /// <returns>Сообщение, которое можно показать пользователю.</returns>
        public static string FormatDatabaseError(Exception exception)
        {
            if (exception == null)
            {
                return "неизвестная ошибка базы данных.";
            }

            PostgresException postgresException = FindInnerException<PostgresException>(exception);
            if (postgresException != null)
            {
                return FormatPostgresError(postgresException);
            }

            SocketException socketException = FindInnerException<SocketException>(exception);
            if (socketException != null)
            {
                return FormatSocketError(socketException);
            }

            TimeoutException timeoutException = FindInnerException<TimeoutException>(exception);
            if (timeoutException != null)
            {
                return "истекло время ожидания ответа от PostgreSQL. Проверьте доступность сервера БД и сетевое подключение.";
            }

            NpgsqlException npgsqlException = FindInnerException<NpgsqlException>(exception);
            if (npgsqlException != null)
            {
                return "не удалось выполнить операцию PostgreSQL. Проверьте строку подключения, доступность сервера и права роли БД.";
            }

            return exception.Message;
        }

        #endregion

        #region Connection String Selection

        /// <summary>
        /// Возвращает актуальную строку подключения для рабочих операций.
        /// </summary>
        /// <returns>Строка подключения роли пользователя или стартовая строка из App.config.</returns>
        private string GetRuntimeConnectionString()
        {
            lock (_syncRoot)
            {
                // После входа все рабочие сервисы должны использовать ограниченную роль пользователя.
                if (!string.IsNullOrWhiteSpace(_runtimeConnectionString))
                {
                    return _runtimeConnectionString;
                }
            }

            // До входа остаётся только стартовая роль vn_app_auth.
            return GetConfiguredConnectionString();
        }

        /// <summary>
        /// Читает стартовую строку подключения NotesDb из App.config.
        /// </summary>
        /// <returns>Строка подключения vn_app_auth.</returns>
        private string GetConfiguredConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Строка подключения PostgreSQL '" + ConnectionStringName + "' не настроена в App.config.");
            }

            return settings.ConnectionString;
        }

        #endregion

        #region Error Formatting

        /// <summary>
        /// Ищет исключение нужного типа в цепочке InnerException.
        /// </summary>
        /// <typeparam name="TException">Тип искомого исключения.</typeparam>
        /// <param name="exception">Начальное исключение.</param>
        /// <returns>Найденное исключение или null.</returns>
        private static TException FindInnerException<TException>(Exception exception)
            where TException : Exception
        {
            Exception current = exception;
            while (current != null)
            {
                TException typed = current as TException;
                if (typed != null)
                {
                    return typed;
                }

                current = current.InnerException;
            }

            return null;
        }

        /// <summary>
        /// Форматирует ошибку, которую вернул сам PostgreSQL.
        /// </summary>
        /// <param name="exception">PostgreSQL-исключение.</param>
        /// <returns>Понятное русское сообщение.</returns>
        private static string FormatPostgresError(PostgresException exception)
        {
            if (exception.SqlState == "28P01")
            {
                return "PostgreSQL отклонил логин или пароль роли БД. Проверьте строку подключения в App.config или строку роли в таблице roles.";
            }

            if (exception.SqlState == "3D000")
            {
                return "указанная база данных не найдена. Проверьте параметр Database в строке подключения.";
            }

            if (exception.SqlState == "42501")
            {
                return "у роли БД недостаточно прав для выполнения операции.";
            }

            return "PostgreSQL вернул ошибку: " + exception.MessageText;
        }

        /// <summary>
        /// Форматирует сетевую ошибку подключения к PostgreSQL.
        /// </summary>
        /// <param name="exception">Сетевая ошибка сокета.</param>
        /// <returns>Понятное русское сообщение.</returns>
        private static string FormatSocketError(SocketException exception)
        {
            if (exception.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return "сервер PostgreSQL отклонил подключение. Проверьте, что PostgreSQL запущен, а порт из App.config открыт.";
            }

            if (exception.SocketErrorCode == SocketError.TimedOut)
            {
                return "истекло время подключения к PostgreSQL. Проверьте IP-адрес, порт и доступность сети.";
            }

            if (exception.SocketErrorCode == SocketError.HostNotFound ||
                exception.SocketErrorCode == SocketError.NoData)
            {
                return "сервер PostgreSQL не найден. Проверьте Host в строке подключения.";
            }

            if (exception.SocketErrorCode == SocketError.NetworkUnreachable ||
                exception.SocketErrorCode == SocketError.HostUnreachable)
            {
                return "сервер PostgreSQL недоступен по сети. Проверьте подключение к сети, IP-адрес и настройки firewall.";
            }

            return "не удалось подключиться к PostgreSQL по сети. Детали: " + exception.Message;
        }

        #endregion
    }
}
