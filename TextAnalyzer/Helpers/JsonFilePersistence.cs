using System.IO;
using System.Text;
using System.Text.Json;
using TextAnalyzer.Interfaces;

namespace TextAnalyzer.Helpers
{
    internal class JsonFilePersistence : IFilePersistence
    {
        public T? Load<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default(T);

            var jsonStr = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<T>(jsonStr);
        }

        public void Save<T>(T instance, string filePath)
        {
            var jsonStr = JsonSerializer.Serialize(instance);
            File.WriteAllText(filePath, jsonStr, Encoding.UTF8);
        }
    }
}
