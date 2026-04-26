using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD.Commands
{
    // Команда выводит подробную карту CLI по доступным пользователю разделам.
    public sealed class HelpCommand : ICommand
    {
        #region Metadata

        public HelpCommand(CommandRegistry registry)
        {
            // Реестр приходит из старой точки подключения команды и оставлен для совместимости конструктора.
        }

        public string Name => "help";

        public string Description => "Вывод карты всех доступных команд";

        #endregion

        #region Execute

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

        private static void AppendCommonCommands(StringBuilder builder)
        {
            builder.AppendLine("Основные:");
            builder.AppendLine("  help                     - показать эту справку");
            builder.AppendLine("  version                  - показать версию приложения");
            builder.AppendLine("  exit                     - выйти из приложения");
            builder.AppendLine("  auth logout              - выйти из текущей учетной записи");
            builder.AppendLine();
        }

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

        private static void AppendWatchCommands(StringBuilder builder)
        {
            builder.AppendLine("Мониторинг:");
            builder.AppendLine("  watch list               - список устройств мониторинга");
            builder.AppendLine("  watch show <key> [count] - показать CPU/RAM/HDD устройства");
            builder.AppendLine("  watch add <key> <name> [address] [description] - добавить или обновить устройство");
            builder.AppendLine("  watch del <key>          - удалить устройство из мониторинга");
            builder.AppendLine();
        }

        private static void AppendUpdateCommands(StringBuilder builder)
        {
            builder.AppendLine("Обновления:");
            builder.AppendLine("  update check             - проверить наличие новой версии");
            builder.AppendLine("  update apply             - запустить установщик обновления");
            builder.AppendLine();
        }

        private static void AppendSecurityCommands(StringBuilder builder)
        {
            builder.AppendLine("Безопасность:");
            builder.AppendLine("  sec logs [count]         - показать журнал безопасности");
            builder.AppendLine();
        }

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
