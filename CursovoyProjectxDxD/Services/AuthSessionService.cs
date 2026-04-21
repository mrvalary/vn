namespace CursovoyProjectxDxD.Services
{
    // Хранит сведения о текущей локальной сессии пользователя.
    public sealed class AuthSessionService
    {
        // Показывает, выполнен ли вход.
        public bool IsAuthenticated
        {
            get { return CurrentUserId.HasValue && !string.IsNullOrWhiteSpace(CurrentLogin); }
        }

        // Идентификатор текущего пользователя из таблицы users.
        public int? CurrentUserId { get; private set; }

        // Логин текущего пользователя.
        public string CurrentLogin { get; private set; }

        // Открывает пользовательскую сессию после успешного входа.
        public void SignIn(int userId, string login)
        {
            // Сохраняем идентификатор вошедшего пользователя.
            CurrentUserId = userId;
            // Сохраняем имя вошедшего пользователя.
            CurrentLogin = login;
        }

        // Полностью очищает данные текущей сессии.
        public void SignOut()
        {
            // Убираем текущий идентификатор пользователя.
            CurrentUserId = null;
            // Убираем текущий логин.
            CurrentLogin = null;
        }
    }
}
