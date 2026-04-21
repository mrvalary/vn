using CursovoyProjectxDxD.Models;

namespace CursovoyProjectxDxD.Services
{
    // Хранит данные текущей локальной сессии пользователя.
    public sealed class AuthSessionService
    {
        // Пользователь считается авторизованным, если у него есть id и логин.
        public bool IsAuthenticated
        {
            get { return CurrentUserId.HasValue && !string.IsNullOrWhiteSpace(CurrentLogin); }
        }

        // Id текущего пользователя из таблицы users.
        public int? CurrentUserId { get; private set; }

        // Логин текущего пользователя.
        public string CurrentLogin { get; private set; }

        // Системное название роли текущего пользователя.
        public string CurrentRoleName { get; private set; }

        // Русское название роли для вывода в консоль.
        public string CurrentRoleDisplayName
        {
            get { return UserRole.GetDisplayName(CurrentRoleName); }
        }

        // Открывает сессию после успешного входа.
        public void SignIn(int userId, string login, string roleName)
        {
            // Запоминаем id пользователя.
            CurrentUserId = userId;
            // Запоминаем логин пользователя.
            CurrentLogin = login;
            // Запоминаем роль пользователя.
            CurrentRoleName = string.IsNullOrWhiteSpace(roleName) ? UserRole.User : roleName;
        }

        // Полностью очищает данные текущей сессии.
        public void SignOut()
        {
            // Убираем id пользователя.
            CurrentUserId = null;
            // Убираем логин пользователя.
            CurrentLogin = null;
            // Убираем роль пользователя.
            CurrentRoleName = null;
        }

        // Проверяет, является ли текущий пользователь администратором.
        public bool IsAdmin()
        {
            // Роль admin используется админскими командами.
            return CurrentRoleName == UserRole.Admin;
        }

        // Проверяет, может ли пользователь просматривать будущую статистику.
        public bool CanViewStatistics()
        {
            // Статист и админ смогут видеть будущие вотчи/статистику.
            return CurrentRoleName == UserRole.Statistician || CurrentRoleName == UserRole.Admin;
        }

        // Проверяет, может ли пользователь просматривать логи безопасности.
        public bool CanViewSecurityLogs()
        {
            // Логи безопасности доступны админу и статисту.
            return CurrentRoleName == UserRole.Admin || CurrentRoleName == UserRole.Statistician;
        }
    }
}
