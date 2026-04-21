using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    // Сервис регистрации, входа и администрирования учётных записей.
    public sealed class AuthService
    {
        // Минимальная длина пароля для обычной регистрации и админского создания пользователя.
        private const int MinPasswordLength = 6;

        // SQL поиска пользователя по логину вместе с ролью и флагом блокировки.
        private const string FindUserByLoginSql =
            "SELECT u.id, u.login, u.password_hash, r.name, u.is_blocked " +
            "FROM users u " +
            "JOIN roles r ON r.id = u.role_id " +
            "WHERE u.login = @login;";

        // SQL добавления обычного пользователя при самостоятельной регистрации.
        private const string InsertUserSql =
            "INSERT INTO users (login, password_hash, role_id, is_blocked) " +
            "VALUES (@login, @passwordHash, (SELECT id FROM roles WHERE name = @roleName), FALSE) " +
            "RETURNING id;";

        // SQL получения информации о пользователе для админских команд.
        private const string GetUserInfoSql =
            "SELECT u.id, u.login, r.name, u.is_blocked, u.created_at " +
            "FROM users u " +
            "JOIN roles r ON r.id = u.role_id " +
            "WHERE u.login = @login;";

        // SQL удаления пользователя по логину.
        private const string DeleteUserSql =
            "DELETE FROM users WHERE login = @login;";

        // SQL блокировки или разблокировки пользователя.
        private const string SetUserBlockedSql =
            "UPDATE users SET is_blocked = @isBlocked WHERE login = @login;";

        // Фабрика создаёт открытые подключения к PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Получаем зависимости через DI.
        public AuthService(DatabaseConnectionFactory connectionFactory)
        {
            // Сохраняем фабрику подключений для всех методов сервиса.
            _connectionFactory = connectionFactory;
        }

        // Регистрирует обычного пользователя с ролью user.
        public async Task<AuthResult> RegisterAsync(string login, string password, CancellationToken cancellationToken)
        {
            // Самостоятельная регистрация всегда создаёт обычного пользователя.
            return await CreateUserInternalAsync(login, password, UserRole.User, "Пользователь успешно зарегистрирован.", cancellationToken);
        }

        // Выполняет вход пользователя по логину и паролю.
        public async Task<AuthResult> AuthenticateAsync(string login, string password, CancellationToken cancellationToken)
        {
            try
            {
                // Нормализуем логин и пароль перед проверками.
                login = Normalize(login);
                password = Normalize(password);

                // Логин обязателен.
                if (string.IsNullOrWhiteSpace(login))
                {
                    return AuthResult.Failure("Логин не может быть пустым.");
                }

                // Пароль обязателен.
                if (string.IsNullOrWhiteSpace(password))
                {
                    return AuthResult.Failure("Пароль не может быть пустым.");
                }

                // Открываем соединение с PostgreSQL.
                using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
                {
                    // Ищем пользователя по логину.
                    using (NpgsqlCommand command = new NpgsqlCommand(FindUserByLoginSql, connection))
                    {
                        command.Parameters.AddWithValue("login", login);

                        // Читаем найденную строку.
                        using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                        {
                            // Если строки нет, такого логина в БД нет.
                            if (!await reader.ReadAsync(cancellationToken))
                            {
                                return AuthResult.Failure("Пользователь не найден.");
                            }

                            // Забираем данные пользователя из результата запроса.
                            int userId = reader.GetInt32(0);
                            string databaseLogin = reader.GetString(1);
                            string passwordHash = reader.GetString(2);
                            string roleName = reader.GetString(3);
                            bool isBlocked = reader.GetBoolean(4);

                            // Заблокированный пользователь не может войти даже с правильным паролем.
                            if (isBlocked)
                            {
                                return AuthResult.Failure("Учётная запись заблокирована администратором.");
                            }

                            // Сравниваем MD5 введённого пароля с хэшем из БД.
                            string inputHash = ComputeMd5(password);
                            if (!string.Equals(passwordHash, inputHash, StringComparison.OrdinalIgnoreCase))
                            {
                                return AuthResult.Failure("Неверный пароль.");
                            }

                            // Успешный вход возвращает id, логин и роль для AuthSessionService.
                            return AuthResult.Success("Аутентификация успешна.", userId, databaseLogin, roleName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ошибку подключения или SQL возвращаем пользователю понятным сообщением.
                return AuthResult.Failure("Ошибка при аутентификации: " + ex.Message);
            }
        }

        // Создаёт пользователя от имени администратора с выбранной ролью.
        public async Task<AuthResult> CreateUserAsync(string login, string password, string roleName, CancellationToken cancellationToken)
        {
            // Админ может создать user, admin или statistician.
            return await CreateUserInternalAsync(login, password, roleName, null, cancellationToken);
        }

        // Удаляет пользователя по логину.
        public async Task<bool> DeleteUserAsync(string login, CancellationToken cancellationToken)
        {
            // Нормализуем логин.
            login = Normalize(login);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Удаляем пользователя; его заметки удалятся каскадно из-за ON DELETE CASCADE.
                using (NpgsqlCommand command = new NpgsqlCommand(DeleteUserSql, connection))
                {
                    command.Parameters.AddWithValue("login", login);
                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        // Блокирует или разблокирует пользователя.
        public async Task<bool> SetUserBlockedAsync(string login, bool isBlocked, CancellationToken cancellationToken)
        {
            // Нормализуем логин.
            login = Normalize(login);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Меняем флаг блокировки.
                using (NpgsqlCommand command = new NpgsqlCommand(SetUserBlockedSql, connection))
                {
                    command.Parameters.AddWithValue("login", login);
                    command.Parameters.AddWithValue("isBlocked", isBlocked);
                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        // Возвращает информацию о пользователе.
        public async Task<UserAccount> GetUserInfoAsync(string login, CancellationToken cancellationToken)
        {
            // Нормализуем логин.
            login = Normalize(login);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Читаем пользователя вместе с ролью.
                using (NpgsqlCommand command = new NpgsqlCommand(GetUserInfoSql, connection))
                {
                    command.Parameters.AddWithValue("login", login);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            return null;
                        }

                        return ReadUserAccount(reader);
                    }
                }
            }
        }

        // Возвращает список всех пользователей.
        public async Task<IReadOnlyList<UserAccount>> ListUsersAsync(CancellationToken cancellationToken)
        {
            // SQL списка пользователей держим внутри метода, потому что он нужен только здесь.
            const string sql =
                "SELECT u.id, u.login, r.name, u.is_blocked, u.created_at " +
                "FROM users u " +
                "JOIN roles r ON r.id = u.role_id " +
                "ORDER BY u.id;";

            // Список результатов.
            List<UserAccount> users = new List<UserAccount>();

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Выполняем запрос списка пользователей.
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            users.Add(ReadUserAccount(reader));
                        }
                    }
                }
            }

            // Возвращаем готовый список.
            return users;
        }

        // Общий метод создания пользователя для регистрации и админской команды.
        private async Task<AuthResult> CreateUserInternalAsync(string login, string password, string roleName, string successMessage, CancellationToken cancellationToken)
        {
            try
            {
                // Нормализуем входные данные.
                login = Normalize(login);
                password = Normalize(password);
                roleName = NormalizeRole(roleName);

                // Проверяем логин и пароль.
                AuthResult validation = ValidateCredentials(login, password);
                if (!validation.IsSuccess)
                {
                    return validation;
                }

                // Проверяем, что роль поддерживается приложением.
                if (!IsKnownRole(roleName))
                {
                    return AuthResult.Failure("Неизвестная роль. Доступные роли: user, admin, statistician.");
                }

                // Открываем соединение с PostgreSQL.
                using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
                {
                    // Проверяем, что логин ещё не занят.
                    using (NpgsqlCommand findCommand = new NpgsqlCommand(FindUserByLoginSql, connection))
                    {
                        findCommand.Parameters.AddWithValue("login", login);

                        using (NpgsqlDataReader reader = await findCommand.ExecuteReaderAsync(cancellationToken))
                        {
                            if (await reader.ReadAsync(cancellationToken))
                            {
                                return AuthResult.Failure("Пользователь с таким логином уже существует.");
                            }
                        }
                    }

                    // Хэшируем пароль перед сохранением.
                    string passwordHash = ComputeMd5(password);

                    // Добавляем пользователя в БД.
                    using (NpgsqlCommand insertCommand = new NpgsqlCommand(InsertUserSql, connection))
                    {
                        insertCommand.Parameters.AddWithValue("login", login);
                        insertCommand.Parameters.AddWithValue("passwordHash", passwordHash);
                        insertCommand.Parameters.AddWithValue("roleName", roleName);

                        object result = await insertCommand.ExecuteScalarAsync(cancellationToken);
                        if (result == null || result == DBNull.Value)
                        {
                            return AuthResult.Failure("Не удалось создать пользователя.");
                        }
                    }
                }

                // Если особое сообщение не передано, формируем админское сообщение с ролью.
                string message = successMessage ??
                    "Пользователь " + login + " создан с ролью " + UserRole.GetDisplayName(roleName) + ".";

                return AuthResult.Success(message);
            }
            catch (Exception ex)
            {
                // Возвращаем ошибку как AuthResult, чтобы команды не падали аварийно.
                return AuthResult.Failure("Ошибка создания пользователя: " + ex.Message);
            }
        }

        // Приводит строку к безопасному виду.
        private static string Normalize(string value)
        {
            // null превращаем в пустую строку, пробелы по краям убираем.
            return value == null ? string.Empty : value.Trim();
        }

        // Нормализует роль: пустая роль превращается в обычного пользователя.
        private static string NormalizeRole(string value)
        {
            // Роли хранятся маленькими латинскими строками.
            string roleName = Normalize(value).ToLowerInvariant();
            return string.IsNullOrWhiteSpace(roleName) ? UserRole.User : roleName;
        }

        // Проверяет, что роль входит в список поддерживаемых ролей.
        private static bool IsKnownRole(string roleName)
        {
            return roleName == UserRole.User ||
                   roleName == UserRole.Admin ||
                   roleName == UserRole.Statistician;
        }

        // Проверяет логин и пароль перед созданием пользователя.
        private static AuthResult ValidateCredentials(string login, string password)
        {
            // Логин обязателен.
            if (string.IsNullOrWhiteSpace(login))
            {
                return AuthResult.Failure("Логин не может быть пустым.");
            }

            // Пароль обязателен.
            if (string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.Failure("Пароль не может быть пустым.");
            }

            // Пароль должен быть не короче минимальной длины.
            if (password.Length < MinPasswordLength)
            {
                return AuthResult.Failure("Пароль должен содержать минимум " + MinPasswordLength + " символов.");
            }

            // Проверки пройдены.
            return AuthResult.Success("Данные корректны.");
        }

        // Читает UserAccount из текущей строки NpgsqlDataReader.
        private static UserAccount ReadUserAccount(NpgsqlDataReader reader)
        {
            return new UserAccount
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                RoleName = reader.GetString(2),
                IsBlocked = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        // Вычисляет MD5-хэш строки.
        private static string ComputeMd5(string value)
        {
            // Создаём объект алгоритма хэширования.
            using (MD5 md5 = MD5.Create())
            {
                // Преобразуем строку в байты UTF-8.
                byte[] inputBytes = Encoding.UTF8.GetBytes(value);
                // Вычисляем хэш.
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                // Собираем hex-строку.
                StringBuilder builder = new StringBuilder();

                // Каждый байт переводим в две hex-цифры.
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                // Возвращаем готовый хэш.
                return builder.ToString();
            }
        }
    }
}
