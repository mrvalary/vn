using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Core
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default);
    }
}
