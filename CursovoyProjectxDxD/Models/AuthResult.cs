namespace CursovoyProjectxDxD.Models
{
    // Результат регистрации или входа пользователя.
    public sealed class AuthResult
    {
        // Показывает, успешно ли завершилась операция.
        public bool IsSuccess { get; }

        // Сообщение, которое можно сразу вывести пользователю в консоль.
        public string Message { get; }

        // Id пользователя из таблицы users, если операция входа успешна.
        public int? UserId { get; }

        // Логин пользователя, если операция входа успешна.
        public string Login { get; }

        // Системное название роли пользователя: user, admin или statistician.
        public string RoleName { get; }

        // Закрытый конструктор заставляет создавать результат только через фабричные методы.
        private AuthResult(bool isSuccess, string message, int? userId, string login, string roleName)
        {
            // Сохраняем флаг успешности операции.
            IsSuccess = isSuccess;
            // Сохраняем текст ответа для консоли.
            Message = message;
            // Сохраняем id пользователя, если он есть.
            UserId = userId;
            // Сохраняем логин пользователя, если он есть.
            Login = login;
            // Сохраняем роль пользователя, если она есть.
            RoleName = roleName;
        }

        // Создаёт успешный результат без пользовательских данных, например после регистрации.
        public static AuthResult Success(string message)
        {
            // Для регистрации сессия ещё не открывается, поэтому id, login и role не нужны.
            return new AuthResult(true, message, null, null, null);
        }

        // Создаёт успешный результат входа с данными пользователя и его ролью.
        public static AuthResult Success(string message, int userId, string login, string roleName)
        {
            // Эти данные затем попадут в AuthSessionService.
            return new AuthResult(true, message, userId, login, roleName);
        }

        // Создаёт неуспешный результат операции.
        public static AuthResult Failure(string message)
        {
            // При ошибке пользовательские данные не заполняются.
            return new AuthResult(false, message, null, null, null);
        }
    }
}
