using Avalonia.Platform.Storage;
using TextAnalyzer.Models;

namespace TextAnalyzer.Interfaces
{
    internal interface IFileHandler
    {
        void LoadFile(IStorageFile file, FileType type);
    }
}
