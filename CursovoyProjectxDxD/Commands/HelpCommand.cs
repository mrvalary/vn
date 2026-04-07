using CursovoyProjectxDxD.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    public sealed class HelpCommand : ICommand
    {
        private readonly CommandRegistry _registry;

        public HelpCommand(CommandRegistry registry)
        {
            _registry = registry;
        }

        public string Name => "help";
        public string Description => "Вывод карты всех команд";

        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Доступные команды:");
            sb.AppendLine();

            foreach (var command in _registry.GetAll().Values.OrderBy(c => c.Name))
            {
                sb.AppendLine($"{command.Name} - {command.Description}");
            }

            return Task.FromResult(CommandResult.Ok(sb.ToString()));
        }
    }
}
