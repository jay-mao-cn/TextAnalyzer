using System;
using System.Collections.Generic;
using TextAnalyzer.Models;

namespace TextAnalyzer.Interfaces;

interface IRecentFileManager
{
    event Action<FileType, string> OnRecentFileAdded;
    event Action<FileType, string> OnRecentFileRemoved;
    event Action<FileType> OnRecentFileCleared;
    IEnumerable<string> GetRecentFiles(FileType fileType);
}