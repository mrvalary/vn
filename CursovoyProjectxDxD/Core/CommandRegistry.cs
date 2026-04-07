using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Core
{
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> _commands =
    new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        public void Register(ICommand command)
        {
            _commands[command.Name] = command;
        }

        public bool TryGet(string name, out ICommand command)
        {
            return _commands.TryGetValue(name, out command);
        }

        public IReadOnlyDictionary<string, ICommand> GetAll()
        {
            return new ReadOnlyDictionary<string, ICommand>(_commands);
        }
    }
}
