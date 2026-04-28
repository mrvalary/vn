using System;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Один снимок нагрузки устройства, полученный от VnWatcher.
    /// </summary>
    public sealed class SystemMetricRecord
    {
        #region Identity

        /// <summary>
        /// Внутренний id записи в system_metrics.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id устройства из monitored_devices.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Стабильный ключ устройства.
        /// </summary>
        public string DeviceKey { get; set; }

        /// <summary>
        /// Имя устройства на момент чтения метрики.
        /// </summary>
        public string DeviceName { get; set; }

        #endregion

        #region Load Values

        /// <summary>
        /// Загрузка CPU в процентах.
        /// </summary>
        public decimal CpuPercent { get; set; }

        /// <summary>
        /// Загрузка RAM в процентах.
        /// </summary>
        public decimal RamPercent { get; set; }

        /// <summary>
        /// Загрузка HDD в процентах.
        /// </summary>
        public decimal HddPercent { get; set; }

        #endregion

        #region Time

        /// <summary>
        /// Время получения метрики сервером БД.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #endregion
    }
}
