using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VnInstaller.Core;
using VnInstaller.Services;

namespace VnInstaller
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
                Console.WriteLine("Критическая ошибка установщика: " + ex.Message);
                return 1;
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            InstallerArguments installerArguments;
            string parseError;

            if (!InstallerArguments.TryParse(args, out installerArguments, out parseError))
            {
                Console.WriteLine(parseError);
                return 1;
            }

            ServiceProvider serviceProvider = ConfigureServices(installerArguments);
            FileLogger logger = serviceProvider.GetRequiredService<FileLogger>();
            InstallerOrchestrator orchestrator = serviceProvider.GetRequiredService<InstallerOrchestrator>();

            try
            {
                logger.Info("VnInstaller started.");
                logger.Info("Target directory: " + installerArguments.TargetDirectory);
                logger.Info("App exe path: " + installerArguments.AppExePath);
                logger.Info("Process id: " + (installerArguments.AppProcessId.HasValue ? installerArguments.AppProcessId.Value.ToString() : "not specified"));

                await orchestrator.ExecuteAsync(installerArguments);

                logger.Info("VnInstaller finished successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error("Installer failed.", ex);
                Console.WriteLine("Ошибка установки: " + ex.Message);
                return 1;
            }
        }

        private static ServiceProvider ConfigureServices(InstallerArguments installerArguments)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<HttpClient>(provider =>
            {
                return new HttpClient();
            });

            services.AddSingleton(new FileLogger(installerArguments.GetLogFilePath()));
            services.AddSingleton<GitHubReleaseService>();
            services.AddSingleton<ReleaseDownloadService>();
            services.AddSingleton<ArchiveExtractorService>();
            services.AddSingleton<ProcessWaitService>();
            services.AddSingleton<FileDeploymentService>();
            services.AddSingleton<AppStarterService>();
            services.AddSingleton<InstallerOrchestrator>();

            return services.BuildServiceProvider();
        }
    }
}
