using System;

namespace CursovoyProjectxDxD.Models
{
    // Модель учётной записи пользователя для админских команд.
    public sealed class UserAccount
    {
        // Id пользователя в таблице users.
        public int Id { get; set; }

        // Логин пользователя.
        public string Login { get; set; }

        // Системное название роли: user, admin или statistician.
        public string RoleName { get; set; }

        // Показывает, заблокирована ли учётная запись.
        public bool IsBlocked { get; set; }

        // Дата создания пользователя, если колонка уже есть в базе.
        public DateTime CreatedAt { get; set; }
    }
}
