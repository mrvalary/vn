using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    // Команда проверки доступности новой версии.
    public sealed class UpdateCheckCommand : ICommand
    {
        // Имя команды.
        public string Name => "update check";
        // Описание команды.
        public string Description => "Проверка наличия обновлений";

        // Команда только выводит информацию о релизе.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // Получаем сервис чтения релизов.
            var service = context.GetRequiredService<GitHubReleaseService>();
            // Получаем сведения о текущей и последней версиях.
            var info = await service.CheckForUpdateAsync(cancellationToken);

            // Если обновления нет, выводим только текущую версию.
            if (!info.IsAvailable)
                return CommandResult.Ok("Обновлений нет. Текущая версия: " + info.CurrentVersion);

            // Если обновление найдено, выводим полную информацию.
            return CommandResult.Ok(
                "Доступно обновление: " + info.LatestVersion + "\n" +
                "Текущая версия: " + info.CurrentVersion + "\n" +
                "Файл: " + info.AssetName + "\n" +
                "Ссылка: " + info.DownloadUrl);
        }
    }
}
