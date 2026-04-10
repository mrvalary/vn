using System.IO;
using System.IO.Compression;

namespace VnInstaller.Services
{
    public sealed class ArchiveExtractorService
    {
        private readonly FileLogger _logger;

        public ArchiveExtractorService(FileLogger logger)
        {
            _logger = logger;
        }

        public string Extract(string archivePath, string version)
        {
            string versionDirectory = Path.Combine(Path.GetTempPath(), "vn-installer", version);
            string extractDirectory = Path.Combine(versionDirectory, "extracted");

            if (Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, true);
            }

            Directory.CreateDirectory(extractDirectory);

            _logger.Info("Extracting archive: " + archivePath);
            ZipFile.ExtractToDirectory(archivePath, extractDirectory);
            _logger.Info("Archive extracted to: " + extractDirectory);

            return extractDirectory;
        }
    }
}
