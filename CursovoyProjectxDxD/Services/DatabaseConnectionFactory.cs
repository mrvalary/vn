using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    // Фабрика создаёт соединения с PostgreSQL по строке подключения из App.config.
    public sealed class DatabaseConnectionFactory
    {
        // Имя connection string в App.config.
        private const string ConnectionStringName = "NotesDb";

        // Создаёт и открывает соединение с базой данных.
        public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            // Создаём объект подключения по строке из конфигурации.
            NpgsqlConnection connection = new NpgsqlConnection(GetConnectionString());
            // Открываем физическое соединение с PostgreSQL.
            await connection.OpenAsync(cancellationToken);
            // Возвращаем уже открытое соединение вызывающей стороне.
            return connection;
        }

        // Считывает строку подключения из App.config.
        private string GetConnectionString()
        {
            // Пытаемся найти named connection string в конфигурации приложения.
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            // Если строка подключения не задана, даём понятную ошибку.
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Не настроено подключение к PostgreSQL. Заполните connection string '" + ConnectionStringName + "' в App.config.");
            }

            // Возвращаем готовую строку подключения.
            return settings.ConnectionString;
        }
    }
}
