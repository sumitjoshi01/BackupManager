using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager
{
    public static class ConfigHelper
    {

        public static T GetSetting<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default;
            }

            string settingValue = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(settingValue))
            {
                return default;
            }

            T returnValue;

            try
            {
                returnValue = (T)Convert.ChangeType(settingValue, typeof(T));
            }
            catch (Exception)
            {
                return default;
            }

            return returnValue;
        }
    }
}
