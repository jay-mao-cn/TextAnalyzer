using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels
{
    internal partial class FilterItemVM : ViewModelBase
    {
        public static Action? EnablementChanged;

        enum LogicOperType
        {
            None,
            And,
            Or,
        }

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

        private bool _caseSensitive = false;
        private bool _useRegularExpression = false;
        private bool _isLogicOperation = false;
        private bool IsLogicOperation
        {
            get => _isLogicOperation;
            set
            {
                if (_isLogicOperation != value)
                {
                    _isLogicOperation = value;
                    ParseText(Text);
                }
            }
        }

        public bool IsExcluding { get; private set; }
        private string[] _texts = [];
        private LogicOperType _logicOperator = LogicOperType.None;

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
            set
            {
                if (_text != value)
                {
                    _text = value;
                    ParseText(value);
                    OnPropertyChanged(nameof(Pattern));
                }
            }
        }

        private int _marker = 0;
        public int Marker
        {
            get => _marker;
            set
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
            set
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
            set
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

        public List<int> HitLines { get; private set; } = [];
        public int Hits => HitLines.Count;

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

        public FilterItemVM()
        {
            _fgColor = Colors.Black;
            _bgColor = Colors.Transparent;

            Init();
        }

        public FilterItemVM(FilterModel model)
        {
            _isEnabled = model.IsEnabled;
            _fgColor = model.ForegroundColor;
            _bgColor = model.BackgroundColor;

            Description = model.Description;

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

            Init();
            OnModifiersUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Match(string input, IEnumerable<int> markers)
        {
            if (_useMarker)
            {
                return markers.Contains(_marker);
            }

            if (_useRegularExpression)
            {
                var options = _caseSensitive
                    ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(input, _text, options);
            }

            StringComparison comparison = _caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            switch (_logicOperator)
            {
                case LogicOperType.None:
                    return input.Contains(_text, comparison);

                case LogicOperType.And:
                    foreach (var txt in _texts)
                    {
                        if (!input.Contains(txt, comparison))
                            return false;
                    }
                    return true;

                case LogicOperType.Or:
                    foreach (var txt in _texts)
                    {
                        if (input.Contains(txt, comparison))
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        public void CommitHits()
        {
            OnPropertyChanged(nameof(Hits));
        }

        public int FindNextLineNumber(int curLineNum, bool backward)
        {
            Debug.Assert(HitLines.Count > 0);
            if (HitLines.Count == 0)
                return -1;

            int index = HitLines.BinarySearch(curLineNum);
            if (index < 0)
            {
                // If not found, BinarySearch returns bitwise complement of next larger element
                var nextLarger = ~index;
                if (backward)
                {
                    if (nextLarger == 0)
                    {
                        index = HitLines.Count - 1;
                    }
                    else
                    {
                        index = nextLarger - 1;
                    }
                }
                else
                {
                    if (nextLarger < HitLines.Count)
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
                        index = HitLines.Count - 1;
                    }
                }
                else
                {
                    if (index < HitLines.Count - 1)
                    {
                        ++index;
                    }
                    else
                    {
                        index = 0;
                    }
                }
            }

            return HitLines[index];
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

            _caseSensitive =
                Modifiers.Contains(FilterModiferType.CaseSensitive);

            _useRegularExpression =
                Modifiers.Contains(FilterModiferType.RegularExpression);

            IsLogicOperation =
                Modifiers.Contains(FilterModiferType.LogicOperation);

            IsExcluding = Modifiers.Contains(FilterModiferType.Excluding);
        }

        void ParseText(string text)
        {
            if (IsLogicOperation)
            {
                _texts = text.Split(" && ");
                if (_texts.Length > 1)
                {
                    _logicOperator = LogicOperType.And;
                }
                else
                {
                    _texts = text.Split(" || ");
                    if (_texts.Length > 1)
                    {
                        _logicOperator = LogicOperType.Or;
                    }
                    else
                    {
                        _logicOperator = LogicOperType.None;
                    }
                }
            }
            else
            {
                _logicOperator = LogicOperType.None;
            }
        }
    }
}
