using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда выхода из текущей пользовательской сессии.
    /// </summary>
    public sealed class AuthLogoutCommand : ICommand
    {
        /// <summary>
        /// Имя команды, которое вводится пользователем.
        /// </summary>
        public string Name => "auth logout";

        /// <summary>
        /// Описание команды для help.
        /// </summary>
        public string Description => "Выход из текущего аккаунта: auth logout";

        /// <summary>
        /// Очищает локальную сессию пользователя.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // AuthSessionService хранит id, логин и роль текущего пользователя.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();

            // Если пользователь уже не авторизован, повторный выход не считается ошибкой.
            if (!sessionService.IsAuthenticated)
            {
                return CommandResult.Ok("Вы уже не авторизованы.");
            }

            // Запоминаем логин до очистки сессии.
            string login = sessionService.CurrentLogin;

            // Пишем logout в журнал безопасности до очистки сессии.
            SecurityLogService securityLogService = context.GetRequiredService<SecurityLogService>();
            await securityLogService.WriteCurrentUserEventAsync("logout", "Пользователь вышел из системы.", login, cancellationToken);

            // Полностью очищаем текущую локальную сессию.
            sessionService.SignOut();
            context.GetRequiredService<DatabaseConnectionFactory>().ClearRuntimeConnectionString();

            // После этой команды Program вернёт пользователя в меню авторизации.
            return CommandResult.Ok("Пользователь " + login + " вышел из системы.");
        }
    }
}
