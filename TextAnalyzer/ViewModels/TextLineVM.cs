using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TextAnalyzer.ViewModels
{
    internal partial class TextLineVM : ViewModelBase
    {
        public bool IsExcluded { get; set; } = false;

        [ObservableProperty]
        private bool _isShowLineNum = true;
        /// <summary>
        /// Start from 1
        /// </summary>
        public int LineNumber { get; private set; }

        [ObservableProperty]
        private bool _isShowMarker = true;
        public ObservableCollection<int> Markers { get; private set; } = [];

        [ObservableProperty]
        private IBrush _foreground = Brushes.Black;

        [ObservableProperty]
        private IBrush _background = Brushes.Transparent;

        public IBrush DisplayBackground
        {
            get
            {
                if (IsSelected)
                {
                    return Brushes.Transparent;
                }
                else
                {
                    return Background;
                }
            }
        }

        public string Text { get; private set; }

        bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(DisplayBackground));
                }
            }
        }

        public TextLineVM(int lineNum, string text)
        {
            LineNumber = lineNum;
            Text = text;
        }

        public TextLineVM(
            int lineNum, string text, IBrush foreground, IBrush background)
        {
            LineNumber = lineNum;
            Text = text;
            _foreground = foreground;
            _background = background;
        }
    }
}
