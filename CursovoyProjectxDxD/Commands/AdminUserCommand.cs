using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Группа админских команд управления пользователями.
    public sealed class AdminUserCommand : ICommand
    {
        // Команда ищется по первым двум словам: admin user.
        public string Name => "admin user";

        // В справке перечисляем все действия этой группы.
        public string Description => "Админ: create/delete/block/unblock/info/list/nt";

        // Выполняет выбранное действие над учётной записью.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем текущую сессию для проверки роли.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();
            // Админские команды доступны только роли admin.
            if (!sessionService.IsAdmin())
            {
                return CommandResult.Fail("Команда доступна только администратору.");
            }

            // После admin user должно быть действие.
            if (context.Args == null || context.Args.Length < 3)
            {
                return CommandResult.Fail(GetUsage());
            }

            // Действие находится третьим словом.
            string action = context.Args[2].ToLowerInvariant();

            // Выбираем обработчик действия.
            if (action == "create")
                return await CreateUserAsync(context, cancellationToken);

            if (action == "delete")
                return await DeleteUserAsync(context, sessionService, cancellationToken);

            if (action == "block")
                return await SetBlockedAsync(context, sessionService, true, cancellationToken);

            if (action == "unblock")
                return await SetBlockedAsync(context, sessionService, false, cancellationToken);

            if (action == "info")
                return await ShowUserInfoAsync(context, cancellationToken);

            if (action == "list")
                return await ListUsersAsync(context, cancellationToken);

            if (action == "nt")
                return await ListUserNotesAsync(context, cancellationToken);

            // Неизвестное действие выводит справку по группе.
            return CommandResult.Fail(GetUsage());
        }

        // Создаёт нового пользователя.
        private static async Task<CommandResult> CreateUserAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис: роль можно не указывать, тогда будет user.
            if (context.Args.Length < 5 || context.Args.Length > 6)
            {
                return CommandResult.Fail("Использование: admin user create <login> <password> [user|admin]");
            }

            // Получаем AuthService.
            AuthService authService = context.GetRequiredService<AuthService>();

            // Разбираем аргументы.
            string login = context.Args[3];
            string password = context.Args[4];
            string roleName = context.Args.Length >= 6 ? context.Args[5] : UserRole.User;

            // Создаём пользователя через сервис авторизации.
            AuthResult result = await authService.CreateUserAsync(login, password, roleName, cancellationToken);
            await WriteAdminLogAsync(context, result.IsSuccess ? "admin_user_create" : "admin_user_create_failed", result.Message, login, cancellationToken);

            // Возвращаем результат как ответ команды.
            return result.IsSuccess ? CommandResult.Ok(result.Message) : CommandResult.Fail(result.Message);
        }

        // Удаляет пользователя.
        private static async Task<CommandResult> DeleteUserAsync(CommandContext context, AuthSessionService sessionService, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length != 4)
            {
                return CommandResult.Fail("Использование: admin user delete <login>");
            }

            // Админ не должен удалить сам себя во время текущей сессии.
            string login = context.Args[3];
            if (login == sessionService.CurrentLogin)
            {
                return CommandResult.Fail("Нельзя удалить текущую учётную запись администратора.");
            }

            // Удаляем пользователя.
            AuthService authService = context.GetRequiredService<AuthService>();
            bool deleted = await authService.DeleteUserAsync(login, cancellationToken);
            await WriteAdminLogAsync(context, deleted ? "admin_user_delete" : "admin_user_delete_failed", deleted ? "Пользователь удалён." : "Пользователь не найден.", login, cancellationToken);
            return deleted
                ? CommandResult.Ok("Пользователь " + login + " удалён.")
                : CommandResult.Fail("Пользователь " + login + " не найден.");
        }

        // Блокирует или разблокирует пользователя.
        private static async Task<CommandResult> SetBlockedAsync(CommandContext context, AuthSessionService sessionService, bool isBlocked, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length != 4)
            {
                return CommandResult.Fail(isBlocked
                    ? "Использование: admin user block <login>"
                    : "Использование: admin user unblock <login>");
            }

            // Админ не должен заблокировать сам себя во время текущей сессии.
            string login = context.Args[3];
            if (isBlocked && login == sessionService.CurrentLogin)
            {
                return CommandResult.Fail("Нельзя заблокировать текущую учётную запись администратора.");
            }

            // Меняем флаг блокировки.
            AuthService authService = context.GetRequiredService<AuthService>();
            bool updated = await authService.SetUserBlockedAsync(login, isBlocked, cancellationToken);
            await WriteAdminLogAsync(context, updated ? (isBlocked ? "admin_user_block" : "admin_user_unblock") : (isBlocked ? "admin_user_block_failed" : "admin_user_unblock_failed"), updated ? "Статус блокировки пользователя изменён." : "Пользователь не найден.", login, cancellationToken);
            if (!updated)
            {
                return CommandResult.Fail("Пользователь " + login + " не найден.");
            }

            return CommandResult.Ok(isBlocked
                ? "Пользователь " + login + " заблокирован."
                : "Пользователь " + login + " разблокирован.");
        }

        // Показывает информацию о пользователе.
        private static async Task<CommandResult> ShowUserInfoAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length != 4)
            {
                return CommandResult.Fail("Использование: admin user info <login>");
            }

            // Получаем пользователя.
            AuthService authService = context.GetRequiredService<AuthService>();
            UserAccount user = await authService.GetUserInfoAsync(context.Args[3], cancellationToken);
            await WriteAdminLogAsync(context, user == null ? "admin_user_info_failed" : "admin_user_info", user == null ? "Пользователь не найден." : "Просмотрена информация о пользователе.", context.Args[3], cancellationToken);
            if (user == null)
            {
                return CommandResult.Fail("Пользователь не найден.");
            }

            // Возвращаем карточку пользователя.
            return CommandResult.Ok(FormatUser(user));
        }

        // Показывает список всех пользователей.
        private static async Task<CommandResult> ListUsersAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // У команды list нет дополнительных аргументов.
            if (context.Args.Length != 3)
            {
                return CommandResult.Fail("Использование: admin user list");
            }

            // Получаем список пользователей.
            AuthService authService = context.GetRequiredService<AuthService>();
            IReadOnlyList<UserAccount> users = await authService.ListUsersAsync(cancellationToken);
            await WriteAdminLogAsync(context, "admin_user_list", "Просмотрен список пользователей.", null, cancellationToken);
            if (users.Count == 0)
            {
                return CommandResult.Ok("Пользователи не найдены.");
            }

            // Формируем таблицу в текстовом виде.
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Пользователи:");
            builder.AppendLine();

            foreach (UserAccount user in users)
            {
                builder.AppendLine("#" + user.Id + " | " + user.Login + " | " +
                    UserRole.GetDisplayName(user.RoleName) + " | " +
                    (user.IsBlocked ? "заблокирован" : "активен"));
            }

            return CommandResult.Ok(builder.ToString().TrimEnd());
        }

        // Показывает заметки указанного пользователя.
        private static async Task<CommandResult> ListUserNotesAsync(CommandContext context, CancellationToken cancellationToken)
        {
            // Проверяем синтаксис.
            if (context.Args.Length != 4)
            {
                return CommandResult.Fail("Использование: admin user nt <login>");
            }

            // Получаем заметки пользователя.
            NoteService noteService = context.GetRequiredService<NoteService>();
            IReadOnlyList<NoteRecord> notes = await noteService.ListNotesByLoginForAdminAsync(context.Args[3], cancellationToken);
            await WriteAdminLogAsync(context, "admin_user_notes", "Просмотрены заметки пользователя.", context.Args[3], cancellationToken);
            if (notes.Count == 0)
            {
                return CommandResult.Ok("У пользователя " + context.Args[3] + " нет заметок.");
            }

            // Формируем список заметок.
            return CommandResult.Ok(FormatNotes(notes));
        }

        // Форматирует карточку пользователя.
        private static string FormatUser(UserAccount user)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Пользователь #" + user.Id);
            builder.AppendLine("Логин: " + user.Login);
            builder.AppendLine("Роль: " + UserRole.GetDisplayName(user.RoleName) + " (" + user.RoleName + ")");
            builder.AppendLine("Статус: " + (user.IsBlocked ? "заблокирован" : "активен"));
            builder.AppendLine("Создан: " + user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            return builder.ToString().TrimEnd();
        }

        // Форматирует список заметок.
        private static string FormatNotes(IReadOnlyList<NoteRecord> notes)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Заметки пользователя " + notes[0].AuthorLogin + ":");
            builder.AppendLine();

            foreach (NoteRecord note in notes)
            {
                builder.AppendLine("#" + note.Id + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine(note.Text);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        // Возвращает справку по группе admin user.
        private static string GetUsage()
        {
            return "Использование:\n" +
                   "admin user create <login> <password> [user|admin]\n" +
                   "admin user delete <login>\n" +
                   "admin user block <login>\n" +
                   "admin user unblock <login>\n" +
                   "admin user info <login>\n" +
                   "admin user list\n" +
                   "admin user nt <login>";
        }

        // Записывает действие администратора в журнал безопасности.
        private static async Task WriteAdminLogAsync(CommandContext context, string eventType, string message, string target, CancellationToken cancellationToken)
        {
            // Получаем сервис журнала безопасности.
            SecurityLogService securityLogService = context.GetRequiredService<SecurityLogService>();
            // Пишем событие от имени текущего администратора.
            await securityLogService.WriteCurrentUserEventAsync(eventType, message, target, cancellationToken);
        }
    }
}
