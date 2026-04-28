using CursovoyProjectxDxD.Models;

namespace CursovoyProjectxDxD.Services
{
    // Хранит данные текущей локальной сессии пользователя.
    public sealed class AuthSessionService
    {
        #region Current User

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

        #endregion

        #region Session State

        // Открывает сессию после успешного входа.
        public void SignIn(int userId, string login, string roleName)
        {
            CurrentUserId = userId;
            CurrentLogin = login;
            CurrentRoleName = string.IsNullOrWhiteSpace(roleName) ? UserRole.User : roleName;
        }

        // Полностью очищает данные текущей сессии.
        public void SignOut()
        {
            CurrentUserId = null;
            CurrentLogin = null;
            CurrentRoleName = null;
        }

        #endregion

        #region Role Checks

        // Проверяет, является ли текущий пользователь администратором.
        public bool IsAdmin()
        {
            return CurrentRoleName == UserRole.Admin;
        }

        // Проверяет, является ли текущий пользователь статистом.
        public bool IsStatistician()
        {
            return CurrentRoleName == UserRole.Statistician;
        }

        // Мониторинг доступен администратору и статисту.
        public bool CanManageMonitoring()
        {
            return IsAdmin() || IsStatistician();
        }

        // Логи безопасности доступны только администратору.
        public bool CanViewSecurityLogs()
        {
            return IsAdmin();
        }

        #endregion
    }
}
