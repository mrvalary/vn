using CursovoyProjectxDxD.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    public sealed class VersionCommand : ICommand
    {
        public string Name => "--version";
        public string Description => "Показ версии клиента";

        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string displayVersion = version != null ? version.ToString(3) : "1.0.0";
            return Task.FromResult(CommandResult.Ok("vn version " + displayVersion));
        }
    }
}
