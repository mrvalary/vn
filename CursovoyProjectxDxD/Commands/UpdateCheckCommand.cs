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
    public sealed class UpdateCheckCommand : ICommand
    {
        public string Name => "update check";
        public string Description => "Проверка наличия обновлений";

        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            var service = context.GetRequiredService<GitHubReleaseService>();
            var info = await service.CheckForUpdateAsync(cancellationToken);

            if (!info.IsAvailable)
                return CommandResult.Ok($"Обновлений нет. Текущая версия: {info.CurrentVersion}");

            return CommandResult.Ok(
                $"Доступно обновление: {info.LatestVersion}\n" +
                $"Текущая версия: {info.CurrentVersion}\n" +
                $"Файл: {info.AssetName}\n" +
                $"Ссылка: {info.DownloadUrl}");
        }
    }
}
