using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager.Models
{
    public class BackupExistResult
    {
        public BackupExistResult()
        {

        }

        public BackupExistResult(string dirName, int existingCount)
        {
            TodaysBackupDirName = dirName;
            TodaysBackupNo = existingCount;
        }

        public string TodaysBackupDirName { get; set; }

        public int TodaysBackupNo { get; set; }

        public bool IsTodaysBackupExist => !string.IsNullOrEmpty(TodaysBackupDirName);

        public int NewBackupNo => TodaysBackupNo + 1;

        public IEnumerable<string> ExistingBackupList { get; set; } = new List<string>();

        public int TotalNoOfExistingBackups => ExistingBackupList.Count();

        public string GetNewBackupDirName(string backupDirNamePatternDateFormat)
        {
            if (string.IsNullOrEmpty(TodaysBackupDirName))
            {
                return DateTime.Today.ToString(backupDirNamePatternDateFormat);
            }

            if (TodaysBackupNo == 1)
            {
                return DateTime.Today.ToString(backupDirNamePatternDateFormat) + "_2";
            }

            return TodaysBackupDirName.Replace($"_{TodaysBackupNo}", $"_{NewBackupNo}");
        }
    }
}
