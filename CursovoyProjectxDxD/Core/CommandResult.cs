namespace CursovoyProjectxDxD.Core
{
    /// <summary>
    /// Унифицированный результат выполнения команды.
    /// </summary>
    public sealed class CommandResult
    {
        /// <summary>
        /// Показывает, завершилась ли команда успешно.
        /// </summary>
        public bool Success { get; }
        /// <summary>
        /// Сообщение, которое будет выведено в консоль.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Конструктор скрыт, чтобы использовать только фабрики Ok и Fail.
        /// </summary>
        private CommandResult(bool success, string message)
        {
            // Сохраняем флаг успеха.
            Success = success;
            // Сохраняем текст сообщения.
            Message = message;
        }

        /// <summary>
        /// Создаёт успешный результат.
        /// </summary>
        public static CommandResult Ok(string message)
        {
            return new CommandResult(true, message);
        }

        /// <summary>
        /// Создаёт неуспешный результат.
        /// </summary>
        public static CommandResult Fail(string message)
        {
            return new CommandResult(false, message);
        }
    }
}
