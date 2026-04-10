using CursovoyProjectxDxD.Core;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Commands
{
    // Команда вывода списка всех CLI-команд.
    public sealed class HelpCommand : ICommand
    {
        // Реестр команд нужен для динамического построения справки.
        private readonly CommandRegistry _registry;

        // Получаем реестр через конструктор.
        public HelpCommand(CommandRegistry registry)
        {
            _registry = registry;
        }

        // Имя команды.
        public string Name => "help";
        // Краткое описание команды.
        public string Description => "Вывод карты всех команд";

        // Формирует справку на основе текущего содержимого реестра.
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default)
        {
            // StringBuilder используется для сборки многострочного текста.
            var sb = new StringBuilder();
            // Пишем заголовок справки.
            sb.AppendLine("Доступные команды:");
            // Добавляем пустую строку для читаемости.
            sb.AppendLine();

            // Берём все команды и сортируем их по имени.
            foreach (var command in _registry.GetAll().Values.OrderBy(c => c.Name))
            {
                // Каждая команда печатается одной строкой.
                sb.AppendLine(command.Name + " - " + command.Description);
            }

            // Возвращаем успешный результат.
            return Task.FromResult(CommandResult.Ok(sb.ToString()));
        }
    }
}
