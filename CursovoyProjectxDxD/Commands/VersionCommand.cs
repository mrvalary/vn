using CursovoyProjectxDxD.Core;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда показа версии текущей сборки.
    /// </summary>
    public sealed class VersionCommand : ICommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string Name => "version";
        /// <summary>
        /// Описание команды.
        /// </summary>
        public string Description => "Показ версии клиента";

        /// <summary>
        /// Возвращает текущую версию приложения.
        /// </summary>
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // Получаем строку версии через единый провайдер.
            string displayVersion = AppVersionProvider.GetCurrentVersion();
            // Возвращаем результат в стандартном формате.
            return Task.FromResult(CommandResult.Ok("Версия vn: " + displayVersion));
        }
    }
}
