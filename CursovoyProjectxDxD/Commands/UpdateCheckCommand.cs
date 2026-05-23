using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда проверки доступности новой версии.
    /// </summary>
    public sealed class UpdateCheckCommand : ICommand
    {
        /// <summary>
        /// Имя команды.
        /// </summary>
        public string Name => "update check";
        /// <summary>
        /// Описание команды.
        /// </summary>
        public string Description => "Проверка наличия обновлений";

        /// <summary>
        /// Команда только выводит информацию о релизе.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // Получаем сервис чтения релизов.
            var service = context.GetRequiredService<GitHubReleaseService>();
            // Получаем сведения о текущей и последней версиях.
            var info = await service.CheckForUpdateAsync(cancellationToken);

            // Если обновления нет, выводим только текущую версию.
            if (!info.IsAvailable)
                return CommandResult.Ok(FormatNoUpdateMessage(info));

            // Если обновление найдено, выводим полную информацию.
            return CommandResult.Ok(FormatUpdateMessage(info));
        }

        #region Formatting

        /// <summary>
        /// Формирует сообщение, когда новая версия не найдена.
        /// </summary>
        /// <param name="info">Информация о последнем релизе.</param>
        /// <returns>Готовое сообщение для консоли.</returns>
        private static string FormatNoUpdateMessage(AppUpdateInfo info)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Обновлений нет. Текущая версия: " + info.CurrentVersion);
            AppendReleaseInfo(builder, info);
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Формирует подробное сообщение о найденном обновлении.
        /// </summary>
        /// <param name="info">Информация о найденном обновлении.</param>
        /// <returns>Готовое сообщение для консоли.</returns>
        private static string FormatUpdateMessage(AppUpdateInfo info)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Доступно обновление: " + info.LatestVersion);
            builder.AppendLine("Текущая версия: " + info.CurrentVersion);
            AppendReleaseInfo(builder, info);
            builder.AppendLine("Файл: " + info.AssetName);
            builder.AppendLine("Ссылка: " + info.DownloadUrl);
            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Добавляет название и описание релиза, если GitHub вернул эти поля.
        /// </summary>
        /// <param name="builder">Буфер сообщения.</param>
        /// <param name="info">Информация о релизе.</param>
        private static void AppendReleaseInfo(StringBuilder builder, AppUpdateInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.ReleaseName))
            {
                builder.AppendLine("Релиз: " + info.ReleaseName);
            }

            if (!string.IsNullOrWhiteSpace(info.ReleaseNotes))
            {
                builder.AppendLine("Описание релиза:");
                builder.AppendLine(info.ReleaseNotes.Trim());
            }
        }

        #endregion
    }
}
