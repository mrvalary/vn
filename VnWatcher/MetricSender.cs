using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace VnWatcher
{
    /// <summary>
    /// Отправляет собранные метрики устройства в PostgreSQL.
    /// </summary>
    internal sealed class MetricSender
    {
        #region Constants

        private const string SaveMetricSql =
            "SELECT save_device_metric(@deviceKey, @deviceName, @cpuPercent, @ramPercent, @hddPercent);";

        #endregion

        #region Fields

        private readonly WatcherSettings _settings;

        #endregion

        #region Construction

        /// <summary>
        /// Создает отправитель метрик.
        /// </summary>
        /// <param name="settings">Настройки watcher-агента.</param>
        public MetricSender(WatcherSettings settings)
        {
            _settings = settings;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Отправляет один снимок метрик в базу данных.
        /// </summary>
        /// <param name="snapshot">Снимок нагрузки CPU, RAM и HDD.</param>
        /// <returns>Задача отправки метрики.</returns>
        public async Task SendAsync(MetricSnapshot snapshot)
        {
            using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.DatabaseTimeoutSeconds)))
            using (NpgsqlConnection connection = new NpgsqlConnection(_settings.ConnectionString))
            {
                await connection.OpenAsync(timeout.Token);

                using (NpgsqlCommand command = new NpgsqlCommand(SaveMetricSql, connection))
                {
                    command.CommandTimeout = _settings.DatabaseTimeoutSeconds;
                    command.Parameters.AddWithValue("deviceKey", _settings.ComputerName);
                    command.Parameters.AddWithValue("deviceName", _settings.ComputerName);
                    command.Parameters.AddWithValue("cpuPercent", snapshot.CpuPercent);
                    command.Parameters.AddWithValue("ramPercent", snapshot.RamPercent);
                    command.Parameters.AddWithValue("hddPercent", snapshot.HddPercent);
                    await command.ExecuteNonQueryAsync(timeout.Token);
                }
            }
        }

        #endregion
    }
}
