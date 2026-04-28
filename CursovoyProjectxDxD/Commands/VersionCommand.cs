using CursovoyProjectxDxD.Core;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    // Команда показа версии текущей сборки.
    public sealed class VersionCommand : ICommand
    {
        // Имя команды.
        public string Name => "version";
        // Описание команды.
        public string Description => "Показ версии клиента";

        // Возвращает текущую версию приложения.
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // Получаем строку версии через единый провайдер.
            string displayVersion = AppVersionProvider.GetCurrentVersion();
            // Возвращаем результат в стандартном формате.
            return Task.FromResult(CommandResult.Ok("vn version " + displayVersion));
        }
    }
}
