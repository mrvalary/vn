using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Core;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    public sealed class InstallerOrchestrator
    {
        private readonly GitHubReleaseService _gitHubReleaseService;
        private readonly ReleaseDownloadService _releaseDownloadService;
        private readonly ArchiveExtractorService _archiveExtractorService;
        private readonly ProcessWaitService _processWaitService;
        private readonly FileDeploymentService _fileDeploymentService;
        private readonly AppStarterService _appStarterService;
        private readonly FileLogger _logger;

        public InstallerOrchestrator(
            GitHubReleaseService gitHubReleaseService,
            ReleaseDownloadService releaseDownloadService,
            ArchiveExtractorService archiveExtractorService,
            ProcessWaitService processWaitService,
            FileDeploymentService fileDeploymentService,
            AppStarterService appStarterService,
            FileLogger logger)
        {
            _gitHubReleaseService = gitHubReleaseService;
            _releaseDownloadService = releaseDownloadService;
            _archiveExtractorService = archiveExtractorService;
            _processWaitService = processWaitService;
            _fileDeploymentService = fileDeploymentService;
            _appStarterService = appStarterService;
            _logger = logger;
        }

        public async Task ExecuteAsync(InstallerArguments installerArguments, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Установщик сам получает релиз и не зависит от состояния vn-app.
            AppUpdateInfo updateInfo = await _gitHubReleaseService.GetLatestReleaseAsync(cancellationToken);
            _logger.Info("Latest release version: " + updateInfo.LatestVersion);

            string archivePath = await _releaseDownloadService.DownloadAsync(updateInfo, cancellationToken);
            string extractedDirectory = _archiveExtractorService.Extract(archivePath, updateInfo.LatestVersion);

            // Сначала освобождаем файлы приложения, затем заменяем их и запускаем vn-app заново.
            _processWaitService.WaitForExit(installerArguments.AppProcessId);
            _fileDeploymentService.Deploy(extractedDirectory, installerArguments.TargetDirectory);
            _appStarterService.Start(installerArguments.AppExePath);
        }
    }
}
