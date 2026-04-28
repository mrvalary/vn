using System;

namespace CursovoyProjectxDxD.Core
{
    // Контекст выполнения отдельной команды.
    public sealed class CommandContext
    {
        // Аргументы, полученные после разбора строки ввода.
        public string[] Args { get; }
        // Провайдер сервисов приложения.
        public IServiceProvider Services { get; }

        // Создаём контекст перед выполнением команды.
        public CommandContext(string[] args, IServiceProvider services)
        {
            // Сохраняем аргументы.
            Args = args;
            // Сохраняем контейнер сервисов.
            Services = services;
        }

        // Возвращает обязательный сервис.
        public T GetRequiredService<T>()
        {
            // Ищем сервис по типу.
            object service = Services.GetService(typeof(T));
            // Если сервис не найден, сразу бросаем ошибку.
            if (service == null)
                throw new InvalidOperationException("Service " + typeof(T).Name + " is not registered.");

            // Возвращаем сервис в нужном типе.
            return (T)service;
        }
    }
}
