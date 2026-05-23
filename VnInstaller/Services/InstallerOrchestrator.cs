using System.Threading;
using System.Threading.Tasks;
using VnInstaller.Models;

namespace VnInstaller.Services
{
    /// <summary>
    /// Координатор полного сценария установки.
    /// </summary>
    public sealed class InstallerOrchestrator
    {
        // Сервис обращения к GitHub Releases.
        private readonly GitHubReleaseService _gitHubReleaseService;
        // Сервис скачивания zip-архива релиза.
        private readonly ReleaseDownloadService _releaseDownloadService;
        // Сервис распаковки архива во временную папку.
        private readonly ArchiveExtractorService _archiveExtractorService;
        // Сервис ожидания завершения обновляемого процесса.
        private readonly ProcessWaitService _processWaitService;
        // Сервис копирования файлов в каталог установки.
        private readonly FileDeploymentService _fileDeploymentService;
        // Сервис запуска основного приложения после установки.
        private readonly AppStarterService _appStarterService;
        // Логгер установщика.
        private readonly FileLogger _logger;

        /// <summary>
        /// Создает координатор установки со всеми зависимостями.
        /// </summary>
        /// <param name="gitHubReleaseService">Сервис обращения к GitHub Releases.</param>
        /// <param name="releaseDownloadService">Сервис скачивания архива релиза.</param>
        /// <param name="archiveExtractorService">Сервис распаковки архива.</param>
        /// <param name="processWaitService">Сервис ожидания завершения обновляемого процесса.</param>
        /// <param name="fileDeploymentService">Сервис копирования файлов в папку установки.</param>
        /// <param name="appStarterService">Сервис запуска приложения после установки.</param>
        /// <param name="logger">Логгер установщика.</param>
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

        /// <summary>
        /// Выполняет полный сценарий установки и обновления в одном режиме.
        /// </summary>
        /// <param name="targetDirectory">Целевая папка установки приложения.</param>
        /// <param name="appExePath">Путь к exe приложения после установки.</param>
        /// <param name="appProcessId">PID обновляемого процесса приложения.</param>
        /// <param name="cancellationToken">Токен отмены асинхронных операций.</param>
        /// <returns>Информация об установленном релизе.</returns>
        public async Task<AppUpdateInfo> ExecuteAsync(string targetDirectory, string appExePath, int? appProcessId, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Сначала получаем сведения о последнем релизе.
            AppUpdateInfo updateInfo = await _gitHubReleaseService.GetLatestReleaseAsync(cancellationToken);
            // Пишем найденную версию в лог.
            _logger.Info("Latest release version: " + updateInfo.LatestVersion);

            // Скачиваем zip-архив релиза.
            string archivePath = await _releaseDownloadService.DownloadAsync(updateInfo, cancellationToken);
            // Распаковываем архив во временный каталог.
            string extractedDirectory = _archiveExtractorService.Extract(archivePath, updateInfo.LatestVersion);

            // Дожидаемся завершения обновляемого экземпляра основного приложения.
            _processWaitService.WaitForExit(appProcessId);
            // Копируем новые файлы в целевую папку установки.
            _fileDeploymentService.Deploy(extractedDirectory, targetDirectory);
            // После успешного копирования запускаем основное приложение.
            _appStarterService.Start(appExePath);

            // Возвращаем сведения об установленной версии.
            return updateInfo;
        }
    }
}
