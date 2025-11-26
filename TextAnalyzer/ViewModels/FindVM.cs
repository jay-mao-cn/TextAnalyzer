using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels
{
    internal partial class FindVM : ViewModelBase, IDisposable
    {
        static List<string> _historyTexts = new List<string>();

        public IEnumerable<string> HistoryTexts => _historyTexts;

        string? _textToFind = null;
        public string? TextToFind
        {
            get => _textToFind;
            set
            {
                if (_textToFind != value)
                {
                    _textToFind = value;
                    OnPropertyChanged(nameof(IsTextValid));
                }
            }
        }

        public bool IsTextValid => !string.IsNullOrEmpty(TextToFind);

        public bool IsCaseSensitive { get; set; }
        [ObservableProperty]
        bool _isRegularExpression = false;
        public bool IsLogicOperation { get; set; }

        internal FindVM(string? text)
        {
            TextToFind = text;
        }

        internal FilterBase GetFilter()
        {
            Debug.Assert(TextToFind != null);
            return new FilterBase()
            {
                FilterType = FilterType.Text,
                FilterText = TextToFind!,
                IsCaseSensitive = IsCaseSensitive,
                IsRegularExpression = IsRegularExpression,
                IsLogicOperation = IsLogicOperation,
            };
        }

        #region IDisposable

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(TextToFind)
                && !_historyTexts.Contains(TextToFind))
            {
                _historyTexts.Add(TextToFind);
            }
        }

        #endregion
    }
}
