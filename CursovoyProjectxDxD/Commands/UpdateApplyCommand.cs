using System;
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

        // Проверяет наличие новой версии и запускает vn-installer без аргументов.
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сервис проверки релизов.
            GitHubReleaseService releaseService = context.GetRequiredService<GitHubReleaseService>();
            // Получаем сервис запуска установщика.
            InstallerLauncherService launcherService = context.GetRequiredService<InstallerLauncherService>();
            // Проверяем, доступно ли обновление.
            AppUpdateInfo info = await releaseService.CheckForUpdateAsync(cancellationToken);

            // Если новой версии нет, ничего не делаем.
            if (!info.IsAvailable)
            {
                return CommandResult.Ok("Обновление не требуется.");
            }

            // Запускаем установщик в единственном режиме без аргументов.
            bool started = launcherService.Launch();
            // Если старт не удался, возвращаем ошибку.
            if (!started)
            {
                return CommandResult.Fail("Не удалось запустить vn-installer.exe.");
            }

            // Сообщаем пользователю о передаче управления установщику.
            Console.WriteLine("Установщик обновления запущен.");
            // Предупреждаем о полном закрытии основного приложения.
            Console.WriteLine("Основное приложение будет полностью закрыто для установки обновления.");

            // Полностью завершаем основной процесс, чтобы его файлы можно было заменить.
            Environment.Exit(0);
            // Формальный возврат нужен только из-за сигнатуры метода.
            return CommandResult.Ok("Установщик запущен.");
        }
    }
}
