using System;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Карточка устройства, которое VnWatcher зарегистрировал в базе данных.
    /// </summary>
    public sealed class MonitoredDevice
    {
        #region Identity

        /// <summary>
        /// Внутренний id записи в monitored_devices.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Стабильный ключ устройства, который присылает агент.
        /// </summary>
        public string DeviceKey { get; set; }

        #endregion

        #region Display

        /// <summary>
        /// Понятное имя устройства для вывода в списке.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Адрес или инвентарная привязка устройства, если ее указал админ или статист.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Дополнительное описание устройства.
        /// </summary>
        public string Description { get; set; }

        #endregion

        #region State

        /// <summary>
        /// Флаг активности устройства в мониторинге.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Время последнего пакета метрик от агента.
        /// </summary>
        public DateTime? LastSeenAt { get; set; }

        /// <summary>
        /// Время создания карточки устройства.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #endregion
    }
}
