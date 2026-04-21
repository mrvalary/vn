namespace CursovoyProjectxDxD.Models
{
    // Результат регистрации или входа пользователя.
    public sealed class AuthResult
    {
        // Признак успешного выполнения операции.
        public bool IsSuccess { get; }

        // Текстовое сообщение для консоли.
        public string Message { get; }

        // Идентификатор пользователя в таблице users.
        public int? UserId { get; }

        // Логин пользователя, успешно прошедшего аутентификацию.
        public string Login { get; }

        // Приватный конструктор ограничивает создание результата фабричными методами.
        private AuthResult(bool isSuccess, string message, int? userId, string login)
        {
            // Сохраняем итог операции.
            IsSuccess = isSuccess;
            // Сохраняем сообщение для пользователя.
            Message = message;
            // Сохраняем идентификатор пользователя, если он известен.
            UserId = userId;
            // Сохраняем логин пользователя, если он известен.
            Login = login;
        }

        // Создаёт успешный результат без пользовательских данных.
        public static AuthResult Success(string message)
        {
            // Возвращаем простой успешный ответ.
            return new AuthResult(true, message, null, null);
        }

        // Создаёт успешный результат входа с данными пользователя.
        public static AuthResult Success(string message, int userId, string login)
        {
            // Возвращаем успешный ответ с идентификатором и логином пользователя.
            return new AuthResult(true, message, userId, login);
        }

        // Создаёт неуспешный результат.
        public static AuthResult Failure(string message)
        {
            // Возвращаем ответ с ошибкой.
            return new AuthResult(false, message, null, null);
        }
    }
}
