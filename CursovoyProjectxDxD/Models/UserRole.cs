namespace CursovoyProjectxDxD.Models
{
    // Единое место, где приложение хранит системные названия ролей.
    public static class UserRole
    {
        // Обычный пользователь приложения: работа со своими заметками.
        public const string User = "user";

        // Администратор: управление пользователями, заметками и просмотр логов безопасности.
        public const string Admin = "admin";

        // Статист: просмотр статистики и логов безопасности без админского изменения данных.
        public const string Statistician = "statistician";

        // Возвращает русское название роли для вывода в консоль.
        public static string GetDisplayName(string roleName)
        {
            // Админа показываем понятным русским словом.
            if (roleName == Admin)
            {
                return "Админ";
            }

            // Статиста показываем отдельной ролью.
            if (roleName == Statistician)
            {
                return "Статист";
            }

            // Любая неизвестная или обычная роль отображается как пользователь.
            return "Пользователь";
        }
    }
}
