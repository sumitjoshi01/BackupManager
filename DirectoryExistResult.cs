using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager
{
    public class BackupExistResult
    {
        public BackupExistResult()
        {

        }

        public BackupExistResult(string dirName)
        {
            OldBackupDirName = dirName;
        }

        public BackupExistResult(string dirName, int existingCount)
        {
            OldBackupDirName = dirName;
            OldBackupNo = existingCount;
        }

        public string OldBackupDirName { get; set; }
        public int OldBackupNo { get; set; }

        public bool IsExist
        {
            get { return !string.IsNullOrEmpty(OldBackupDirName); }
        }

        public int NewBackupNo
        {
            get
            {
                return OldBackupNo + 1;
            }
        }

        public string NewBackupDirName
        {
            get
            {
                if (string.IsNullOrEmpty(OldBackupDirName))
                {
                    return DateTime.Today.ToString("dd-MM-yyyy");
                }

                if (OldBackupNo == 1)
                {
                    return DateTime.Today.ToString("dd-MM-yyyy") + "_2";
                }


                return OldBackupDirName.Replace($"_{OldBackupNo}", $"_{NewBackupNo}");
            }
        }

    }
}
