using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace VnUpdater
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            string logFile = null;

            try
            {
                if (args.Length < 4)
                {
                    Console.WriteLine("Недостаточно аргументов.");
                    return 1;
                }

                string targetAppDirectory = args[0];
                string updateSourceDirectory = args[1];
                string appExePath = args[2];
                int mainProcessId = int.Parse(args[3]);

                logFile = Path.Combine(targetAppDirectory, "vn-updater.log");
                Log(logFile, "Updater started.");
                Log(logFile, "Target directory: " + targetAppDirectory);
                Log(logFile, "Update source: " + updateSourceDirectory);
                Log(logFile, "App exe path: " + appExePath);
                Log(logFile, "Main process id: " + mainProcessId);

                WaitForMainAppToExit(mainProcessId, logFile);
                CopyAllFiles(updateSourceDirectory, targetAppDirectory, logFile);
                StartUpdatedApplication(appExePath, targetAppDirectory, logFile);

                Log(logFile, "Updater finished successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(logFile))
                    {
                        Log(logFile, "Updater error: " + ex);
                    }
                }
                catch
                {
                }

                Console.WriteLine("Ошибка updater: " + ex.Message);
                return 1;
            }
        }

        private static void WaitForMainAppToExit(int processId, string logFile)
        {
            Log(logFile, "Waiting for main process to exit...");

            for (int i = 0; i < 30; i++)
            {
                try
                {
                    Process process = Process.GetProcessById(processId);
                    if (process.HasExited)
                    {
                        Log(logFile, "Main process has exited.");
                        return;
                    }
                }
                catch
                {
                    Log(logFile, "Main process not found anymore. Continue update.");
                    return;
                }

                Thread.Sleep(1000);
            }

            Log(logFile, "Timeout while waiting main process. Continue anyway.");
        }

        private static void CopyAllFiles(string sourceDir, string targetDir, string logFile)
        {
            Log(logFile, "Copying files...");

            string[] directories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < directories.Length; i++)
            {
                string relativeDir = directories[i].Substring(sourceDir.Length).TrimStart('\\');
                string targetSubDir = Path.Combine(targetDir, relativeDir);

                if (!Directory.Exists(targetSubDir))
                {
                    Directory.CreateDirectory(targetSubDir);
                    Log(logFile, "Created directory: " + targetSubDir);
                }
            }

            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);

                // ВАЖНО: не пытаемся перезаписать сам updater, пока он запущен
                if (string.Equals(fileName, "vn-updater.exe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "vn-updater.exe.config", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "vn-updater.pdb", StringComparison.OrdinalIgnoreCase))
                {
                    Log(logFile, "Skipped updater file: " + files[i]);
                    continue;
                }

                string relativeFile = files[i].Substring(sourceDir.Length).TrimStart('\\');
                string targetFile = Path.Combine(targetDir, relativeFile);

                string targetFileDirectory = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetFileDirectory))
                {
                    Directory.CreateDirectory(targetFileDirectory);
                }

                CopyFileWithRetry(files[i], targetFile, logFile);
            }

            Log(logFile, "File copy completed.");
        }

        private static void CopyFileWithRetry(string sourceFile, string targetFile, string logFile)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    File.Copy(sourceFile, targetFile, true);
                    Log(logFile, "Copied: " + sourceFile + " -> " + targetFile);
                    return;
                }
                catch (IOException ex)
                {
                    Log(logFile, "Copy retry " + (i + 1) + " failed for: " + targetFile + ". " + ex.Message);
                    Thread.Sleep(1000);
                }
            }

            // Последняя попытка с выбросом исключения
            File.Copy(sourceFile, targetFile, true);
            Log(logFile, "Copied after retries: " + sourceFile + " -> " + targetFile);
        }

        private static void StartUpdatedApplication(string appExePath, string workingDirectory, string logFile)
        {
            Log(logFile, "Starting updated application...");

            Process.Start(new ProcessStartInfo
            {
                FileName = appExePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });

            Log(logFile, "Updated application started.");
        }

        private static void Log(string logFile, string message)
        {
            File.AppendAllText(
                logFile,
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine);
        }
    }
}