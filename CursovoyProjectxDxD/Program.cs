using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CursovoyProjectxDxD.Commands;
using CursovoyProjectxDxD.Core;
using CursovoyProjectxDxD.Services;

namespace CursovoyProjectxDxD
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка: " + ex.Message);
                return 1;
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            ServiceProvider serviceProvider = ConfigureServices();
            CommandRegistry registry = serviceProvider.GetRequiredService<CommandRegistry>();

            RegisterCommands(registry);

            Console.WriteLine("Интерактивная консоль vn запущена.");
            Console.WriteLine("Введите команду. Для справки: help");
            Console.WriteLine("Для выхода: exit");
            Console.WriteLine();

            while (true)
            {
                Console.Write("vn> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                input = input.Trim();

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Выход из программы.");
                    break;
                }

                string[] commandArgs = SplitCommandLine(input);
                string commandKey = BuildCommandKey(commandArgs);

                ICommand command;
                if (!registry.TryGet(commandKey, out command))
                {
                    Console.WriteLine("Неизвестная команда.");
                    Console.WriteLine("Введите 'help' для просмотра списка команд.");
                    Console.WriteLine();
                    continue;
                }

                try
                {
                    CommandContext context = new CommandContext(commandArgs, serviceProvider);
                    CommandResult result = await command.ExecuteAsync(context);

                    Console.WriteLine(result.Message);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка выполнения команды: " + ex.Message);
                    Console.WriteLine();
                }
            }

            return 0;
        }

        private static ServiceProvider ConfigureServices()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<CommandRegistry>();

            services.AddSingleton<HttpClient>(provider =>
            {
                HttpClient client = new HttpClient();
                return client;
            });

            services.AddSingleton<GitHubReleaseService>();
            services.AddSingleton<UpdaterService>();

            return services.BuildServiceProvider();
        }

        private static void RegisterCommands(CommandRegistry registry)
        {
            registry.Register(new HelpCommand(registry));
            registry.Register(new VersionCommand());
            registry.Register(new UpdateCheckCommand());
            registry.Register(new UpdateApplyCommand());
        }

        private static string BuildCommandKey(string[] args)
        {
            if (args == null || args.Length == 0)
                return "help";

            if (args.Length == 1)
            {
                if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                    return "help";

                if (args[0].Equals("version", StringComparison.OrdinalIgnoreCase))
                    return "version";
            }

            if (args.Length >= 2)
            {
                if (args[0].Equals("update", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("check", StringComparison.OrdinalIgnoreCase))
                {
                    return "update check";
                }

                if (args[0].Equals("update", StringComparison.OrdinalIgnoreCase) &&
                    args[1].Equals("apply", StringComparison.OrdinalIgnoreCase))
                {
                    return "update apply";
                }
            }

            return string.Join(" ", args).Trim();
        }

        private static string[] SplitCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return new string[0];

            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char ch in commandLine)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result.ToArray();
        }
    }
}