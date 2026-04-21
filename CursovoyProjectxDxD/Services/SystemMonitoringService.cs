using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using Npgsql;

namespace CursovoyProjectxDxD.Services
{
    // Сервис отвечает за список наблюдаемых устройств и за сохранение снимков CPU/RAM/HDD.
    public sealed class SystemMonitoringService
    {
        // SQL создает таблицу устройств, если база еще не была подготовлена скриптом.
        private const string CreateDevicesTableSql =
            "CREATE TABLE IF NOT EXISTS monitored_devices (" +
            "id SERIAL PRIMARY KEY, " +
            "name VARCHAR(100) NOT NULL UNIQUE, " +
            "address VARCHAR(255) NULL, " +
            "description TEXT NULL, " +
            "created_at TIMESTAMP NOT NULL DEFAULT NOW()" +
            ");";

        // SQL создает таблицу снимков нагрузки.
        private const string CreateMetricsTableSql =
            "CREATE TABLE IF NOT EXISTS system_metrics (" +
            "id SERIAL PRIMARY KEY, " +
            "device_id INT NOT NULL REFERENCES monitored_devices(id) ON DELETE CASCADE, " +
            "cpu_percent NUMERIC(5,2) NOT NULL, " +
            "ram_percent NUMERIC(5,2) NOT NULL, " +
            "hdd_percent NUMERIC(5,2) NOT NULL, " +
            "created_at TIMESTAMP NOT NULL DEFAULT NOW()" +
            ");";

        // SQL индекс ускоряет просмотр последних снимков конкретного устройства.
        private const string CreateMetricsIndexSql =
            "CREATE INDEX IF NOT EXISTS idx_system_metrics_device_id_created_at " +
            "ON system_metrics(device_id, created_at DESC);";

        // SQL добавления устройства в список наблюдения.
        private const string InsertDeviceSql =
            "INSERT INTO monitored_devices (name, address, description) " +
            "VALUES (@name, @address, @description);";

        // SQL удаления устройства по имени.
        private const string DeleteDeviceSql =
            "DELETE FROM monitored_devices WHERE name = @name;";

        // SQL получения списка устройств.
        private const string ListDevicesSql =
            "SELECT id, name, address, description, created_at " +
            "FROM monitored_devices " +
            "ORDER BY name;";

        // SQL поиска устройства по имени.
        private const string FindDeviceByNameSql =
            "SELECT id, name, address, description, created_at " +
            "FROM monitored_devices " +
            "WHERE name = @name;";

        // SQL сохранения одного снимка нагрузки.
        private const string InsertMetricSql =
            "INSERT INTO system_metrics (device_id, cpu_percent, ram_percent, hdd_percent) " +
            "VALUES (@deviceId, @cpuPercent, @ramPercent, @hddPercent) " +
            "RETURNING id, created_at;";

        // SQL просмотра последних снимков нагрузки по устройству.
        private const string ListMetricsSql =
            "SELECT m.id, m.device_id, d.name, m.cpu_percent, m.ram_percent, m.hdd_percent, m.created_at " +
            "FROM system_metrics m " +
            "JOIN monitored_devices d ON d.id = m.device_id " +
            "WHERE d.name = @name " +
            "ORDER BY m.created_at DESC, m.id DESC " +
            "LIMIT @limit;";

        // Фабрика создает подключения к PostgreSQL.
        private readonly DatabaseConnectionFactory _connectionFactory;

        // Сессия нужна для проверки, что команду выполняет админ или статист.
        private readonly AuthSessionService _sessionService;

        // Получаем зависимости через DI.
        public SystemMonitoringService(DatabaseConnectionFactory connectionFactory, AuthSessionService sessionService)
        {
            // Сохраняем фабрику подключений.
            _connectionFactory = connectionFactory;
            // Сохраняем текущую пользовательскую сессию.
            _sessionService = sessionService;
        }

