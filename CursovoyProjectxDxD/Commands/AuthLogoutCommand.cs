using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда выхода из текущей пользовательской сессии.
    public sealed class AuthLogoutCommand : ICommand
    {
        // Имя команды, которое вводится пользователем.
        public string Name => "auth logout";

        // Описание команды для help.
        public string Description => "Выход из текущего аккаунта: auth logout";

        // Очищает локальную сессию пользователя.
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

            // После этой команды Program вернёт пользователя в меню авторизации.
            return CommandResult.Ok("Пользователь " + login + " вышел из системы.");
        }
    }
}
