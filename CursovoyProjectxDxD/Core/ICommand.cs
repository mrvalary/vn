using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Core
{
    // Контракт любой команды CLI.
    public interface ICommand
    {
        // Имя команды, по которому она ищется в реестре.
        string Name { get; }
        // Описание команды для help.
        string Description { get; }
        // Метод выполнения команды.
        Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken));
    }
}
