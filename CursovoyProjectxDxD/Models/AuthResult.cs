namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Результат операции авторизации, регистрации или администрирования пользователя.
    /// </summary>
    public sealed class AuthResult
    {
        #region Properties

        /// <summary>
        /// Признак успешного выполнения операции.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Сообщение для вывода пользователю.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Идентификатор пользователя после успешной авторизации.
        /// </summary>
        public int? UserId { get; }

        /// <summary>
        /// Логин пользователя после успешной авторизации.
        /// </summary>
        public string Login { get; }

        /// <summary>
        /// Роль пользователя после успешной авторизации.
        /// </summary>
        public string RoleName { get; }

        /// <summary>
        /// Строка подключения роли пользователя, полученная из БД.
        /// </summary>
        public string RoleConnectionString { get; }

        #endregion

        #region Construction

        /// <summary>
        /// Создает результат операции авторизации.
        /// </summary>
        private AuthResult(bool isSuccess, string message, int? userId, string login, string roleName, string roleConnectionString)
        {
            IsSuccess = isSuccess;
            Message = message;
            UserId = userId;
            Login = login;
            RoleName = roleName;
            RoleConnectionString = roleConnectionString;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создает успешный результат без пользовательской сессии.
        /// </summary>
        /// <param name="message">Сообщение для пользователя.</param>
        /// <returns>Успешный результат операции.</returns>
        public static AuthResult Success(string message)
        {
            return new AuthResult(true, message, null, null, null, null);
        }

        /// <summary>
        /// Создает успешный результат авторизации без строки подключения роли.
        /// </summary>
        /// <param name="message">Сообщение для пользователя.</param>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="login">Логин пользователя.</param>
        /// <param name="roleName">Роль пользователя.</param>
        /// <returns>Успешный результат авторизации.</returns>
        public static AuthResult Success(string message, int userId, string login, string roleName)
        {
            return new AuthResult(true, message, userId, login, roleName, null);
        }

        /// <summary>
        /// Создает успешный результат авторизации со строкой подключения роли.
        /// </summary>
        /// <param name="message">Сообщение для пользователя.</param>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="login">Логин пользователя.</param>
        /// <param name="roleName">Роль пользователя.</param>
        /// <param name="roleConnectionString">Строка подключения роли пользователя.</param>
        /// <returns>Успешный результат авторизации.</returns>
        public static AuthResult Success(string message, int userId, string login, string roleName, string roleConnectionString)
        {
            return new AuthResult(true, message, userId, login, roleName, roleConnectionString);
        }

        /// <summary>
        /// Создает неуспешный результат операции.
        /// </summary>
        /// <param name="message">Причина ошибки для пользователя.</param>
        /// <returns>Неуспешный результат операции.</returns>
        public static AuthResult Failure(string message)
        {
            return new AuthResult(false, message, null, null, null, null);
        }

        #endregion
    }
}
