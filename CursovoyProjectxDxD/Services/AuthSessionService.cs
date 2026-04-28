using CursovoyProjectxDxD.Models;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Хранит данные текущей локальной сессии пользователя.
    /// </summary>
    public sealed class AuthSessionService
    {
        #region Current User

        /// <summary>
        /// Пользователь считается авторизованным, если у него есть id и логин.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return CurrentUserId.HasValue && !string.IsNullOrWhiteSpace(CurrentLogin); }
        }

        /// <summary>
        /// Id текущего пользователя из таблицы users.
        /// </summary>
        public int? CurrentUserId { get; private set; }

        /// <summary>
        /// Логин текущего пользователя.
        /// </summary>
        public string CurrentLogin { get; private set; }

        /// <summary>
        /// Системное название роли текущего пользователя.
        /// </summary>
        public string CurrentRoleName { get; private set; }

        /// <summary>
        /// Русское название роли для вывода в консоль.
        /// </summary>
        public string CurrentRoleDisplayName
        {
            get { return UserRole.GetDisplayName(CurrentRoleName); }
        }

        #endregion

        #region Session State

        /// <summary>
        /// Открывает сессию после успешного входа.
        /// </summary>
        public void SignIn(int userId, string login, string roleName)
        {
            CurrentUserId = userId;
            CurrentLogin = login;
            CurrentRoleName = string.IsNullOrWhiteSpace(roleName) ? UserRole.User : roleName;
        }

        /// <summary>
        /// Полностью очищает данные текущей сессии.
        /// </summary>
        public void SignOut()
        {
            CurrentUserId = null;
            CurrentLogin = null;
            CurrentRoleName = null;
        }

        #endregion

        #region Role Checks

        /// <summary>
        /// Проверяет, является ли текущий пользователь администратором.
        /// </summary>
        public bool IsAdmin()
        {
            return CurrentRoleName == UserRole.Admin;
        }

        /// <summary>
        /// Проверяет, является ли текущий пользователь статистом.
        /// </summary>
        public bool IsStatistician()
        {
            return CurrentRoleName == UserRole.Statistician;
        }

        /// <summary>
        /// Мониторинг доступен администратору и статисту.
        /// </summary>
        public bool CanManageMonitoring()
        {
            return IsAdmin() || IsStatistician();
        }

        /// <summary>
        /// Логи безопасности доступны только администратору.
        /// </summary>
        public bool CanViewSecurityLogs()
        {
            return IsAdmin();
        }

        #endregion
    }
}
