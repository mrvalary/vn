namespace CursovoyProjectxDxD.Models
{
    public sealed class AuthResult
    {
        public bool IsSuccess { get; }

        public string Message { get; }

        public int? UserId { get; }

        public string Login { get; }

        public string RoleName { get; }

        public string RoleConnectionString { get; }

        private AuthResult(bool isSuccess, string message, int? userId, string login, string roleName, string roleConnectionString)
        {
            IsSuccess = isSuccess;
            Message = message;
            UserId = userId;
            Login = login;
            RoleName = roleName;
            RoleConnectionString = roleConnectionString;
        }

        public static AuthResult Success(string message)
        {
            return new AuthResult(true, message, null, null, null, null);
        }

        public static AuthResult Success(string message, int userId, string login, string roleName)
        {
            return new AuthResult(true, message, userId, login, roleName, null);
        }

        public static AuthResult Success(string message, int userId, string login, string roleName, string roleConnectionString)
        {
            return new AuthResult(true, message, userId, login, roleName, roleConnectionString);
        }

        public static AuthResult Failure(string message)
        {
            return new AuthResult(false, message, null, null, null, null);
        }
    }
}
