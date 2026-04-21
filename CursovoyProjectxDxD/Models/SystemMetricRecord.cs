using System;

namespace CursovoyProjectxDxD.Models
{
    // Один снимок нагрузки устройства.
    public sealed class SystemMetricRecord
    {
        // Id записи в таблице system_metrics.
        public int Id { get; set; }

        // Id устройства из таблицы monitored_devices.
        public int DeviceId { get; set; }

        // Имя устройства выводим вместе со снимком, чтобы результат был удобнее читать.
        public string DeviceName { get; set; }

        // Текущая нагрузка процессора в процентах.
        public decimal CpuPercent { get; set; }

        // Использование оперативной памяти в процентах.
        public decimal RamPercent { get; set; }

        // Использование постоянной памяти на дисках HDD/SSD в процентах.
        public decimal HddPercent { get; set; }

        // Время, когда снимок был сохранен.
        public DateTime CreatedAt { get; set; }
    }
}
