namespace CursovoyProjectxDxD.Models
{
    // Единое место, где приложение хранит системные названия ролей.
    public static class UserRole
    {
        #region Role Names

        // Обычный пользователь приложения: работа со своими заметками.
        public const string User = "user";

        // Администратор: управление пользователями, заметками, логами и мониторингом.
        public const string Admin = "admin";

        // Статист: просмотр и редактирование устройств мониторинга.
        public const string Statistician = "statistician";

        #endregion

        #region Display Names

        // Возвращает русское название роли для вывода в консоль.
        public static string GetDisplayName(string roleName)
        {
            if (roleName == Admin)
            {
                return "Админ";
            }

            if (roleName == Statistician)
            {
                return "Статист";
            }

            return "Пользователь";
        }

        #endregion
    }
}
