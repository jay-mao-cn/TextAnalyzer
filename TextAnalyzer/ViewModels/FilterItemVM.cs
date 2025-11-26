using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TextAnalyzer.Helpers;
using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels
{
    internal partial class FilterItemVM : ViewModelBase
    {
        public static Action? EnablementChanged;

        FilterRunner? _filterRunner = null;

        // For performance concern
        private bool _useMarker = false;
        private bool UseMarker
        {
            get => _useMarker;
            set
            {
                if (_useMarker != value)
                {
                    _useMarker = value;
                    OnPropertyChanged(nameof(Pattern));
                }
            }
        }

        public bool IsExcluding { get; private set; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                    EnablementChanged?.Invoke();
                }
            }
        }

        [ObservableProperty]
        private string _name = string.Empty;

        public ObservableCollection<FilterModiferType> Modifiers
        { get; private set; } = [];

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            private set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Pattern));
                }
            }
        }

        private int _marker = 0;
        public int Marker
        {
            get => _marker;
            private set
            {
                if (_marker != value)
                {
                    _marker = value;
                    OnPropertyChanged(nameof(Pattern));
                }
            }
        }

        public string Pattern
        {
            get
            {
                if (Modifiers.Contains(FilterModiferType.Marker))
                {
                    return Marker.ToString();
                }
                else
                {
                    return Text;
                }
            }
        }

        [ObservableProperty]
        private string _description = string.Empty;

        private Color _fgColor;
        public Color ForegroundColor
        {
            get => _fgColor;
            private set
            {
                if (_fgColor != value)
                {
                    _fgColor = value;
                    Foreground = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(Foreground));
                }
            }
        }

        // Can only be accessed form UI thread.
        public IBrush Foreground { get; private set; } = Brushes.Black;

        private Color _bgColor;
        public Color BackgroundColor
        {
            get => _bgColor;
            private set
            {
                if (_bgColor != value)
                {
                    _bgColor = value;
                    Background = new SolidColorBrush(value);
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        // Can only be accessed form UI thread.
        public IBrush Background { get; private set; } = Brushes.Transparent;

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

        List<int> _hitLines = [];
        public int Hits => _hitLines.Count;

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

        internal FilterItemVM(FilterModel model)
        {
            Update(model);
            Init();
            OnModifiersUpdate();
        }

        internal void Update(FilterModel model)
        {
            _isEnabled = model.IsEnabled;
            _fgColor = model.ForegroundColor;
            _bgColor = model.BackgroundColor;

            Description = model.Description;

            Modifiers.Clear();
            if (model.IsExcluded)
                Modifiers.Add(FilterModiferType.Excluding);

            if (model.FilterType == FilterType.Marker)
            {
                Modifiers.Add(FilterModiferType.Marker);
                Marker = model.Marker;
            }
            else
            {
                Text = model.FilterText;

                if (model.IsCaseSensitive)
                    Modifiers.Add(FilterModiferType.CaseSensitive);

                if (model.IsRegularExpression)
                {
                    Modifiers.Add(FilterModiferType.RegularExpression);
                }
                else if (model.IsLogicOperation)
                {
                    Modifiers.Add(FilterModiferType.LogicOperation);
                }
            }

            _filterRunner = new FilterRunner(model);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Match(string input, IEnumerable<int> markers)
        {
            return _filterRunner!.Match(input, markers);
        }

        internal void ClearHits()
        {
            _hitLines.Clear();
        }

        internal void AddHitLine(int lineNum)
        {
            _hitLines.Add(lineNum);
        }

        internal void CommitHits()
        {
            OnPropertyChanged(nameof(Hits));
        }

        internal int FindNextLineNumber(int curLineNum, bool backward)
        {
            Debug.Assert(_hitLines.Count > 0);
            if (_hitLines.Count == 0)
                return -1;

            int index = _hitLines.BinarySearch(curLineNum);
            if (index < 0)
            {
                // If not found, BinarySearch returns bitwise complement of next larger element
                var nextLarger = ~index;
                if (backward)
                {
                    if (nextLarger == 0)
                    {
                        index = _hitLines.Count - 1;
                    }
                    else
                    {
                        index = nextLarger - 1;
                    }
                }
                else
                {
                    if (nextLarger < _hitLines.Count)
                    {
                        index = nextLarger;
                    }
                    else
                    {
                        index = 0;
                    }
                }
            }
            else
            {
                if (backward)
                {
                    if (index > 0)
                    {
                        --index;
                    }
                    else
                    {
                        index = _hitLines.Count - 1;
                    }
                }
                else
                {
                    if (index < _hitLines.Count - 1)
                    {
                        ++index;
                    }
                    else
                    {
                        index = 0;
                    }
                }
            }

            return _hitLines[index];
        }

        void Init()
        {
            Foreground = new SolidColorBrush(_fgColor);
            Background = new SolidColorBrush(_bgColor);

            Modifiers.CollectionChanged += (s, e) => OnModifiersUpdate();
        }

        void OnModifiersUpdate()
        {
            UseMarker = Modifiers.Contains(FilterModiferType.Marker);
            IsExcluding = Modifiers.Contains(FilterModiferType.Excluding);
        }
    }
}
