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
    // Команда запуска обновления через внешний установщик.
    public sealed class UpdateApplyCommand : ICommand
    {
        // Имя команды в CLI.
        public string Name
        {
            get { return "update apply"; }
        }

        // Описание команды для help.
        public string Description
        {
            get { return "Запуск внешнего установщика обновления"; }
        }

        // Проверяет наличие новой версии и передаёт управление vn-installer.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис проверки релизов.
            GitHubReleaseService releaseService = context.GetRequiredService<GitHubReleaseService>();
            // Получаем сервис запуска установщика.
            InstallerLauncherService launcherService = context.GetRequiredService<InstallerLauncherService>();
            // Проверяем, доступно ли обновление.
            AppUpdateInfo info = await releaseService.CheckForUpdateAsync(cancellationToken);

            // Если новой версии нет, ничего не устанавливаем.
            if (!info.IsAvailable)
            {
                return CommandResult.Ok("Обновление не требуется.");
            }

            // Находим путь к текущему exe.
            string appExePath = Process.GetCurrentProcess().MainModule.FileName;
            // Определяем каталог приложения.
            string appDirectory = Path.GetDirectoryName(appExePath);
            // Считываем PID текущего процесса.
            int currentProcessId = Process.GetCurrentProcess().Id;

            // Запускаем установщик.
            bool started = launcherService.Launch(appDirectory, appExePath, currentProcessId);
            // Если запуск не удался, возвращаем ошибку.
            if (!started)
            {
                return CommandResult.Fail("Не удалось запустить vn-installer.exe.");
            }

            // Сообщаем пользователю о передаче управления установщику.
            Console.WriteLine("Установщик обновления запущен.");
            // Предупреждаем о полном закрытии основного приложения.
            Console.WriteLine("Основное приложение будет полностью закрыто для установки обновления.");

            // Завершаем основной процесс, чтобы его файлы можно было заменить.
            Environment.Exit(0);
            // Формальный возврат нужен только из-за сигнатуры метода.
            return CommandResult.Ok("Установщик запущен.");
        }
    }
}
