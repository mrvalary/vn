using System;

namespace CursovoyProjectxDxD.Models
{
    // Одна запись журнала безопасности.
    public sealed class SecurityLogRecord
    {
        // Id записи в таблице security_logs.
        public int Id { get; set; }

        // Id пользователя, который выполнил действие, если он известен.
        public int? ActorUserId { get; set; }

        // Логин пользователя, который выполнил действие, если он известен.
        public string ActorLogin { get; set; }

        // Тип события: login_success, login_failed, admin_user_block и т.д.
        public string EventType { get; set; }

        // Текстовое описание события.
        public string Message { get; set; }

        // Целевой пользователь или объект действия, если есть.
        public string Target { get; set; }

        // Дата и время события.
        public DateTime CreatedAt { get; set; }
    }
}