        // Добавляет устройство в список наблюдения.
        public async Task AddDeviceAsync(string name, string address, string description, CancellationToken cancellationToken)
        {
            // Проверяем право на работу со статистикой.
            EnsureCanViewStatistics();
            // Нормализуем короткое имя устройства.
            name = NormalizeRequired(name, "Имя устройства не может быть пустым.");
            // Адрес может быть пустым, потому что устройство можно обозначить просто именем.
            address = NormalizeOptional(address);
            // Описание тоже необязательно.
            description = NormalizeOptional(description);

            // Открываем соединение с PostgreSQL.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // На всякий случай создаем таблицы, если пользователь еще не запускал SQL-скрипт.
                await EnsureTablesAsync(connection, cancellationToken);

                // Записываем устройство в базу.
                using (NpgsqlCommand command = new NpgsqlCommand(InsertDeviceSql, connection))
                {
                    command.Parameters.AddWithValue("name", name);
                    command.Parameters.AddWithValue("address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                    command.Parameters.AddWithValue("description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        // Удаляет устройство из наблюдения вместе с его историей метрик.
        public async Task<bool> DeleteDeviceAsync(string name, CancellationToken cancellationToken)
        {
            // Проверяем право на работу со статистикой.
            EnsureCanViewStatistics();
            // Имя обязательно, иначе непонятно, что удалять.
            name = NormalizeRequired(name, "Имя устройства не может быть пустым.");

            // Открываем подключение к БД.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаем таблицы при необходимости.
                await EnsureTablesAsync(connection, cancellationToken);

                // Удаляем устройство по имени.
                using (NpgsqlCommand command = new NpgsqlCommand(DeleteDeviceSql, connection))
                {
                    command.Parameters.AddWithValue("name", name);
                    int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                    return affectedRows > 0;
                }
            }
        }

        // Возвращает список всех устройств, которые добавлены в наблюдение.
        public async Task<IReadOnlyList<MonitoredDevice>> ListDevicesAsync(CancellationToken cancellationToken)
        {
            // Проверяем право на просмотр статистики.
            EnsureCanViewStatistics();

            // Список результата.
            List<MonitoredDevice> devices = new List<MonitoredDevice>();

            // Открываем подключение к БД.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаем таблицы при необходимости.
                await EnsureTablesAsync(connection, cancellationToken);

                // Читаем устройства из БД.
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

            // Возвращаем готовый список.
            return devices;
        }

        // Снимает показатели с текущего компьютера и сохраняет их за выбранным устройством.
        public async Task<SystemMetricRecord> CollectLocalMetricsAsync(string deviceName, CancellationToken cancellationToken)
        {
            // Проверяем право на работу со статистикой.
            EnsureCanViewStatistics();
            // Имя устройства обязательно, потому что снимок должен быть привязан к записи в БД.
            deviceName = NormalizeRequired(deviceName, "Имя устройства не может быть пустым.");

            // Сначала получаем значения нагрузки с текущей Windows-машины.
            decimal cpuPercent = GetCpuPercent();
            decimal ramPercent = GetRamPercent();
            decimal hddPercent = GetHddPercent();

            // Открываем подключение к БД.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаем таблицы при необходимости.
                await EnsureTablesAsync(connection, cancellationToken);

                // Находим устройство, к которому будет привязан снимок.
                MonitoredDevice device = await FindDeviceAsync(connection, deviceName, cancellationToken);
                if (device == null)
                {
                    throw new InvalidOperationException("Устройство '" + deviceName + "' не найдено. Сначала добавьте его командой stat device add.");
                }

                // Записываем снимок нагрузки.
                using (NpgsqlCommand command = new NpgsqlCommand(InsertMetricSql, connection))
                {
                    command.Parameters.AddWithValue("deviceId", device.Id);
                    command.Parameters.AddWithValue("cpuPercent", cpuPercent);
                    command.Parameters.AddWithValue("ramPercent", ramPercent);
                    command.Parameters.AddWithValue("hddPercent", hddPercent);

                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (!await reader.ReadAsync(cancellationToken))
                        {
                            throw new InvalidOperationException("Не удалось сохранить снимок нагрузки.");
                        }

                        return new SystemMetricRecord
                        {
                            Id = reader.GetInt32(0),
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            CpuPercent = cpuPercent,
                            RamPercent = ramPercent,
                            HddPercent = hddPercent,
                            CreatedAt = reader.GetDateTime(1)
                        };
                    }
                }
            }
        }

