using System;
using System.IO;
using System.Text.Json;

namespace BackupManager.Models
{
    [Serializable]
    public class JsonSettings<T> where T : new()
    {
        private const string DEFAULT_FILENAME = "data.json";

        public void Save(string fileName = DEFAULT_FILENAME)
        {
            File.WriteAllText(fileName, JsonSerializer.Serialize(this, typeof(T), new JsonSerializerOptions { WriteIndented = true }));
        }

        public static void Save(T pSettings, string fileName = DEFAULT_FILENAME)
        {
            File.WriteAllText(fileName, JsonSerializer.Serialize(pSettings, typeof(T), new JsonSerializerOptions { WriteIndented = true }));
        }

        public static T Load(string fileName = DEFAULT_FILENAME)
        {
            T t = new T();
            if (File.Exists(fileName))
                t = JsonSerializer.Deserialize<T>(File.ReadAllText(fileName));
            return t;
        }
    }
}
