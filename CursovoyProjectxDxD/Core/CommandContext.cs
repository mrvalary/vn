using System;

namespace CursovoyProjectxDxD.Core
{
    /// <summary>
    /// Контекст выполнения отдельной команды.
    /// </summary>
    public sealed class CommandContext
    {
        /// <summary>
        /// Аргументы, полученные после разбора строки ввода.
        /// </summary>
        public string[] Args { get; }
        /// <summary>
        /// Провайдер сервисов приложения.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Создаём контекст перед выполнением команды.
        /// </summary>
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
                throw new InvalidOperationException("Сервис " + typeof(T).Name + " не зарегистрирован.");

            // Возвращаем сервис в нужном типе.
            return (T)service;
        }
    }
}