        // Возвращает последние снимки нагрузки для выбранного устройства.
        public async Task<IReadOnlyList<SystemMetricRecord>> ListMetricsAsync(string deviceName, int limit, CancellationToken cancellationToken)
        {
            // Проверяем право на просмотр статистики.
            EnsureCanViewStatistics();
            // Имя устройства обязательно.
            deviceName = NormalizeRequired(deviceName, "Имя устройства не может быть пустым.");
            // Ограничиваем число строк, чтобы случайно не завалить консоль большим выводом.
            limit = NormalizeLimit(limit);

            // Список результата.
            List<SystemMetricRecord> metrics = new List<SystemMetricRecord>();

            // Открываем подключение к БД.
            using (NpgsqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
            {
                // Создаем таблицы при необходимости.
                await EnsureTablesAsync(connection, cancellationToken);

                // Читаем последние снимки.
                using (NpgsqlCommand command = new NpgsqlCommand(ListMetricsSql, connection))
                {
                    command.Parameters.AddWithValue("name", deviceName);
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

            // Возвращаем готовую историю.
            return metrics;
        }

        // Создает таблицы мониторинга, если их еще нет.
        private static async Task EnsureTablesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            // Выполняем создание таблицы устройств.
            using (NpgsqlCommand command = new NpgsqlCommand(CreateDevicesTableSql, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Выполняем создание таблицы метрик.
            using (NpgsqlCommand command = new NpgsqlCommand(CreateMetricsTableSql, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Создаем индекс для быстрых выборок истории.
            using (NpgsqlCommand command = new NpgsqlCommand(CreateMetricsIndexSql, connection))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        // Ищет устройство по имени внутри уже открытого подключения.
        private static async Task<MonitoredDevice> FindDeviceAsync(NpgsqlConnection connection, string name, CancellationToken cancellationToken)
        {
            // Готовим SQL-команду поиска.
            using (NpgsqlCommand command = new NpgsqlCommand(FindDeviceByNameSql, connection))
            {
                command.Parameters.AddWithValue("name", name);

                // Читаем найденную строку.
                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return null;
                    }

                    return ReadDevice(reader);
                }
            }
        }

        // Проверяет, что текущий пользователь имеет доступ к статистике.
        private void EnsureCanViewStatistics()
        {
            // Неавторизованный пользователь не должен работать со статистикой.
            if (!_sessionService.IsAuthenticated)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }

            // Статистика доступна только админу и статисту.
            if (!_sessionService.CanViewStatistics())
            {
                throw new InvalidOperationException("Статистика доступна только админу и статисту.");
            }
        }

        // Читает устройство из текущей строки NpgsqlDataReader.
        private static MonitoredDevice ReadDevice(NpgsqlDataReader reader)
        {
            return new MonitoredDevice
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Address = reader.IsDBNull(2) ? null : reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        // Читает снимок нагрузки из текущей строки NpgsqlDataReader.
        private static SystemMetricRecord ReadMetric(NpgsqlDataReader reader)
        {
            return new SystemMetricRecord
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetInt32(1),
                DeviceName = reader.GetString(2),
                CpuPercent = reader.GetDecimal(3),
                RamPercent = reader.GetDecimal(4),
                HddPercent = reader.GetDecimal(5),
                CreatedAt = reader.GetDateTime(6)
            };
        }

        // Получает нагрузку CPU через стандартный WMI-класс Windows.
        private static decimal GetCpuPercent()
        {
            try
            {
                // Win32_Processor возвращает LoadPercentage по каждому процессору.
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"))
                {
                    decimal total = 0;
                    int count = 0;

                    foreach (ManagementObject item in searcher.Get())
                    {
                        object value = item["LoadPercentage"];
                        if (value != null)
                        {
                            total += Convert.ToDecimal(value);
                            count++;
                        }
                    }

                    return count == 0 ? 0 : ClampPercent(total / count);
                }
            }
            catch
            {
                // Если WMI недоступен, не ломаем команду: возвращаем 0 и сохраняем остальные показатели.
                return 0;
            }
        }

        // Получает процент занятой оперативной памяти через WMI.
        private static decimal GetRamPercent()
        {
            try
            {
                // Win32_OperatingSystem хранит общий и свободный объем памяти в килобайтах.
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        decimal total = Convert.ToDecimal(item["TotalVisibleMemorySize"]);
                        decimal free = Convert.ToDecimal(item["FreePhysicalMemory"]);

                        if (total <= 0)
                        {
                            return 0;
                        }

                        return ClampPercent((total - free) * 100 / total);
                    }
                }

                return 0;
            }
            catch
            {
                // Если WMI недоступен, не ломаем команду.
                return 0;
            }
        }

        // Получает общий процент заполнения всех готовых фиксированных дисков.
        private static decimal GetHddPercent()
        {
            try
            {
                long totalBytes = 0;
                long freeBytes = 0;

                // DriveInfo работает без сторонних библиотек и видит локальные диски Windows.
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        totalBytes += drive.TotalSize;
                        freeBytes += drive.AvailableFreeSpace;
                    }
                }

                if (totalBytes <= 0)
                {
                    return 0;
                }

                return ClampPercent(((decimal)(totalBytes - freeBytes)) * 100 / totalBytes);
            }
            catch
            {
                // Если диски прочитать не удалось, сохраняем 0 вместо падения команды.
                return 0;
            }
        }

        // Ограничивает процент диапазоном 0..100 и оставляет две цифры после запятой.
        private static decimal ClampPercent(decimal value)
        {
            if (value < 0)
            {
                value = 0;
            }

            if (value > 100)
            {
                value = 100;
            }

            return Math.Round(value, 2);
        }

        // Нормализует обязательную строку.
        private static string NormalizeRequired(string value, string errorMessage)
        {
            value = value == null ? string.Empty : value.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }

        // Нормализует необязательную строку.
        private static string NormalizeOptional(string value)
        {
            return value == null ? null : value.Trim();
        }

        // Нормализует лимит истории.
        private static int NormalizeLimit(int limit)
        {
            if (limit <= 0)
            {
                return 10;
            }

            if (limit > 100)
            {
                return 100;
            }

            return limit;
        }
    }
}
