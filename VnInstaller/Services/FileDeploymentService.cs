using System;
using System.IO;
using System.Threading;

namespace VnInstaller.Services
{
    public sealed class FileDeploymentService
    {
        private readonly FileLogger _logger;

        public FileDeploymentService(FileLogger logger)
        {
            _logger = logger;
        }

        public void Deploy(string sourceDirectory, string targetDirectory)
        {
            _logger.Info("Deploying files from " + sourceDirectory + " to " + targetDirectory);

            string[] directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < directories.Length; i++)
            {
                string relativeDirectory = directories[i].Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetSubDirectory = Path.Combine(targetDirectory, relativeDirectory);

                if (!Directory.Exists(targetSubDirectory))
                {
                    Directory.CreateDirectory(targetSubDirectory);
                }
            }

            string[] files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (ShouldSkip(fileName))
                {
                    _logger.Info("Skipped locked installer file: " + files[i]);
                    continue;
                }

                string relativeFile = files[i].Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetFile = Path.Combine(targetDirectory, relativeFile);
                string targetFileDirectory = Path.GetDirectoryName(targetFile);

                if (!Directory.Exists(targetFileDirectory))
                {
                    Directory.CreateDirectory(targetFileDirectory);
                }

                CopyFileWithRetry(files[i], targetFile);
            }

            _logger.Info("Deployment completed.");
        }

        private void CopyFileWithRetry(string sourceFile, string targetFile)
        {
            // Повторные попытки позволяют дождаться освобождения файлов после закрытия vn-app.
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    File.Copy(sourceFile, targetFile, true);
                    _logger.Info("Copied: " + sourceFile + " -> " + targetFile);
                    return;
                }
                catch (IOException ex)
                {
                    _logger.Info("Copy attempt " + attempt + " failed for " + targetFile + ". " + ex.Message);
                    Thread.Sleep(1000);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.Info("Copy attempt " + attempt + " failed for " + targetFile + ". " + ex.Message);
                    Thread.Sleep(1000);
                }
            }

            File.Copy(sourceFile, targetFile, true);
            _logger.Info("Copied after retries: " + sourceFile + " -> " + targetFile);
        }

        private static bool ShouldSkip(string fileName)
        {
            // Установщик не должен перезаписывать сам себя, пока он запущен.
            return
                string.Equals(fileName, "vn-installer.exe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "vn-installer.exe.config", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "vn-installer.pdb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "vn-installer.log", StringComparison.OrdinalIgnoreCase);
        }
    }
}
