using System;

namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Модель учётной записи пользователя для админских команд.
    /// </summary>
    public sealed class UserAccount
    {
        /// <summary>
        /// Id пользователя в таблице users.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин пользователя.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Системное название роли: user или admin.
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// Показывает, заблокирована ли учётная запись.
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Дата создания пользователя, если колонка уже есть в базе.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
