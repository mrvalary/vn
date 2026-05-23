using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Сервис просмотра и редактирования устройств, которые присылают метрики через VnWatcher.
    /// </summary>
    public sealed class MonitoringService
    {
        #region SQL

        private const string UpsertDeviceSql =
            "INSERT INTO monitored_devices (device_key, name, address, description, is_active) " +
            "VALUES (@deviceKey, @name, @address, @description, TRUE) " +
            "ON CONFLICT (device_key) DO UPDATE " +
            "SET name = EXCLUDED.name, address = EXCLUDED.address, description = EXCLUDED.description, is_active = TRUE;";

        private const string DeleteDeviceSql =
            "DELETE FROM monitored_devices WHERE device_key = @deviceKey;";

        private const string ListDevicesSql =
            "SELECT id, device_key, name, address, description, is_active, last_seen_at, created_at " +
            "FROM monitored_devices " +
            "ORDER BY name, device_key;";

        private const string ListMetricsSql =
            "SELECT m.id, m.device_id, d.device_key, d.name, m.cpu_percent, m.ram_percent, m.hdd_percent, m.created_at " +
            "FROM system_metrics m " +
            "JOIN monitored_devices d ON d.id = m.device_id " +
            "WHERE d.device_key = @deviceKey " +
            "ORDER BY m.created_at DESC, m.id DESC " +
            "LIMIT @limit;";

        #endregion

        #region Fields

        private readonly DatabaseConnectionFactory _connectionFactory;
        private readonly AuthSessionService _sessionService;

        #endregion

        #region Construction

        public MonitoringService(DatabaseConnectionFactory connectionFactory, AuthSessionService sessionService)
        {
            _connectionFactory = connectionFactory;
            _sessionService = sessionService;
        }

        #endregion

        #region Devices

        /// <summary>
        /// Админ или статист могут вручную поправить карточку устройства, которое агент создает автоматически.
        /// </summary>
        public async Task SaveDeviceAsync(string deviceKey, string name, string address, string description, CancellationToken cancellationToken)
        {
            EnsureCanManageMonitoring();

            deviceKey = NormalizeRequired(deviceKey, "Ключ устройства не может быть пустым.");
            name = NormalizeRequired(name, "Имя устройства не может быть пустым.");
            address = NormalizeOptional(address);
            description = NormalizeOptional(description);

            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(UpsertDeviceSql, connection))
                {
                    command.Parameters.AddWithValue("deviceKey", deviceKey);
                    command.Parameters.AddWithValue("name", name);
                    command.Parameters.AddWithValue("address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                    command.Parameters.AddWithValue("description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// Удаление устройства удаляет и его историю метрик через ON DELETE CASCADE.
        /// </summary>
        public async Task<bool> DeleteDeviceAsync(string deviceKey, CancellationToken cancellationToken)
        {
            EnsureCanManageMonitoring();
            deviceKey = NormalizeRequired(deviceKey, "Ключ устройства не может быть пустым.");

            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(DeleteDeviceSql, connection))
                {
                    command.Parameters.AddWithValue("deviceKey", deviceKey);
                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        /// <summary>
        /// Возвращает устройства, которые уже присылали метрики или были добавлены вручную.
        /// </summary>
        public async Task<IReadOnlyList<MonitoredDevice>> ListDevicesAsync(CancellationToken cancellationToken)
        {
            EnsureCanManageMonitoring();

            List<MonitoredDevice> devices = new List<MonitoredDevice>();

            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(ListDevicesSql, connection))
                {
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            devices.Add(ReadDevice(reader));
                        }
                    }
                }
            }

            return devices;
        }

        #endregion

        #region Metrics

        /// <summary>
        /// Показывает последние снимки нагрузки по выбранному устройству.
        /// </summary>
        public async Task<IReadOnlyList<SystemMetricRecord>> ListMetricsAsync(string deviceKey, int limit, CancellationToken cancellationToken)
        {
            EnsureCanManageMonitoring();
            deviceKey = NormalizeRequired(deviceKey, "Ключ устройства не может быть пустым.");
            limit = NormalizeLimit(limit);

            List<SystemMetricRecord> metrics = new List<SystemMetricRecord>();

            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(ListMetricsSql, connection))
                {
                    command.Parameters.AddWithValue("deviceKey", deviceKey);
                    command.Parameters.AddWithValue("limit", limit);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            metrics.Add(ReadMetric(reader));
                        }
                    }
                }
            }

            return metrics;
        }

        #endregion

        #region Guards

        private void EnsureCanManageMonitoring()
        {
            if (!_sessionService.IsAuthenticated)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }

            if (!_sessionService.CanManageMonitoring())
            {
                throw new InvalidOperationException("Мониторинг доступен только админу или статисту.");
            }
        }

        #endregion

        #region Mapping

        private static MonitoredDevice ReadDevice(NpgsqlDataReader reader)
        {
            return new MonitoredDevice
            {
                Id = reader.GetInt32(0),
                DeviceKey = reader.GetString(1),
                Name = reader.GetString(2),
                Address = reader.IsDBNull(3) ? null : reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                IsActive = reader.GetBoolean(5),
                LastSeenAt = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        private static SystemMetricRecord ReadMetric(NpgsqlDataReader reader)
        {
            return new SystemMetricRecord
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetInt32(1),
                DeviceKey = reader.GetString(2),
                DeviceName = reader.GetString(3),
                CpuPercent = reader.GetDecimal(4),
                RamPercent = reader.GetDecimal(5),
                HddPercent = reader.GetDecimal(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        #endregion

        #region Normalization

        private static string NormalizeRequired(string value, string errorMessage)
        {
            value = value == null ? string.Empty : value.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }

        private static string NormalizeOptional(string value)
        {
            return value == null ? null : value.Trim();
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 10;
            }

            if (limit > 200)
            {
                return 200;
            }

            return limit;
        }

        #endregion
    }
}
