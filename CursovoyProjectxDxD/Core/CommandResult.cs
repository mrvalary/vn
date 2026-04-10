namespace CursovoyProjectxDxD.Core
{
    // Унифицированный результат выполнения команды.
    public sealed class CommandResult
    {
        // Показывает, завершилась ли команда успешно.
        public bool Success { get; }
        // Сообщение, которое будет выведено в консоль.
        public string Message { get; }

        // Конструктор скрыт, чтобы использовать только фабрики Ok и Fail.
        private CommandResult(bool success, string message)
        {
            // Сохраняем флаг успеха.
            Success = success;
            // Сохраняем текст сообщения.
            Message = message;
        }

        // Создаёт успешный результат.
        public static CommandResult Ok(string message)
        {
            return new CommandResult(true, message);
        }

        // Создаёт неуспешный результат.
        public static CommandResult Fail(string message)
        {
            return new CommandResult(false, message);
        }
    }
}
