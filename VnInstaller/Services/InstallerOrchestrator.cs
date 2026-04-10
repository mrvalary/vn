using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Core;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    // Координатор полного сценария установки или обновления.
    public sealed class InstallerOrchestrator
    {
        // Сервис чтения информации о релизе.
        private readonly GitHubReleaseService _gitHubReleaseService;
        // Сервис скачивания архива.
        private readonly ReleaseDownloadService _releaseDownloadService;
        // Сервис распаковки архива.
        private readonly ArchiveExtractorService _archiveExtractorService;
        // Сервис ожидания завершения основного процесса.
        private readonly ProcessWaitService _processWaitService;
        // Сервис копирования файлов.
        private readonly FileDeploymentService _fileDeploymentService;
        // Сервис запуска приложения после установки.
        private readonly AppStarterService _appStarterService;
        // Логгер сценария установки.
        private readonly FileLogger _logger;

        // Все зависимости получаем через конструктор.
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

        // Выполняет полный сценарий установки или обновления.
        public async Task<AppUpdateInfo> ExecuteAsync(InstallerArguments installerArguments, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Получаем сведения о последнем релизе.
            AppUpdateInfo updateInfo = await _gitHubReleaseService.GetLatestReleaseAsync(cancellationToken);
            // Пишем версию релиза в лог.
            _logger.Info("Latest release version: " + updateInfo.LatestVersion);

            // Скачиваем zip-архив релиза.
            string archivePath = await _releaseDownloadService.DownloadAsync(updateInfo, cancellationToken);
            // Распаковываем архив во временную папку.
            string extractedDirectory = _archiveExtractorService.Extract(archivePath, updateInfo.LatestVersion);

            // При обновлении ждём завершения основного приложения.
            _processWaitService.WaitForExit(installerArguments.AppProcessId);
            // Копируем распакованные файлы в целевой каталог.
            _fileDeploymentService.Deploy(extractedDirectory, installerArguments.TargetDirectory);
            // Запускаем основное приложение после завершения копирования.
            _appStarterService.Start(installerArguments.AppExePath);

            // Возвращаем информацию об установленной версии.
            return updateInfo;
        }
    }
}
