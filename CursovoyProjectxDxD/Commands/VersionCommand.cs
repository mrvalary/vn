using CursovoyProjectxDxD.Core;
using System;
using System.Reflection;
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

        // Читает версию из assembly и возвращает её пользователю.
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // Получаем объект Version из исполняемой сборки.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            // Формируем строку версии.
            string displayVersion = version != null ? version.ToString(3) : "1.0.0";
            // Возвращаем результат в стандартном формате.
            return Task.FromResult(CommandResult.Ok("vn version " + displayVersion));
        }
    }
}
