using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда выводит подробную карту CLI по доступным пользователю разделам.
    /// </summary>
    public sealed class HelpCommand : ICommand
    {
        #region Metadata

        /// <summary>
        /// Создает команду справки.
        /// </summary>
        /// <param name="registry">Реестр команд, сохраненный для совместимости старого конструктора.</param>
        public HelpCommand(CommandRegistry registry)
        {
            // Реестр приходит из старой точки подключения команды и оставлен для совместимости конструктора.
        }

        /// <summary>
        /// Имя команды в консоли.
        /// </summary>
        public string Name => "help";

        /// <summary>
        /// Краткое описание команды для реестра команд.
        /// </summary>
        public string Description => "Вывод карты всех доступных команд";

        #endregion

        #region Execute

        /// <summary>
        /// Формирует справку с учетом прав текущего пользователя.
        /// </summary>
        /// <param name="context">Контекст выполнения команды.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат команды со строкой справки.</returns>
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            AuthSessionService sessionService = context.GetRequiredService<AuthSessionService>();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Карта команд vn:");
            builder.AppendLine();

            AppendCommonCommands(builder);
            AppendNoteCommands(builder);
            AppendUpdateCommands(builder);

            if (sessionService.CanManageMonitoring())
            {
                AppendWatchCommands(builder);
            }

            if (sessionService.CanViewSecurityLogs())
            {
                AppendSecurityCommands(builder);
            }

            if (sessionService.IsAdmin())
            {
                AppendAdminCommands(builder);
            }

            return Task.FromResult(CommandResult.Ok(builder.ToString().TrimEnd()));
        }

        #endregion

        #region Help Blocks

        /// <summary>
        /// Добавляет общие команды клиента.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendCommonCommands(StringBuilder builder)
        {
            builder.AppendLine("Основные:");
            builder.AppendLine("  help                     - показать эту справку");
            builder.AppendLine("  version                  - показать версию приложения");
            builder.AppendLine("  exit                     - выйти из приложения");
            builder.AppendLine("  auth logout              - выйти из текущей учетной записи");
            builder.AppendLine();
        }

        /// <summary>
        /// Добавляет команды работы с заметками.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendNoteCommands(StringBuilder builder)
        {
            builder.AppendLine("Заметки:");
            builder.AppendLine("  nt add \"текст\"           - добавить заметку");
            builder.AppendLine("  nt list                  - список своих заметок");
            builder.AppendLine("  nt edit <id> \"текст\"     - изменить свою заметку");
            builder.AppendLine("  nt del <id>              - удалить свою заметку");
            builder.AppendLine("  nt search \"текст\"        - найти заметки по тексту");
            builder.AppendLine();
        }

        /// <summary>
        /// Добавляет команды мониторинга устройств.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendWatchCommands(StringBuilder builder)
        {
            builder.AppendLine("Мониторинг:");
            builder.AppendLine("  watch list               - список устройств мониторинга");
            builder.AppendLine("  watch show <key> [count] - показать CPU/RAM/HDD устройства");
            builder.AppendLine("  watch add <key> <name> [address] [description] - добавить или обновить устройство");
            builder.AppendLine("  watch del <key>          - удалить устройство из мониторинга");
            builder.AppendLine();
        }

        /// <summary>
        /// Добавляет команды обновления приложения.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendUpdateCommands(StringBuilder builder)
        {
            builder.AppendLine("Обновления:");
            builder.AppendLine("  update check             - проверить наличие новой версии");
            builder.AppendLine("  update apply             - запустить установщик обновления");
            builder.AppendLine();
        }

        /// <summary>
        /// Добавляет команды просмотра журнала безопасности.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendSecurityCommands(StringBuilder builder)
        {
            builder.AppendLine("Безопасность:");
            builder.AppendLine("  sec logs [count]         - показать журнал безопасности");
            builder.AppendLine();
        }

        /// <summary>
        /// Добавляет административные команды.
        /// </summary>
        /// <param name="builder">Буфер текста справки.</param>
        private static void AppendAdminCommands(StringBuilder builder)
        {
            builder.AppendLine("Администрирование:");
            builder.AppendLine("  admin user create <login> <password> [user|admin|statistician]");
            builder.AppendLine("  admin user list");
            builder.AppendLine("  admin user info <login>");
            builder.AppendLine("  admin user block <login>");
            builder.AppendLine("  admin user unblock <login>");
            builder.AppendLine("  admin user delete <login>");
            builder.AppendLine("  admin user nt <login>");
            builder.AppendLine("  admin nt view <id>");
            builder.AppendLine("  admin nt edit <id> \"новый текст\"");
        }

        #endregion
    }
}
