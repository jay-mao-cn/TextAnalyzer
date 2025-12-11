using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels
{
    internal partial class EditFilterVM : ViewModelBase
    {
        public string Title { get; private set; } = "Add Filter";

        public IEnumerable<FilterType> FilterTypes { get; private set; }
        [ObservableProperty]
        FilterType _selectedFilterType;

        [ObservableProperty]
        Color _selectedTextColor;

        [ObservableProperty]
        Color _selectedBackgroundColor = Colors.Transparent;

        [ObservableProperty]
        string _text = string.Empty;

        public IEnumerable<int> Markers { get; private set; }
        [ObservableProperty]
        int _selectedMarker = 1;

        [ObservableProperty]
        string _description = string.Empty;

        [ObservableProperty]
        bool _isExcluded = false;

        [ObservableProperty]
        bool _isCaseSensitive = false;

        [ObservableProperty]
        bool _isRegularExpression = false;

        [ObservableProperty]
        bool _isLogicOperation = false;

        public bool IsFilterValid
        {
            get
            {
                if (SelectedFilterType == FilterType.Text)
                {
                    return Text.Length > 0;
                }
                else
                {
                    return true;
                }
            }
        }

        public EditFilterVM()
        {
            FilterTypes = new List<FilterType>()
            {
                FilterType.Text,
                FilterType.Marker
            };

            var markers = new List<int>();
            for (int i = 1; i < 10; ++i)
            {
                markers.Add(i);
            }
            Markers = markers;

            object? color = null;
            var app = Application.Current;
            if (app?.TryFindResource("ForegroundColor", app.ActualThemeVariant, out color) == true)
                _selectedTextColor = (Color)color!;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedFilterType)
                    || e.PropertyName == nameof(Text))
                {
                    OnPropertyChanged(nameof(IsFilterValid));
                }
            };
        }

        public EditFilterVM(FilterModel model) : this()
        {
            Title = "Edit Filter";
            _selectedFilterType = model.FilterType;
            switch (model.FilterType)
            {
                case FilterType.Text:
                    _text = model.FilterText;
                    break;
                case FilterType.Marker:
                    _selectedMarker = model.Marker;
                    break;
            }

            _selectedTextColor = model.ForegroundColor;
            _selectedBackgroundColor = model.BackgroundColor;
            _description = model.Description;
            _isExcluded = model.IsExcluded;
            _isCaseSensitive = model.IsCaseSensitive;
            _isRegularExpression = model.IsRegularExpression;
            _isLogicOperation = model.IsLogicOperation;
        }

        internal FilterModel GetModel()
        {
            var model = new FilterModel()
            {
                FilterType = SelectedFilterType,
                Description = Description,
                ForegroundColor = SelectedTextColor,
                BackgroundColor = SelectedBackgroundColor,
                IsExcluded = IsExcluded,
            };

            switch (model.FilterType)
            {
                case FilterType.Marker:
                    model.Marker = SelectedMarker;
                    break;

                case FilterType.Text:
                    model.FilterText = Text;
                    model.IsCaseSensitive = IsCaseSensitive;
                    model.IsRegularExpression = IsRegularExpression;
                    model.IsLogicOperation = IsLogicOperation;
                    break;

                default:
                    throw new Exception($"Invalid filter type: {model.FilterType}");
            }

            return model;
        }
    }
}
