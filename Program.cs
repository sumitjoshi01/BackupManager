using BackupManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BackupManager
{
    class Program
    {
        private static readonly string _sourceFolderPattern = @"^[a-zA-Z]:\\(((?![<>:""/\\|?*]).)+((?<![ .])\\)?)*$";
        private static readonly string _relativeFolderPtrn = @"^\\.+$";
        private static readonly string _fileExtensionPatern = @"^\.[\w]+$";
        private static readonly string _relativeFileNamePtrn = @"^[\w\-. ]+\.[a-zA-Z0-9]+$";

        private static IEnumerable<string> ignoreExtensions;
        private static IEnumerable<string> ignoreFileNames;

        static long fileCount = 0;
        static long dirCount = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("                          Backup Tool Created By Sumit Joshi                              ");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            string backupDirectory = ConfigHelper.GetSetting<string>("BackupFolder");

            CreateRootBackupDirIfNotExist(backupDirectory);

            BackupExistResult bkpExistRslt = IsBackupExist(backupDirectory);

            if (bkpExistRslt.IsExist)
            {
                Console.WriteLine($"Backup Already Exist At {bkpExistRslt.OldBackupDirName}. Creating New Backup At {bkpExistRslt.NewBackupDirName}\n");
            }

            backupDirectory = Path.Combine(backupDirectory, bkpExistRslt.NewBackupDirName);

            List<ConfigModel> configList = LoadBackupConfigFile();

            TakeBackup(configList, backupDirectory);

            Console.WriteLine($"\nBackup Completed. Copied {fileCount} files and {dirCount} folders");

            if (ConfigHelper.GetSetting<bool>("ShowConsoleAfterComplete"))
            {
                Console.ReadKey();
            }

        }

        private static void CreateRootBackupDirIfNotExist(string backupDirectory)
        {
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }
        }

        private static List<ConfigModel> LoadBackupConfigFile()
        {
            List<ConfigModel> configList = new List<ConfigModel>();

            string backupConfigFileName = ConfigHelper.GetSetting<string>("BackupConfigFilePath");
            var configLines = File.ReadAllLines(backupConfigFileName).ToList();

            foreach (var line in configLines)
            {
                // ignore the comments in files
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var sourceFldrPtrnRslt = Regex.Match(line, _sourceFolderPattern);

                if (sourceFldrPtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.SourceFolder,
                        Value = line
                    });

                    continue;
                }

                var fileExtensionPtrnRslt = Regex.Match(line, _fileExtensionPatern);

                if (fileExtensionPtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.IgnoreExtension,
                        Value = line
                    });

                    continue;
                }

                var relativeFileNamePtrnRslt = Regex.Match(line, _relativeFileNamePtrn);

                if (relativeFileNamePtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.IgnoreFileName,
                        Value = line
                    });

                    continue;
                }

                var releativeFldrPtrnRslt = Regex.Match(line, _relativeFolderPtrn);

                if (releativeFldrPtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.IgnoreFolder,
                        Value = line
                    });

                    continue;
                }

            }

            return configList;
        }

        private static void TakeBackup(List<ConfigModel> configModel, string backupDir)
        {
            IEnumerable<ConfigModel> sourceFolders = configModel.Where(C => C.ConfigPattern == ConfigPatterns.SourceFolder);
            ignoreExtensions = configModel.Where(C => C.ConfigPattern == ConfigPatterns.IgnoreExtension).Select(S => S.Value);
            ignoreFileNames = configModel.Where(C => C.ConfigPattern == ConfigPatterns.IgnoreFileName).Select(S => S.Value);

            if (ignoreExtensions.Any())
            {
                Console.Write("\nIgnoring extensions: ");
                Console.Write($"{string.Join(",", ignoreExtensions)}\n");
                Console.WriteLine();
            }

            if (ignoreFileNames.Any())
            {
                Console.Write("\nIgnoring Files: ");
                Console.Write($"{string.Join(",", ignoreFileNames)}\n");
                Console.WriteLine();
            }

            foreach (var item in sourceFolders)
            {
                CopyDirectory(item.Value, backupDir, configModel);
            }
        }

        private static void CopyDirectory(string source, string destination, List<ConfigModel> configModel)
        {
            var ignoreRelativeFolders = configModel.Where(C => C.ConfigPattern == ConfigPatterns.IgnoreFolder).Select(S => { return Path.Combine(source, S.Value.Substring(1)); });

            if (!Directory.Exists(destination))
            {
                Console.WriteLine($"Copying directory {source} -->");
                Directory.CreateDirectory(destination);
            }

            var filePaths = Directory.GetFiles(source);

            foreach (string filePath in filePaths)
            {
                string fileExtension = Path.GetExtension(filePath);

                if (!string.IsNullOrEmpty(fileExtension) && ignoreExtensions.Any(ignoreExt => string.Equals(ignoreExt, fileExtension, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                string fileName = Path.GetFileName(filePath);

                if (!string.IsNullOrEmpty(fileName) && ignoreFileNames.Any(ignoreFileName => string.Equals(ignoreFileName, fileName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                Console.WriteLine($"Copying file {fileName}");
                fileCount++;
                string dest = Path.Combine(destination, fileName);
                File.Copy(filePath, dest);
            }

            string[] nestedFolders = Directory.GetDirectories(source);

            foreach (string nestedFolderPath in nestedFolders)
            {
                if (ignoreRelativeFolders.Any(ignoreFolderPath => string.Equals(ignoreFolderPath, nestedFolderPath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                dirCount++;

                string sourceFolder = Path.GetFileName(nestedFolderPath);
                string destFolder = Path.Combine(destination, sourceFolder);
                CopyDirectory(nestedFolderPath, destFolder, configModel);
            }
        }

        private static BackupExistResult IsBackupExist(string backupDirectory)
        {
            BackupExistResult directoryExistResult = new BackupExistResult();

            DirectoryInfo directoryInfo = new DirectoryInfo(backupDirectory);


            string today = DateTime.Today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            string _backupFolderNamePtrn = $"{today}(_(\\d+))?$";

            var topDirResult = directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly).OrderByDescending(O => O.CreationTimeUtc).Select(D => D.Name);

            Match matchResult;

            foreach (var dirName in topDirResult)
            {
                matchResult = Regex.Match(dirName, _backupFolderNamePtrn);

                if (matchResult.Success)
                {
                    directoryExistResult = new BackupExistResult(dirName);

                    directoryExistResult.OldBackupNo = matchResult.Groups[2].Success ? Convert.ToInt32(matchResult.Groups[2].Value) : 1;

                    break;
                }
            }

            return directoryExistResult;
        }
    }
}
