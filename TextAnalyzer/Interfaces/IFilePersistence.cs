namespace TextAnalyzer.Interfaces
{
    internal interface IFilePersistence
    {
        T? Load<T>(string filePath);
        void Save<T>(T instance, string filePath);
    }
}
