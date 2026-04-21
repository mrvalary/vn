using System;

namespace CursovoyProjectxDxD.Models
{
    // Модель устройства, за которым пользователь хочет наблюдать.
    public sealed class MonitoredDevice
    {
        // Внутренний id устройства в PostgreSQL.
        public int Id { get; set; }

        // Короткое понятное имя устройства: pc-home, server-1, notebook и т.п.
        public string Name { get; set; }

        // Адрес устройства хранится отдельно, чтобы позже можно было добавить удаленный агент.
        public string Address { get; set; }

        // Описание помогает понять, зачем устройство добавлено в наблюдение.
        public string Description { get; set; }

        // Дата добавления устройства в список наблюдения.
        public DateTime CreatedAt { get; set; }
    }
}
