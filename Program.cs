using BackupManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupManager
{
    class Program
    {
        private static AppData applicationData;

        private static readonly string _sourceFolderPattern = @"^[a-zA-Z]:\\(((?![<>:""/\\|?*]).)+((?<![ .])\\)?)*$";
        private static readonly string _relativeFolderPtrn = @"^\\.+$";
        private static readonly string _fileExtensionPatern = @"^\.[\w]+$";
        private static readonly string _relativeFileNamePtrn = @"^[\w\-. ]+\.[a-zA-Z0-9]+$";

        private static IEnumerable<string> ignoreExtensions;
        private static IEnumerable<string> ignoreFileNames;
        private static long fileCount = 0;
        private static long dirCount = 0;

        static void Main(string[] args)
        {
            try
            {
                if (applicationData == null)
                {
                    applicationData = AppData.Load();
                }

                applicationData.NoOfTimesAppRan = ++applicationData.NoOfTimesAppRan;
                applicationData.LastRanTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss tt");

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

                applicationData.NoOfFilesBackedUp += fileCount;
                applicationData.NoOfDirectoriesBackedUp += dirCount;
                applicationData.Save();

                Console.WriteLine($"\nBackup Completed. Copied {fileCount} files and {dirCount} folders");
            }
            catch (Exception ex)
            {
                File.AppendAllText("ExceptionDetail.txt", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt \n") + ex.ToString() + "\n");
                SendErrorMail(ex);
            }

            if (ConfigHelper.GetSetting<bool>("ShowConsoleAfterComplete"))
            {
                Console.ReadKey();
            }

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

            foreach (ConfigModel srcFolder in sourceFolders)
            {
                string sourceFolderName = Path.GetFileName(srcFolder.Value);

                CopyDirectory(srcFolder.Value, Path.Combine(backupDir, sourceFolderName), configModel);
            }
        }

        private static void CopyDirectory(string source, string destination, List<ConfigModel> configModel)
        {
            IEnumerable<string> ignoreRelativeFolders = configModel.Where(C => C.ConfigPattern == ConfigPatterns.IgnoreFolder)
                .Select(S => { return Path.Combine(source, S.Value.Substring(1)); });

            if (!Directory.Exists(destination))
            {
                Console.WriteLine($"Copying directory {source} -->");
                Directory.CreateDirectory(destination);
            }

            string[] filePaths = Directory.GetFiles(source);

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
            List<string> configLines = File.ReadAllLines(backupConfigFileName).ToList();

            foreach (string line in configLines)
            {
                // ignore the comments in files
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                Match sourceFldrPtrnRslt = Regex.Match(line, _sourceFolderPattern);

                if (sourceFldrPtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.SourceFolder,
                        Value = line
                    });

                    continue;
                }

                Match fileExtensionPtrnRslt = Regex.Match(line, _fileExtensionPatern);

                if (fileExtensionPtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.IgnoreExtension,
                        Value = line
                    });

                    continue;
                }

                Match relativeFileNamePtrnRslt = Regex.Match(line, _relativeFileNamePtrn);

                if (relativeFileNamePtrnRslt.Success)
                {
                    configList.Add(new ConfigModel
                    {
                        ConfigPattern = ConfigPatterns.IgnoreFileName,
                        Value = line
                    });

                    continue;
                }

                Match releativeFldrPtrnRslt = Regex.Match(line, _relativeFolderPtrn);

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

        private static BackupExistResult IsBackupExist(string backupDirectory)
        {
            BackupExistResult directoryExistResult = new BackupExistResult();

            DirectoryInfo directoryInfo = new DirectoryInfo(backupDirectory);


            string today = DateTime.Today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            string _backupFolderNamePtrn = $"{today}(_(\\d+))?$";

            IEnumerable<string> topDirResult = directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly).OrderByDescending(O => O.CreationTimeUtc).Select(D => D.Name);

            Match matchResult;

            foreach (string dirName in topDirResult)
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

        private static void SendMail(string to, string subject, string body)
        {
            string from = "";
            MailMessage mail = new MailMessage(from, to, subject, body);
            SmtpClient client = new SmtpClient("smtp.gmail.com");
            client.Port = 587;
            client.Credentials = new NetworkCredential(from, "");
            client.EnableSsl = true;
            client.Send(mail);
        }

        private static void SendErrorMail(Exception ex)
        {
            if (!ConfigHelper.GetSetting<bool>("IsSendErrorMail"))
            {
                return;
            }

            StringBuilder mailBody = new StringBuilder(string.Empty);
            mailBody.AppendLine($"Error Date Time: {DateTime.Now:dd-MM-yyyy hh:mm:ss tt}");
            mailBody.AppendLine($"Error Message: {ex.Message}");
            mailBody.AppendLine($"Stack Trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                mailBody.AppendLine($"Inner Exception Details:");

                if (!string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    mailBody.AppendLine($"Inner Error Message: {ex.InnerException.Message}");
                }

                if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                {
                    mailBody.AppendLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
            }

            string mailTo = ConfigHelper.GetSetting<string>("SendErrorMailTo");
            SendMail(mailTo, "Error In PIE Backup QA Scheduled Task", mailBody.ToString());
        }
    }
}
