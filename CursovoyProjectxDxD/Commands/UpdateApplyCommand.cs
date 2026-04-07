using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    public sealed class UpdateApplyCommand : ICommand
    {
        public string Name
        {
            get { return "update apply"; }
        }

        public string Description
        {
            get { return "Установка обновления клиента"; }
        }

        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdaterService service = context.GetRequiredService<UpdaterService>();
            bool result = await service.ApplyUpdateAsync(cancellationToken);

            if (result)
            {
                return CommandResult.Ok("Обновление запущено.");
            }

            return CommandResult.Fail("Не удалось запустить обновление.");
        }
    }
}