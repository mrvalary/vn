using System;
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

            string commandKey = BuildCommandKey(args);

            ICommand command;
            if (!registry.TryGet(commandKey, out command))
            {
                Console.WriteLine("Неизвестная команда.");
                Console.WriteLine("Используйте 'vn --help' для просмотра списка команд.");
                return 1;
            }

            CommandContext context = new CommandContext(args, serviceProvider);
            CommandResult result = await command.ExecuteAsync(context);

            Console.WriteLine(result.Message);
            return result.Success ? 0 : 1;
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
                return "--help";

            if (args.Length == 1)
            {
                if (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
                    return "--help";

                if (args[0].Equals("--version", StringComparison.OrdinalIgnoreCase))
                    return "--version";
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
    }
}