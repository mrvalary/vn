using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    public sealed class DatabaseConnectionFactory
    {
        private const string ConnectionStringName = "NotesDb";

        private readonly object _syncRoot = new object();
        private string _runtimeConnectionString;

        public async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
        {
            NpgsqlConnection connection = new NpgsqlConnection(GetRuntimeConnectionString());
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        public async Task<NpgsqlConnection> CreateOpenBootstrapConnectionAsync(CancellationToken cancellationToken)
        {
            NpgsqlConnection connection = new NpgsqlConnection(GetConfiguredConnectionString());
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        public void SetRuntimeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Stored procedure returned an empty role connection string.");
            }

            lock (_syncRoot)
            {
                _runtimeConnectionString = connectionString.Trim();
            }
        }

        public void ClearRuntimeConnectionString()
        {
            lock (_syncRoot)
            {
                _runtimeConnectionString = null;
            }
        }

        private string GetRuntimeConnectionString()
        {
            lock (_syncRoot)
            {
                if (!string.IsNullOrWhiteSpace(_runtimeConnectionString))
                {
                    return _runtimeConnectionString;
                }
            }

            return GetConfiguredConnectionString();
        }

        private string GetConfiguredConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];

            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string '" + ConnectionStringName + "' is not configured in App.config.");
            }

            return settings.ConnectionString;
        }
    }
}
