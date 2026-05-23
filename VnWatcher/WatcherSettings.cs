using System;
using System.Configuration;

namespace VnWatcher
{
    /// <summary>
    /// Настройки watcher-агента из App.config и watcher.yml.
    /// </summary>
    internal sealed class WatcherSettings
    {
        #region Constants

        private const string ConnectionStringName = "WatcherDb";
        private const string YamlConfigFileName = "watcher.yml";

        #endregion

        #region Properties

        /// <summary>
        /// Строка подключения к базе данных для роли watcher.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Имя текущего компьютера, по которому метрики сохраняются в БД.
        /// </summary>
        public string ComputerName { get; private set; }

        /// <summary>
        /// Интервал между отправками метрик в секундах.
        /// </summary>
        public int IntervalSeconds { get; private set; }

        /// <summary>
        /// Таймаут обращения к базе данных в секундах.
        /// </summary>
        public int DatabaseTimeoutSeconds { get; private set; }

        /// <summary>
        /// Путь диска для расчёта HDD. Пустое значение означает все локальные диски.
        /// </summary>
        public string HddPath { get; private set; }

        #endregion

        #region Load

        /// <summary>
        /// Загружает настройки watcher-агента из конфигурационных файлов.
        /// </summary>
        /// <returns>Готовые настройки для запуска агента.</returns>
        public static WatcherSettings Load()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("Строка подключения '" + ConnectionStringName + "' не настроена.");
            }

            SimpleYamlConfig yaml = SimpleYamlConfig.Load(YamlConfigFileName);
            string machineName = Environment.MachineName;

            return new WatcherSettings
            {
                ConnectionString = settings.ConnectionString,
                ComputerName = machineName,
                IntervalSeconds = yaml.GetPositiveInt("watcher", "intervalSeconds", 60),
                DatabaseTimeoutSeconds = yaml.GetPositiveInt("watcher", "databaseTimeoutSeconds", 10),
                HddPath = yaml.GetValue("watcher", "hddPath", string.Empty)
            };
        }

        #endregion
    }
}
