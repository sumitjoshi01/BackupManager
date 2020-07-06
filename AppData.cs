using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager.Models
{
    public class AppData : JsonSettings<AppData>
    {
        public string AppVersion { get; set; } = "1.0.0.0";
        public string Creator { get; set; } = "Sumit Joshi";
        public long NoOfTimesAppRan { get; set; }
        public long NoOfFilesBackedUp { get; set; }
        public long NoOfDirectoriesBackedUp { get; set; }
        public string LastRanTime { get; set; }
    }
}
