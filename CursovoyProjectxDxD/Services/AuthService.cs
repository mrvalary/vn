using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    // Сервис регистрации и входа пользователей через PostgreSQL.
    public sealed class AuthService
    {
        // Минимальная длина пароля.
        private const int MinPasswordLength = 6;

        // SQL поиска пользователя по логину.
        private const string FindUserByLoginSql =
            "SELECT id, login, password_hash FROM users WHERE login = @login;";

        // SQL добавления нового пользователя.
        private const string InsertUserSql =
            "INSERT INTO users (login, password_hash) VALUES (@login, @passwordHash) RETURNING id;";

        // Фабрика создаёт соединения с PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Конструктор получает фабрику соединений через DI.
        public AuthService(DatabaseConnectionFactory connectionFactory)
        {
            // Сохраняем фабрику соединений.
            _connectionFactory = connectionFactory;
        }

        // Регистрирует нового пользователя в таблице users.
        public async Task<AuthResult> RegisterAsync(string login, string password, CancellationToken cancellationToken)
        {
            try
            {
                // Нормализуем логин.
                login = Normalize(login);
                // Нормализуем пароль.
                password = Normalize(password);

                // Логин не должен быть пустым.
                if (string.IsNullOrWhiteSpace(login))
                {
                    return AuthResult.Failure("Логин не может быть пустым.");
                }

                // Пароль не должен быть пустым.
                if (string.IsNullOrWhiteSpace(password))
                {
                    return AuthResult.Failure("Пароль не может быть пустым.");
                }

                // Пароль должен быть не короче минимальной длины.
                if (password.Length < MinPasswordLength)
                {
                    return AuthResult.Failure("Пароль должен содержать минимум " + MinPasswordLength + " символов.");
                }

                // Открываем соединение с PostgreSQL.
                using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
                {
                    // Проверяем, существует ли пользователь с таким логином.
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

                        object result = await insertCommand.ExecuteScalarAsync(cancellationToken);

                        if (result == null || result == DBNull.Value)
                        {
                            return AuthResult.Failure("Не удалось зарегистрировать пользователя.");
                        }
                    }
                }

                // Возвращаем успешный результат регистрации.
                return AuthResult.Success("Пользователь успешно зарегистрирован.");
            }
            catch (Exception ex)
            {
                // Ошибку БД показываем пользователю понятным текстом.
                return AuthResult.Failure("Ошибка при регистрации: " + ex.Message);
            }
        }

        // Выполняет вход пользователя по логину и паролю.
        public async Task<AuthResult> AuthenticateAsync(string login, string password, CancellationToken cancellationToken)
        {
            try
            {
                // Нормализуем логин.
                login = Normalize(login);
                // Нормализуем пароль.
                password = Normalize(password);

                // Логин не должен быть пустым.
                if (string.IsNullOrWhiteSpace(login))
                {
                    return AuthResult.Failure("Логин не может быть пустым.");
                }

                // Пароль не должен быть пустым.
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

                        using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                        {
                            if (!await reader.ReadAsync(cancellationToken))
                            {
                                return AuthResult.Failure("Пользователь не найден.");
                            }

                            int userId = reader.GetInt32(0);
                            string databaseLogin = reader.GetString(1);
                            string passwordHash = reader.GetString(2);
                            string inputHash = ComputeMd5(password);

                            if (!string.Equals(passwordHash, inputHash, StringComparison.OrdinalIgnoreCase))
                            {
                                return AuthResult.Failure("Неверный пароль.");
                            }

                            return AuthResult.Success("Аутентификация успешна.", userId, databaseLogin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ошибку БД показываем пользователю понятным текстом.
                return AuthResult.Failure("Ошибка при аутентификации: " + ex.Message);
            }
        }

        // Приводит строку к безопасному виду.
        private static string Normalize(string value)
        {
            // null заменяем на пустую строку, пробелы по краям убираем.
            return value == null ? string.Empty : value.Trim();
        }

        // Вычисляет MD5-хэш строки.
        private static string ComputeMd5(string value)
        {
            // Создаём объект алгоритма хэширования.
            using (MD5 md5 = MD5.Create())
            {
                // Преобразуем строку в байты.
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

                // Возвращаем итоговый хэш.
                return builder.ToString();
            }
        }
    }
}
