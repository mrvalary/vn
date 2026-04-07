using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CursovoyProjectxDxD.Core
{
    public sealed class CommandContext //Это объект, который передаётся в каждую команду
    {
        public string[] Args { get; }
        public IServiceProvider Services { get; }

        public CommandContext(string[] args, IServiceProvider services)
        {
            Args = args;
            Services = services;
        }

        public T GetRequiredService<T>()
        {
            object service = Services.GetService(typeof(T));
            if (service == null)
                throw new InvalidOperationException("Service " + typeof(T).Name + " is not registered.");

            return (T)service;
        }
    }
}
