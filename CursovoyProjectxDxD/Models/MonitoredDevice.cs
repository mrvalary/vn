using System;

namespace CursovoyProjectxDxD.Models
{
    // Карточка устройства, которое VnWatcher зарегистрировал в базе данных.
    public sealed class MonitoredDevice
    {
        #region Identity

        // Внутренний id записи в monitored_devices.
        public int Id { get; set; }

        // Стабильный ключ устройства, который присылает агент.
        public string DeviceKey { get; set; }

        #endregion

        #region Display

        // Понятное имя устройства для вывода в списке.
        public string Name { get; set; }

        // Адрес или инвентарная привязка устройства, если ее указал админ или статист.
        public string Address { get; set; }

        // Дополнительное описание устройства.
        public string Description { get; set; }

        #endregion

        #region State

        // Флаг активности устройства в мониторинге.
        public bool IsActive { get; set; }

        // Время последнего пакета метрик от агента.
        public DateTime? LastSeenAt { get; set; }

        // Время создания карточки устройства.
        public DateTime CreatedAt { get; set; }

        #endregion
    }
}
