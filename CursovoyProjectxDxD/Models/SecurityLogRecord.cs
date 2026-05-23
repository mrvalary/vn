using System;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Одна запись журнала безопасности.
    /// </summary>
    public sealed class SecurityLogRecord
    {
        /// <summary>
        /// Id записи в таблице security_logs.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id пользователя, который выполнил действие, если он известен.
        /// </summary>
        public int? ActorUserId { get; set; }

        /// <summary>
        /// Логин пользователя, который выполнил действие, если он известен.
        /// </summary>
        public string ActorLogin { get; set; }

        /// <summary>
        /// Тип события: login_success, login_failed, admin_user_block и т.д.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Текстовое описание события.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Целевой пользователь или объект действия, если есть.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Дата и время события.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
