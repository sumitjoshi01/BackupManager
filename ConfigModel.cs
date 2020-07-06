using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager.Models
{
    public class ConfigModel
    {
        public ConfigPatterns ConfigPattern { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return ConfigPattern.ToString() + $" => {Value}";
        }
    }
}
