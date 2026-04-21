using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда вывода списка доступных CLI-команд.
    public sealed class HelpCommand : ICommand
    {
        // Реестр команд нужен для динамической справки.
        private readonly CommandRegistry _registry;

        // Получаем реестр через конструктор.
        public HelpCommand(CommandRegistry registry)
        {
            // Сохраняем реестр команд.
            _registry = registry;
        }

        // Имя команды.
        public string Name => "help";

        // Описание команды.
        public string Description => "Вывод карты всех доступных команд";

        // Формирует справку на основе текущего реестра команд.
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем текущую сессию, чтобы скрыть admin-команды от неадминов.
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();

            // StringBuilder удобен для многострочного текста.
            StringBuilder builder = new StringBuilder();

            // Заголовок справки.
            builder.AppendLine("Доступные команды:");
            builder.AppendLine();

            // Сортируем команды по имени, чтобы help был стабильным и читаемым.
            foreach (ICommand command in _registry.GetAll().Values.OrderBy(command => command.Name))
            {
                // Команды admin видит только пользователь с ролью admin.
                if (command.Name.StartsWith("admin ") && !sessionService.IsAdmin())
                {
                    continue;
                }

                // Команды security видит только админ или статист.
                if (command.Name.StartsWith("sec ") && !sessionService.CanViewSecurityLogs())
                {
                    continue;
                }

                // Команды stat видит только админ или статист.
                if (command.Name.StartsWith("stat") && !sessionService.CanViewStatistics())
                {
                    continue;
                }

                // Каждая команда выводится одной строкой.
                builder.AppendLine(command.Name + " - " + command.Description);
            }

            // Возвращаем готовую справку.
            return Task.FromResult(CommandResult.Ok(builder.ToString()));
        }
    }
}
