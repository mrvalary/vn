using System;

namespace CursovoyProjectxDxD.Models
{
    // Один снимок нагрузки устройства, полученный от VnWatcher.
    public sealed class SystemMetricRecord
    {
        #region Identity

        // Внутренний id записи в system_metrics.
        public int Id { get; set; }

        // Id устройства из monitored_devices.
        public int DeviceId { get; set; }

        // Стабильный ключ устройства.
        public string DeviceKey { get; set; }

        // Имя устройства на момент чтения метрики.
        public string DeviceName { get; set; }

        #endregion

        #region Load Values

        // Загрузка CPU в процентах.
        public decimal CpuPercent { get; set; }

        // Загрузка RAM в процентах.
        public decimal RamPercent { get; set; }

        // Загрузка HDD в процентах.
        public decimal HddPercent { get; set; }

        #endregion

        #region Time

        // Время получения метрики сервером БД.
        public DateTime CreatedAt { get; set; }

        #endregion
    }
}
