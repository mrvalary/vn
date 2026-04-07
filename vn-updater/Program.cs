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
            try
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Недостаточно аргументов.");
                    return 1;
                }

                string targetAppDirectory = args[0];
                string updateSourceDirectory = args[1];
                string appExePath = args[2];

                Console.WriteLine("Ожидание завершения основного приложения...");
                Thread.Sleep(3000);

                CopyAllFiles(updateSourceDirectory, targetAppDirectory);

                Console.WriteLine("Файлы обновлены.");
                Console.WriteLine("Запуск приложения: " + appExePath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = appExePath,
                    UseShellExecute = true
                });

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка updater: " + ex.Message);
                return 1;
            }
        }

        private static void CopyAllFiles(string sourceDir, string targetDir)
        {
            string[] directories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < directories.Length; i++)
            {
                string relativeDir = directories[i].Substring(sourceDir.Length).TrimStart('\\');
                string targetSubDir = Path.Combine(targetDir, relativeDir);

                if (!Directory.Exists(targetSubDir))
                {
                    Directory.CreateDirectory(targetSubDir);
                }
            }

            string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativeFile = files[i].Substring(sourceDir.Length).TrimStart('\\');
                string targetFile = Path.Combine(targetDir, relativeFile);

                string targetFileDirectory = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetFileDirectory))
                {
                    Directory.CreateDirectory(targetFileDirectory);
                }

                File.Copy(files[i], targetFile, true);
            }
        }
    }
}