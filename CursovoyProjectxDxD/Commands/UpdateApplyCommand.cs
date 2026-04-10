using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
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
            get { return "Запуск внешнего установщика обновления"; }
        }

        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            GitHubReleaseService releaseService = context.GetRequiredService<GitHubReleaseService>();
            InstallerLauncherService launcherService = context.GetRequiredService<InstallerLauncherService>();
            AppUpdateInfo info = await releaseService.CheckForUpdateAsync(cancellationToken);

            if (!info.IsAvailable)
            {
                return CommandResult.Ok("Обновление не требуется.");
            }

            string appExePath = Process.GetCurrentProcess().MainModule.FileName;
            string appDirectory = Path.GetDirectoryName(appExePath);
            int currentProcessId = Process.GetCurrentProcess().Id;

            bool started = launcherService.Launch(appDirectory, appExePath, currentProcessId);
            if (!started)
            {
                return CommandResult.Fail("Не удалось запустить vn-installer.exe.");
            }

            Console.WriteLine("Установщик обновления запущен.");
            Console.WriteLine("Основное приложение будет закрыто.");

            Environment.Exit(0);
            return CommandResult.Ok("Установщик запущен.");
        }
    }
}
