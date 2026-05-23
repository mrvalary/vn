using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CursovoyProjectxDxD.Core
{
    /// <summary>
    /// Реестр всех консольных команд.
    /// </summary>
    public sealed class CommandRegistry
    {
        // Словарь хранит команду по её имени.
        private readonly Dictionary<string, ICommand> _commands =
            new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Добавляет новую команду или заменяет существующую.
        /// </summary>
        public void Register(ICommand command)
        {
            _commands[command.Name] = command;
        }

        /// <summary>
        /// Пытается найти команду по имени.
        /// </summary>
        public bool TryGet(string name, out ICommand command)
        {
            return _commands.TryGetValue(name, out command);
        }

        // Возвращает только read-only представление словаря.
        public IReadOnlyDictionary<string, ICommand> GetAll()
        {
            return new ReadOnlyDictionary<string, ICommand>(_commands);
        }
    }
}
