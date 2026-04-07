using CursovoyProjectxDxD.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Services;
namespace CursovoyProjectxDxD.Commands
{
    public sealed class UpdateApplyCommand : ICommand
    {
        public string Name => "update apply";
        public string Description => "Установка обновления клиента";

        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            var service = context.GetRequiredService<UpdaterService>();
            var result = await service.ApplyUpdateAsync(cancellationToken);
            return result
                ? CommandResult.Ok("Обновление загружено. Запущен процесс обновления.")
                : CommandResult.Fail("Не удалось применить обновление.");
        }
    }
}
