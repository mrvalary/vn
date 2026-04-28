using System;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Core;

namespace CursovoyProjectxDxD.Commands
{
    /// <summary>
    /// Команда очищает текущее окно консоли.
    /// </summary>
    public sealed class ClearCommand : ICommand
    {
        #region Metadata

        /// <summary>
        /// Имя команды в консоли.
        /// </summary>
        public string Name => "clear";

        /// <summary>
        /// Описание команды для справки.
        /// </summary>
        public string Description => "Очистка консоли";

        #endregion

        #region Execute

        /// <summary>
        /// Очищает консоль и возвращает пустой результат.
        /// </summary>
        /// <param name="context">Контекст выполнения команды.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Пустой успешный результат.</returns>
        public Task<CommandResult> ExecuteAsync(CommandContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.Clear();
            return Task.FromResult(CommandResult.Ok(string.Empty));
        }

        #endregion
    }
}
