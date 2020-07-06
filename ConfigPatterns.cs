using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager.Models
{
    public enum ConfigPatterns
    {
        IgnoreExtension = 1,
        IgnoreFileName = 2,
        SourceFolder = 3,
        IgnoreFolder = 4,
        Comment = 5
    }
}
