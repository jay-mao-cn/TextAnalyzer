using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TextAnalyzer.Converters;
using TextAnalyzer.Helpers;
using TextAnalyzer.Interfaces;
using TextAnalyzer.Models;
using TextAnalyzer.Models.Converters;

namespace TextAnalyzer.ViewModels
{
    internal partial class MainWindowVM : ViewModelBase
        , IFileHandler, IRecentFileManager, IFilteringObserver, IDisposable
    {
        const string AppName = "Text Analyzer";

        readonly TopLevel _topLevel;
        readonly IFocusMonitor _focusMonitor;
        BackgroundDispatcher _dispatcher = new();
        Preferences _preferences;
        IFilePersistence _persistence;

        #region IFilteringObserver

        public event Action FilteringStarted;
        public event Action FilteringEnded;

        #endregion

        [ObservableProperty]
        string _appTitle = AppName;

        List<string> _statusList = new List<string>();
        public string? Status => _statusList.LastOrDefault();

#pragma warning disable CS8618
        // Non-nullable field must contain a non-null value when exiting constructor.
        public MainWindowVM()
        // For Design.DataContext
#pragma warning restore CS8618
        {
            InitTextsSource();
            InitFiltersSource();
            LoadArchivedtems();
            InitCommands();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public MainWindowVM(
            TopLevel topLevel, IFocusMonitor focusMonitor) : this()
        {
            _topLevel = topLevel;
            _focusMonitor = focusMonitor;
        }

        #region Commands
        // For app menu's commands which requires CanExecuteChanged for Mac native menu.

        public IRelayCommand ReloadTextFileCommand { get; private set; }
        public IRelayCommand SaveCurrentLinesCommand { get; private set; }
        public IRelayCommand SaveFiltersCommand { get; private set; }
        public IRelayCommand SaveFiltersAsCommand { get; private set; }
        public IRelayCommand CopyCommand { get; private set; }
        public IRelayCommand CopyWithLinesCommand { get; private set; }
        public IRelayCommand PasteCommand { get; private set; }
        public IRelayCommand CopyFiltersCommand { get; private set; }
        public IRelayCommand PasteFiltersCommand { get; private set; }
        public IRelayCommand SelectAllTextsCommand { get; private set; }
        public IRelayCommand FindPreviousTextCommand { get; private set; }
        public IRelayCommand FindNextTextCommand { get; private set; }
        public IRelayCommand GoToLineCommand { get; private set; }
        public IRelayCommand ZoomInCommand { get; private set; }
        public IRelayCommand ZoomOutCommand { get; private set; }
        public IRelayCommand FindPreviousMatchCommand { get; private set; }
        public IRelayCommand FindNextMatchCommand { get; private set; }
        public IRelayCommand EditSelectedFilterCommand { get; private set; }
        public IRelayCommand RemoveSelectedFilterCommand { get; private set; }
        public IRelayCommand EnableAllFiltersCommand { get; private set; }
        public IRelayCommand DisableAllFiltersCommand { get; private set; }
        public IRelayCommand RemoveAllFiltersCommand { get; private set; }

        void InitCommands()
        {
            ReloadTextFileCommand = new RelayCommand(
                ReloadTextFile, () => CurrentTextFile != null);

            SaveCurrentLinesCommand = new RelayCommand(
                SaveCurrentLines, () => _texts.Count > 0);

            SaveFiltersCommand = new RelayCommand(
                async () => await SaveFilters(), () => _filters.Count > 0);

            SaveFiltersAsCommand = new RelayCommand(
                SaveFiltersAs, () => _filters.Count > 0);

            CopyCommand = new RelayCommand(Copy, () =>
            {
                var focusedArea = _focusMonitor?.GetFocusedArea();
                switch (focusedArea)
                {
                    case FocusedArea.Filters:
                        return AnyFilterSelected;

                    default:
                        return AnyTextSelected;
                }
            });

            CopyWithLinesCommand = new RelayCommand(CopyWithLines, () => AnyTextSelected);

            PasteCommand = new RelayCommand(Paste, () =>
            {
                var focusedArea = _focusMonitor?.GetFocusedArea();
                switch (focusedArea)
                {
                    case FocusedArea.Filters:
                        return CanPasteFilters();

                    default:
                        var formats = _topLevel?.Clipboard!.GetDataFormatsAsync().Result;
                        return formats?.Contains(DataFormat.Text) == true;
                }
            });

            CopyFiltersCommand = new RelayCommand(CopyFilters, () => AnyFilterSelected);

            PasteFiltersCommand = new RelayCommand(PasteFilters, CanPasteFilters);

            SelectAllTextsCommand = new RelayCommand(
                SelectAllTexts, () => _texts.Count > 0);

            FindPreviousTextCommand = new RelayCommand(() => FindNextText(true),
                () => _textFinder != null);

            FindNextTextCommand = new RelayCommand(() => FindNextText(false),
                () => _textFinder != null);

            GoToLineCommand = new RelayCommand(GoToLine, () => _texts.Count > 0);

            ZoomInCommand = new RelayCommand(ZoomIn, () => ZoomRatio < MaxZoomRatio);
            ZoomOutCommand = new RelayCommand(ZoomOut, () => ZoomRatio > MinZoomRatio);

            FindPreviousMatchCommand = new RelayCommand<Key?>(
                FindPreviousMatch, CanFindNextMatch);

            FindNextMatchCommand = new RelayCommand<Key?>(
                FindNextMatch, CanFindNextMatch);

            EditSelectedFilterCommand = new RelayCommand(
                EditSelectedFilter, () => AnyFilterSelected);

            RemoveSelectedFilterCommand = new RelayCommand(
                RemoveSelectedFilter, () => AnyFilterSelected);

            EnableAllFiltersCommand = new RelayCommand(
                EnableAllFilters, () => _filters.Count > 0);

            DisableAllFiltersCommand = new RelayCommand(
                DisableAllFilters, () => _filters.Count > 0);

            RemoveAllFiltersCommand = new RelayCommand(
                RemoveAllFilters, () => _filters.Count > 0);

            TextsSource.Rows.CollectionChanged += (s, e) =>
            {
                SaveCurrentLinesCommand.NotifyCanExecuteChanged();
                SelectAllTextsCommand.NotifyCanExecuteChanged();
                GoToLineCommand.NotifyCanExecuteChanged();
            };

            TextsSource.RowSelection!.SelectionChanged += (s, e) =>
            {
                CopyCommand.NotifyCanExecuteChanged();
                CopyWithLinesCommand.NotifyCanExecuteChanged();

                if (e.SelectedItems != null)
                {
                    foreach (var item in e.SelectedItems)
                    {
                        item!.IsSelected = true;
                    }
                }

                if (e.DeselectedItems != null)
                {
                    foreach (var item in e.DeselectedItems)
                    {
                        item!.IsSelected = false;
                    }
                }
            };

            _filters.CollectionChanged += (s, e) =>
            {
                SaveFiltersCommand.NotifyCanExecuteChanged();
                SaveFiltersAsCommand.NotifyCanExecuteChanged();
                EnableAllFiltersCommand.NotifyCanExecuteChanged();
                DisableAllFiltersCommand.NotifyCanExecuteChanged();
                RemoveAllFiltersCommand.NotifyCanExecuteChanged();
            };

            FiltersSource.RowSelection!.SelectionChanged += (s, e) =>
            {
                CopyFiltersCommand.NotifyCanExecuteChanged();
                FindPreviousMatchCommand.NotifyCanExecuteChanged();
                FindNextMatchCommand.NotifyCanExecuteChanged();
                EditSelectedFilterCommand.NotifyCanExecuteChanged();
                RemoveSelectedFilterCommand.NotifyCanExecuteChanged();

                if (e.SelectedItems != null)
                {
                    foreach (var item in e.SelectedItems)
                    {
                        item!.IsSelected = true;
                    }
                }

                if (e.DeselectedItems != null)
                {
                    foreach (var item in e.DeselectedItems)
                    {
                        item!.IsSelected = false;
                    }
                }
            };
        }

        #endregion

        #region Text Operations

        [ObservableProperty]
        Encoding _textEncoding = Encoding.UTF8;
        List<string> _originalTexts = [];
        List<TextLineVM> _allLines = [];
        Dictionary<int, List<int>> _lineMarkers = [];
        IBrush _defaultForeground;
        IBrush _defaultBackground;
        IBrush _excludedTextForeground;

        string? _currentTextFile = null;
        string? CurrentTextFile
        {
            get => _currentTextFile;
            set
            {
                _currentTextFile = value;
                ReloadTextFileCommand.NotifyCanExecuteChanged();
            }
        }

        ObservableCollection<TextLineVM> _texts = [];
        public FlatTreeDataGridSource<TextLineVM> TextsSource { get; private set; }

        [ObservableProperty]
        int _totalLines = 0;

        [ObservableProperty]
        bool _showTextHeaders = false;

        bool AnyTextSelected => TextsSource.RowSelection?.Count > 0;

        void InitTextsSource()
        {
            TextsSource = new FlatTreeDataGridSource<TextLineVM>(_texts)
            {
                Columns =
                {
                    new TextColumn<TextLineVM, int>("#", x => x.LineNumber),

                    new TemplateColumn<TextLineVM>("Marker",
                        new FuncDataTemplate<TextLineVM>(
                            (data, ns) => new ItemsControl()
                            {
                                ItemsPanel = new FuncTemplate<Panel?>(
                                    () => new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        VerticalAlignment = VerticalAlignment.Center,
                                    }),
                                [!ItemsControl.ItemsSourceProperty] = new Binding("Markers"),
                            }),
                        width: new GridLength(0),
                        options: new TemplateColumnOptions<TextLineVM>()
                        {
                            MinWidth = new GridLength(0)
                        }),

                    new TemplateColumn<TextLineVM>("Text",
                        new FuncDataTemplate<TextLineVM>((data, ns) => new TextBlock()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            [!TextBlock.TextProperty] = new Binding("Text"),
                            [!TextBlock.ForegroundProperty] = new Binding("Foreground"),
                            [!TextBlock.BackgroundProperty] = new Binding("DisplayBackground"),
                        }),
                        width: GridLength.Auto),
                }
            };

            TextsSource.RowSelection!.SingleSelect = false;
        }

        public async void OpenTextFile(object param)
        {
            if (param == null)
            {
                var files = await _topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Open Text File",
                        AllowMultiple = false,
                    });

                if (files.Count > 0)
                    LoadTextFile(files[0].Path.LocalPath);
            }
            else
            {
                var filePath = param as string;
                Debug.Assert(filePath != null);
                LoadTextFile(filePath);
            }
        }

        void ReloadTextFile()
        {
            if (_currentTextFile != null)
            {
                LoadTextFile(_currentTextFile);
            }
        }

        async void SaveCurrentLines()
        {
            var file = await _topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions()
                {
                    Title = "Save Current Lines",
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Text")
                        {
                            Patterns = ["*.txt"]
                        }
                    },
                    DefaultExtension = ".txt"
                });

            if (file != null)
            {
                try
                {
                    File.WriteAllLines(file.Path.LocalPath,
                        TextsSource.Rows.Select(r => (r.Model as TextLineVM)!.Text),
                        Encoding.UTF8);

                    AddRecentFile(file.Path.LocalPath);
                }
                catch (Exception ex)
                {
                    await MessageBox(
                        $"Save failed. Exception: {ex.Message}", Icon.Error);
                }
            }
        }

        bool _showOnlyFilteredLines = false;
        public bool IsShowOnlyFilteredLines
        {
            get => _showOnlyFilteredLines;
            set
            {
                if (_showOnlyFilteredLines != value)
                {
                    _showOnlyFilteredLines = value;
                    OnPropertyChanged(nameof(IsShowOnlyFilteredLines));
                    RefreshTexts();
                }
            }
        }

        public void ToggleShowOnlyFilteredLines()
        {
            IsShowOnlyFilteredLines = !IsShowOnlyFilteredLines;
        }

        bool _hideEmptyLines = false;
        public bool IsHideEmptyLines
        {
            get => _hideEmptyLines;
            set
            {
                if (_hideEmptyLines != value)
                {
                    _hideEmptyLines = value;
                    OnPropertyChanged(nameof(IsHideEmptyLines));
                    RefreshTexts();
                }
            }
        }

        public void ToggleHideEmptyLines()
        {
            IsHideEmptyLines = !IsHideEmptyLines;
        }

        void SelectAllTexts()
        {
            Debug.Assert(TextsSource.RowSelection != null);
            TextsSource.RowSelection.Clear();

            TextsSource.RowSelection.BeginBatchUpdate();

            for (int i = 0; i < TextsSource.Rows.Count; i++)
            {
                TextsSource.RowSelection.Select(new IndexPath(i));
            }

            TextsSource.RowSelection.EndBatchUpdate();
        }

        #region Find Text

        FilterRunner? _textFinder = null;

        public async void FindText()
        {
            var vm = new FindVM(TextsSource.RowSelection?.SelectedItem?.Text);
            var wnd = new FindWindow() { DataContext = vm };
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            await wnd.ShowDialog<bool>(owner).ContinueWith(
                result =>
                {
                    vm.Dispose();

                    if (!result.Result)
                        return;

                    _textFinder = new FilterRunner(vm.GetFilter());
                    Dispatcher.UIThread.Post(() => FindNextText(false));
                });
        }

        void FindNextText(bool backward)
        {
            var textsCount = _texts.Count;
            var curSelectedLineIdx = backward ? textsCount : -1;
            var rowSelection = TextsSource.RowSelection;
            if (rowSelection!.SelectedItems.Count > 0)
                curSelectedLineIdx = rowSelection!.SelectedIndex.First();
            var foundLineIdx = -1;
            int step = backward ? -1 : 1;

            var idx = curSelectedLineIdx + step;
            for (int i = 0; i < textsCount; ++i)
            {
                var realIdx = idx % textsCount;
                var textLine = _texts[realIdx];
                if (_textFinder!.Match(textLine.Text, null))
                {
                    foundLineIdx = realIdx;
                    break;
                }

                idx += step;
                if (idx < 0)
                    idx = textsCount - 1;
            }

            if (foundLineIdx >= 0)
            {
                rowSelection!.Clear();
                rowSelection.Select(new IndexPath(foundLineIdx));
            }
            else
            {
                UpdateStatus("Nothing found!", false, 3000);
            }
        }

        #endregion

        async void GoToLine()
        {
            var vm = new GoToVM();
            var wnd = new GoToWindow() { DataContext = vm };
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            await wnd.ShowDialog<bool>(owner).ContinueWith(
                result =>
                {
                    if (!result.Result)
                        return;

                    Dispatcher.UIThread.Post(() =>
                    {
                        TextsSource.RowSelection!.Clear();
                        if (vm.LineNumber <= 1)
                        {
                            TextsSource.RowSelection!.Select(new IndexPath(0));
                            return;
                        }
                        else if (vm.LineNumber >= _texts.Last().LineNumber)
                        {
                            TextsSource.RowSelection!.Select(new IndexPath(_texts.Count - 1));
                            return;
                        }

                        if (IsShowOnlyFilteredLines || IsHideEmptyLines)
                        {
                            int index = 0;
                            foreach (var text in _texts)
                            {
                                if (text.LineNumber >= vm.LineNumber)
                                {
                                    TextsSource.RowSelection!.Select(new IndexPath(index));
                                    break;
                                }
                                ++index;
                            }
                        }
                        else
                        {
                            TextsSource.RowSelection!.Select(new IndexPath(vm.LineNumber - 1));
                        }
                    });
                });
        }

        public void UpdateEncoding(object param)
        {
            var encodingName = (string)param;
            if (encodingName == "Default")
            {
                TextEncoding = Encoding.GetEncoding(
                    CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
            }
            else
            {
                TextEncoding = Encoding.GetEncoding(encodingName);
            }
            Debug.Assert(TextEncoding != null);

            ReloadTextFile();
        }

        #region Markers
        [ObservableProperty]
        bool? _isShowMarkers = null;

        public void ShowMarkers(object param)
        {
            if (param == null)
            {
                IsShowMarkers = null;
                ShowMarkersColumn(
                    _lineMarkers.Values.Any(v => v.Count > 0));
            }
            else
            {
                IsShowMarkers = (bool)param;
                ShowMarkersColumn((bool)param);
            }
        }

        void ShowMarkersColumn(bool visible)
        {
            var markersColumn = TextsSource.Columns[1] as IUpdateColumnLayout;
            if (visible)
            {
                markersColumn?.SetWidth(GridLength.Auto);
            }
            else
            {
                markersColumn?.SetWidth(new GridLength(0));
            }
            ShowTextHeaders = visible;
        }

        public bool CanToggleMarker(object param)
        {
            return AnyTextSelected;
        }

        public void ToggleMarker(object param)
        {
            var marker = Convert.ToInt32(param);
            Debug.Assert(marker > 0 && marker < 10);
            Debug.Assert(TextsSource.RowSelection != null);

            foreach (var textLine in TextsSource.RowSelection.SelectedItems)
            {
                Debug.Assert(textLine != null);
                if (textLine.Markers.Contains(marker))
                {
                    textLine.Markers.Remove(marker);
                    _lineMarkers[textLine.LineNumber - 1].Remove(marker);
                }
                else
                {
                    bool inserted = false;
                    for (int i = 0; i < textLine.Markers.Count; i++)
                    {
                        if (marker < textLine.Markers[i])
                        {
                            textLine.Markers.Insert(i, marker);
                            inserted = true;
                            break;
                        }
                    }

                    if (!inserted)
                        textLine.Markers.Add(marker);

                    var key = textLine.LineNumber - 1;
                    if (_lineMarkers.ContainsKey(key))
                    {
                        _lineMarkers[key].Add(marker);
                    }
                    else
                    {
                        _lineMarkers.Add(key, new List<int> { marker });
                    }
                }
            }

            if (IsShowMarkers == null) // show markers only when in use
                ShowMarkersColumn(_lineMarkers.Values.Any(v => v.Count > 0));

            if (_filters.Any(
                f => f.IsEnabled
                  && f.Modifiers.Contains(FilterModiferType.Marker)
                  && f.Marker == marker))
            {
                FilterTexts();
            }
        }

        #endregion

        public bool CanViewSelectedText(object param)
        {
            return AnyTextSelected;
        }

        public async void ViewSelectedText()
        {
            var wnd = new TextViewerWindow();
            var vm = new TextViewerVM(
                TextsSource.RowSelection!.SelectedItem!.Text, (text) =>
                {
                    wnd.Close();
                    AddFilter(text);
                });
            wnd.DataContext = vm;
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            await wnd.ShowDialog(owner);
        }

        void LoadTextFile(string filePath)
        {
            _dispatcher.BeginInvoke(() =>
            {
                using var streamReader = new StreamReader(
                    filePath, TextEncoding, true, new FileStreamOptions()
                    {
                        Share = FileShare.ReadWrite
                    });

                _originalTexts.Clear();
                while (true)
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        break;
                    _originalTexts.Add(line);
                }

                _currentTextFile = filePath;

                Dispatcher.UIThread.Post(() =>
                {
                    AppTitle = $"{Path.GetFileName(filePath)} - {AppName}";
                    TotalLines = _originalTexts.Count;
                    _lineMarkers.Clear();
                    _ = FilterTexts(false);

                    AddRecentFile(filePath);
                    UpdateStatus($"Source: {filePath}", true);
                });
            });
        }

        void RefreshTexts(bool restoreSelection = true)
        {
            Dispatcher.UIThread.Post(() =>
            {
                int curSelectedLineNo = -1;
                if (restoreSelection)
                {
                    if (TextsSource.RowSelection!.SelectedItems.Count > 0)
                    {
                        curSelectedLineNo = TextsSource.RowSelection!.SelectedItems[0]!.LineNumber;
                        FilteringStarted?.Invoke();
                    }
                }

                _texts.Clear();
                int newSelectionIndex = -1;
                if (IsShowOnlyFilteredLines)
                {
                    foreach (TextLineVM line in _allLines.Where(l => !l.IsExcluded))
                    {
                        if (restoreSelection)
                        {
                            if (newSelectionIndex < 0 && line.LineNumber >= curSelectedLineNo)
                                newSelectionIndex = _texts.Count;
                        }

                        if (!_hideEmptyLines || line.Text.Length > 0)
                            _texts.Add(line);
                    }
                }
                else
                {
                    foreach (TextLineVM line in _allLines)
                    {
                        if (restoreSelection)
                        {
                            if (newSelectionIndex < 0 && line.LineNumber >= curSelectedLineNo)
                                newSelectionIndex = _texts.Count;
                        }

                        if (!_hideEmptyLines || line.Text.Length > 0)
                            _texts.Add(line);
                    }
                }

                if (restoreSelection)
                {
                    if (newSelectionIndex < 0)
                        newSelectionIndex = _texts.Count - 1;

                    if (newSelectionIndex >= 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            TextsSource.RowSelection!.Select(new IndexPath(newSelectionIndex));
                            FilteringEnded?.Invoke();
                        });
                    }
                    else
                    {
                        FilteringEnded?.Invoke();
                    }
                }
            });
        }

        #endregion // Text Operations

        #region Filter Operations

        const string FilterFileExtension = ".flt";

        bool _isBatchProcessingFilters = false;
        ObservableCollection<FilterItemVM> _filters = [];
        string? _currentFilterFile = null;

        public FlatTreeDataGridSource<FilterItemVM> FiltersSource
        { get; private set; }

        bool AnyFilterSelected => FiltersSource.RowSelection?.Count > 0;

        bool AnyEnabledFilterSelected =>
            FiltersSource.RowSelection!.SelectedItems.Any(flt => flt!.IsEnabled);

        void InitFiltersSource()
        {
            var modifierCvt = new ModifierConverter();
            FiltersSource = new FlatTreeDataGridSource<FilterItemVM>(_filters)
            {
                Columns =
                {
                    new TemplateColumn<FilterItemVM>("ID",
                        new FuncDataTemplate<FilterItemVM>((data, ns) => new CheckBox()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Avalonia.Thickness(3,0,0,0),
                            RenderTransform = new ScaleTransform(0.8, 0.8),
                            FontSize = 15,
                            [!ContentControl.ContentProperty] = new Binding("Name"),
                            [!ToggleButton.IsCheckedProperty] = new Binding("IsEnabled"),
                        })),

                    new TemplateColumn<FilterItemVM>("Modifiers",
                        new FuncDataTemplate<FilterItemVM>(
                            (data, ns) => new ItemsControl()
                            {
                                ItemsPanel = new FuncTemplate<Panel?>(
                                    () => new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        VerticalAlignment = VerticalAlignment.Center,
                                    }),
                                ItemTemplate = new FuncDataTemplate<FilterModiferType>(
                                    (_, _) => new ContentControl()
                                    {
                                        [!ContentControl.ContentProperty] = new Binding()
                                        {
                                            Converter = modifierCvt
                                        }
                                    }),
                                [!ItemsControl.ItemsSourceProperty] = new Binding("Modifiers"),
                            })),

                    new TemplateColumn<FilterItemVM>("Pattern",
                        new FuncDataTemplate<FilterItemVM>((data, ns) => new Border()
                            {
                                Child = new TextBlock() {
                                    VerticalAlignment = VerticalAlignment.Center,
                                    [!TextBlock.TextProperty] = new Binding("Pattern"),
                                    [!TextBlock.ForegroundProperty] = new Binding("Foreground"),
                                },
                                [!TextBlock.BackgroundProperty] = new Binding("DisplayBackground"),
                            }), width: GridLength.Star),

                    new TextColumn<FilterItemVM, string>("Description",
                            x => x.Description,
                            new GridLength(0.1, GridUnitType.Star),
                            options: new TextColumnOptions<FilterItemVM>()
                            {
                                MinWidth = new GridLength(100)
                            }),

                    new TextColumn<FilterItemVM, int>("Hits", x => x.Hits,
                            options: new TextColumnOptions<FilterItemVM>()
                            {
                                MinWidth = new GridLength(50)
                            }),
                }
            };
            FiltersSource.RowSelection!.SingleSelect = false;

            _filters.CollectionChanged += (s, e) =>
            {
                var startIdx = Math.Max(0, e.NewStartingIndex);
                if (e.OldStartingIndex >= 0)
                    startIdx = Math.Max(startIdx, e.OldStartingIndex);

                UpdateFilterNames(startIdx);

                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    bool hasActiveFilters = false;
                    foreach (FilterItemVM item in e.OldItems!)
                    {
                        if (item.IsEnabled)
                        {
                            hasActiveFilters = true;
                            break;
                        }
                    }

                    if (!hasActiveFilters) // Avoid unncessary re-filtering
                        return;
                }

                FilterTexts();
            };

            FilterItemVM.EnablementChanged += OnFilterStateChanged;
        }

        public async void AddFilter(object param)
        {
            var vm = new EditFilterVM();
            if (param is string text)
            {
                vm.Text = text;
            }
            else if (TextsSource.RowSelection?.SelectedItem != null)
            {
                vm.Text = TextsSource.RowSelection.SelectedItem.Text;
            }

            var wnd = new EditFilterWindow() { DataContext = vm };
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            await wnd.ShowDialog<bool>(owner).ContinueWith(
                result =>
                {
                    if (!result.Result)
                        return;

                    Dispatcher.UIThread.Post(() =>
                    {
                        _filters.Add(new FilterItemVM(vm.GetModel()));
                    });
                });
        }

        public async Task LoadFilters(object param)
        {
            if (param == null)
            {
                var files = await _topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions()
                    {
                        Title = "Load Filters",
                        FileTypeFilter = new List<FilePickerFileType>() {
                        new FilePickerFileType("Filter")
                        {
                            Patterns = [$"*{FilterFileExtension}"]
                        }
                        },
                        AllowMultiple = false,
                    });

                if (files.Count > 0)
                    LoadFilterFile(files[0].Path.LocalPath);
            }
            else
            {
                LoadFilterFile((string)param);
            }
        }

        async Task SaveFilters()
        {
            if (_currentFilterFile == null)
            {
                SaveFiltersAs();
            }
            else
            {
                await ArchiveFilters(_currentFilterFile);
            }
        }

        async void SaveFiltersAs()
        {
            var file = await _topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions()
                {
                    Title = "Save Filters",
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Filter")
                        {
                            Patterns = [$"*{FilterFileExtension}"]
                        }
                    },
                    DefaultExtension = FilterFileExtension
                });

            if (file != null)
            {
                var filePath = file.Path.LocalPath;
                await ArchiveFilters(filePath);
                AddRecentFilter(filePath);
            }
        }

        async void EditSelectedFilter()
        {
            var filterItem = FiltersSource.RowSelection!.SelectedItem;
            Debug.Assert(filterItem != null);

            var vm = new EditFilterVM(filterItem.ToModel());
            var wnd = new EditFilterWindow() { DataContext = vm };
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            await wnd.ShowDialog<bool>(owner).ContinueWith(
                result =>
                {
                    if (!result.Result)
                        return;

                    Dispatcher.UIThread.Post(() =>
                    {
                        filterItem.Update(vm.GetModel());
                        FilterTexts();
                    });
                });
        }

        void RemoveSelectedFilter()
        {
            Debug.Assert(FiltersSource.RowSelection != null);
            var selIndices = FiltersSource.RowSelection.SelectedIndexes.ToArray();
            for (int i = selIndices.Length - 1; i >= 0; i--)
            {
                foreach (var idx in selIndices[i].Reverse())
                {
                    _filters.RemoveAt(idx);
                }
            }
        }

        void FindPreviousMatch(Key? key)
        {
            FindNextMatch(true, key);
        }

        void FindNextMatch(Key? key)
        {
            FindNextMatch(false, key);
        }

        void EnableAllFilters()
        {
            if (_filters.All(f => f.IsEnabled))
                return;

            _isBatchProcessingFilters = true;
            foreach (var filter in _filters)
            {
                filter.IsEnabled = true;
            }
            _isBatchProcessingFilters = false;

            FilterTexts();
        }

        void DisableAllFilters()
        {
            if (_filters.All(f => !f.IsEnabled))
                return;

            _isBatchProcessingFilters = true;
            foreach (var filter in _filters)
            {
                filter.IsEnabled = false;
            }
            _isBatchProcessingFilters = false;

            FilterTexts();
        }

        void RemoveAllFilters()
        {
            _filters.Clear();
        }

        public bool CanMoveUpFilter(object param)
        {
            var filterItem = param as FilterItemVM;
            if (filterItem == null)
            {
                filterItem = FiltersSource.RowSelection?.SelectedItems.FirstOrDefault();
                if (filterItem == null)
                    return false;
            }

            if (FiltersSource.Items.First() == filterItem)
                return false;

            return true;
        }

        public void MoveUpFilter(object param)
        {
            var filterItem = param as FilterItemVM ??
                FiltersSource.RowSelection?.SelectedItems.First();
            Debug.Assert(filterItem != null);

            MoveFilter(filterItem, true);
        }

        public bool CanMoveDownFilter(object param)
        {
            var filterItem = param as FilterItemVM;
            if (filterItem == null)
            {
                filterItem = FiltersSource.RowSelection?.SelectedItems.LastOrDefault();
                if (filterItem == null)
                    return false;
            }

            if (FiltersSource.Items.Last() == filterItem)
                return false;

            return true;
        }

        public void MoveDownFilter(object param)
        {
            var filterItem = param as FilterItemVM ??
                FiltersSource.RowSelection?.SelectedItems.Last();
            Debug.Assert(filterItem != null);

            MoveFilter(filterItem, false);
        }

        async void LoadFilterFile(string filePath)
        {
            var filters = _persistence.Load<List<FilterModel>>(filePath);
            if (filters == null)
            {
                await MessageBox($"Failed to load {filePath}", Icon.Error);
            }
            else
            {
                _currentFilterFile = filePath;

                Dispatcher.UIThread.Post(() =>
                {
                    _isBatchProcessingFilters = true;
                    _filters.Clear();
                    foreach (var filter in filters)
                    {
                        _filters.Add(new FilterItemVM(filter));
                    }
                    _isBatchProcessingFilters = false;

                    AddRecentFilter(filePath);
                    FilterTexts();
                });
            }
        }

        async Task ArchiveFilters(string filePath)
        {
            var filters = _filters.Select(f => f.ToModel()).ToList();
            try
            {
                _persistence.Save(filters, filePath);
                _currentFilterFile = filePath;
                UpdateStatus("Save filters successfully.", false, 3000);
            }
            catch (Exception ex)
            {
                await MessageBox(
                    $"Save filters failed. Exception: ${ex.Message}", Icon.Error);
            }
        }

        bool _isFilterDirty = false;
        Task? FilterTexts(bool restoreSelection = true)
        {
            if (_isBatchProcessingFilters)
                return null;

            _dispatcher.CancelAllTasks();
            var filters = _filters.ToArray();
            _isFilterDirty = true;

            return _dispatcher.BeginInvoke(() =>
            {
                _isFilterDirty = false;
                Thread.Sleep(20);
                // Prevent unnecessary re-filtering
                if (_isFilterDirty)
                    return;

                _allLines.Clear();
                if (filters.Any(f => f.IsEnabled))
                {
                    foreach (var filter in filters)
                    {
                        filter.ClearHits();
                    }

                    var activeFilters = filters.Where(f => f.IsEnabled)
                        .OrderByDescending(f => f.Modifiers.Contains(FilterModiferType.Excluding))
                        .ToArray();
                    bool hasIncludingFilter = activeFilters.Any(f => !f.IsExcluding);

                    for (int i = 0; i < _originalTexts.Count; ++i)
                    {
                        var text = _originalTexts[i];
                        var line = new TextLineVM(
                                        i + 1,
                                        text,
                                        _defaultForeground,
                                        _defaultBackground);
                        if (_lineMarkers.ContainsKey(i))
                        {
                            foreach (var marker in _lineMarkers[i])
                            {
                                line.Markers.Add(marker);
                            }
                        }

                        bool matchAnyFilter = false;
                        foreach (var filter in activeFilters)
                        {
                            if (filter.Match(text, line.Markers))
                            {
                                filter.AddHitLine(line.LineNumber);
                                if (!matchAnyFilter)
                                {
                                    matchAnyFilter = true;
                                    if (filter.IsExcluding)
                                    {
                                        line.Foreground = _excludedTextForeground;
                                        line.IsExcluded = true;
                                    }
                                    else
                                    {
                                        line.Foreground = filter.Foreground;
                                        line.Background = filter.Background;
                                    }
                                }
                            }
                        }

                        if (!matchAnyFilter && hasIncludingFilter)
                        {
                            // No filter match, set it as excluded
                            line.Foreground = _excludedTextForeground;
                            line.IsExcluded = true;
                        }

                        _allLines.Add(line);
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var filter in filters)
                        {
                            filter.CommitHits();
                        }
                    });
                }
                else
                {
                    for (int i = 0; i < _originalTexts.Count; ++i)
                    {
                        var line = new TextLineVM(
                            i + 1, _originalTexts[i], _defaultForeground, _defaultBackground);
                        if (_lineMarkers.ContainsKey(i))
                        {
                            foreach (var marker in _lineMarkers[i])
                            {
                                line.Markers.Add(marker);
                            }
                        }
                        _allLines.Add(line);
                    }
                }

                RefreshTexts(restoreSelection);
            });
        }

        bool CanFindNextMatch(Key? key)
        {
            return (key != null) || AnyEnabledFilterSelected;
        }

        void FindNextMatch(bool backward, Key? key)
        {
            FilterItemVM? filter = null;
            try
            {
                if (key == null)
                {
                    Debug.Assert(FiltersSource.RowSelection!.SelectedItem != null);
                    filter = FiltersSource.RowSelection!.SelectedItem;
                }
                else
                {
                    var filterName = ((char)(key - Key.A + 'a')).ToString();
                    filter = _filters.FirstOrDefault(f => f.Name == filterName.ToString());
                    if (filter == null)
                        return;
                }

                if (filter.Hits == 0 || (filter.IsExcluding && IsShowOnlyFilteredLines))
                    return;

                var textsSelRow = TextsSource.RowSelection;
                var rowsCnt = TextsSource.Rows.Count;

                int curLineNum = -1;
                int curSelIdx = -1;
                if (backward)
                {
                    curLineNum = 0;
                    curSelIdx = rowsCnt;
                    if (textsSelRow!.SelectedItems.Count > 0)
                    {
                        curLineNum = textsSelRow.SelectedItems.First()!.LineNumber;
                        curSelIdx = textsSelRow.SelectedIndex.First();
                    }
                }
                else
                {
                    if (textsSelRow!.SelectedItems.Count > 0)
                    {
                        curLineNum = textsSelRow.SelectedItems.Last()!.LineNumber;
                        curSelIdx = textsSelRow.SelectedIndex.Last();
                    }
                }

                var nextLineNum = filter.FindNextLineNumber(curLineNum, backward);
                if (IsShowOnlyFilteredLines || IsHideEmptyLines)
                {
                    while (true)
                    {
                        if (nextLineNum == curLineNum)
                            break;

                        bool foundNextLine = false;
                        for (int i = 1; i <= rowsCnt; ++i)
                        {
                            var idx = curSelIdx + i;
                            if (backward)
                            {
                                idx = curSelIdx - i;
                                if (idx < 0)
                                    idx += rowsCnt;
                            }
                            else
                            {
                                if (idx >= rowsCnt)
                                    idx -= rowsCnt;
                            }

                            var row = TextsSource.Rows[idx].Model as TextLineVM;
                            Debug.Assert(row != null);
                            if (row.LineNumber == nextLineNum)
                            {
                                if (IsShowOnlyFilteredLines && row.IsExcluded)
                                    break;

                                if (IsHideEmptyLines && row.Text.Length == 0)
                                    break;

                                textsSelRow.Clear();
                                textsSelRow.Select(idx);
                                foundNextLine = true;
                                break;
                            }
                        }

                        if (foundNextLine)
                            break;

                        nextLineNum = filter.FindNextLineNumber(nextLineNum, backward);
                    }
                }
                else
                {
                    textsSelRow.Clear();
                    textsSelRow.Select(nextLineNum - 1);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        void MoveFilter(FilterItemVM filter, bool upward)
        {
            _isBatchProcessingFilters = true;

            var idx = _filters.IndexOf(filter);
            _filters.RemoveAt(idx);

            var newIdx = upward ? idx - 1 : idx + 1;
            _filters.Insert(newIdx, filter);
            FiltersSource.RowSelection?.Select(new IndexPath(newIdx));

            _isBatchProcessingFilters = false;

            FilterTexts();
        }

        void UpdateFilterNames(int startIndex)
        {
            const int AtoZCharCount = 26;
            for (int i = startIndex; i < Math.Min(AtoZCharCount, _filters.Count); ++i)
            {
                _filters[i].Name = ((char)('a' + i)).ToString();
            }

            for (int i = Math.Max(startIndex, AtoZCharCount); i < _filters.Count; ++i)
            {
                _filters[i].Name = i.ToString();
            }
        }

        void OnFilterStateChanged()
        {
            FindPreviousMatchCommand.NotifyCanExecuteChanged();
            FindNextMatchCommand.NotifyCanExecuteChanged();
            FilterTexts();
        }

        #endregion // Filter Operations

        #region IFileHandler

        public void LoadFile(IStorageFile file, FileType type)
        {
            try
            {
                switch (type)
                {
                    case FileType.Text:
                        LoadTextFile(file.Path.LocalPath);
                        break;

                    case FileType.Filter:
                        LoadFilterFile(file.Path.LocalPath);
                        break;
                }
            }
            catch (Exception e)
            {
                UpdateStatus(e.Message, true);
            }
        }

        #endregion

        #region Copy & Paste

        const string FilterFormatInClipboard = "x-TextAnalyzer-filter";

        void Copy()
        {
            var focusedArea = _focusMonitor.GetFocusedArea();
            switch (focusedArea)
            {
                case FocusedArea.Texts:
                    CopyTexts(false);
                    break;

                case FocusedArea.Filters:
                    CopyFilters();
                    break;

                default:
                    if (AnyTextSelected)
                        CopyTexts(false);
                    break;
            }
        }

        void CopyWithLines()
        {
            CopyTexts(true);
        }

        async void Paste()
        {
            var focusedArea = _focusMonitor.GetFocusedArea();
            switch (focusedArea)
            {
                case FocusedArea.Texts:
                    PasteTexts();
                    break;

                case FocusedArea.Filters:
                    PasteFilters();
                    break;

                default:
                    var formats = await _topLevel.Clipboard!.GetDataFormatsAsync();
                    if (formats.Any(f => f.Identifier == FilterFormatInClipboard))
                    {
                        PasteFilters();
                    }
                    else
                    {
                        PasteTexts();
                    }
                    break;
            }
        }

        async void CopyFilters()
        {
            var data = new DataTransfer();
            var jsonStr = JsonSerializer.Serialize(
                FiltersSource.RowSelection!.SelectedItems.Select(
                    item => item!.ToModel()));
            var fmt = DataFormat.CreateBytesApplicationFormat(FilterFormatInClipboard);
            var item = DataTransferItem.Create(fmt, Encoding.UTF8.GetBytes(jsonStr));
            data.Add(item);

            await _topLevel.Clipboard!.SetDataAsync(data);
        }

        bool CanPasteFilters()
        {
            var formats = _topLevel?.Clipboard?.GetDataFormatsAsync().Result;
            return formats?.Any(f => f.Identifier == FilterFormatInClipboard) == true;
        }

        async void PasteFilters()
        {
            try
            {
                var data = await _topLevel.Clipboard!.TryGetDataAsync();
                var fmt = DataFormat.CreateBytesApplicationFormat(FilterFormatInClipboard);
                var bytes = await data!.TryGetValueAsync(fmt);
                var jsonStr = Encoding.UTF8.GetString(bytes!);
                var filters = JsonSerializer.Deserialize<List<FilterModel>>(jsonStr);
                if (filters == null)
                    return;

                Dispatcher.UIThread.Post(() =>
                {
                    _isBatchProcessingFilters = true;
                    foreach (var filter in filters)
                    {
                        _filters.Add(new FilterItemVM(filter));
                    }
                    _isBatchProcessingFilters = false;

                    FilterTexts();
                });
            }
            catch (Exception ex)
            {
                await MessageBox(
                    $"Paste filters failed. Exception: ${ex.Message}", Icon.Error);
            }
        }

        void CopyTexts(bool withLines)
        {
            Debug.Assert(TextsSource.RowSelection != null);
            var sb = new StringBuilder();
            var ttlCnt = TextsSource.RowSelection.Count;
            for (int i = 0; i < ttlCnt; ++i)
            {
                var line = TextsSource.RowSelection.SelectedItems[i];
                Debug.Assert(line != null);
                if (withLines)
                {
                    sb.Append(line.LineNumber);
                    sb.Append(' ');
                }

                if (i == ttlCnt - 1)
                {
                    sb.Append(line.Text);
                }
                else
                {
                    sb.AppendLine(line.Text);
                }
            }

            _topLevel.Clipboard!.SetTextAsync(sb.ToString());
        }

        async void PasteTexts()
        {
            var texts = await _topLevel.Clipboard!.TryGetTextAsync();
            if (string.IsNullOrEmpty(texts))
                return;

            await _dispatcher.BeginInvoke(() =>
            {
                _originalTexts.Clear();

                foreach (var txt in texts.Split(
                    ["\r\n", "\r", "\n"], StringSplitOptions.None))
                {
                    _originalTexts.Add(txt);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    _currentTextFile = null;
                    AppTitle = $"Clipboard - {AppName}";
                    TotalLines = _originalTexts.Count;
                    _lineMarkers.Clear();
                    _ = FilterTexts(false);
                    UpdateStatus("Source: Clipboard", true);
                });
            });
        }
        #endregion

        #region Zoom

        const double ZoomStep = 0.05;
        const double MinZoomRatio = 0.5;
        const double MaxZoomRatio = 2.0;
        const double DefaultFontSize = 12.0;

        double _zoomRatio = 1.0;
        public double ZoomRatio
        {
            get => _zoomRatio;
            set
            {
                if (_zoomRatio != value)
                {
                    _zoomRatio = value;
                    OnPropertyChanged(nameof(ZoomRatio));
                    TextFontSize = DefaultFontSize * ZoomRatio;

                    ZoomInCommand?.NotifyCanExecuteChanged();
                    ZoomOutCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        [ObservableProperty]
        double _textFontSize = DefaultFontSize;

        void ZoomIn()
        {
            if (ZoomRatio < MaxZoomRatio)
                ZoomRatio += ZoomStep;
        }

        void ZoomOut()
        {
            if (ZoomRatio > MinZoomRatio)
                ZoomRatio -= ZoomStep;
        }

        public void ResetZoom()
        {
            ZoomRatio = 1.0;
        }

        #endregion

        #region Archived Items

        const int MaxRecentItemCount = 10;

        string _preferenceArchivePath = string.Empty;
        string _recentFilesArchivePath = string.Empty;
        string _recentFiltersArchivePath = string.Empty;

        #region  IRecentFileManager

        public event Action<FileType, string> OnRecentFileAdded;
        public event Action<FileType, string> OnRecentFileRemoved;
        public event Action<FileType> OnRecentFileCleared;

        public IEnumerable<string> GetRecentFiles(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Text:
                    return RecentFiles.Select(rf => rf.Name);

                case FileType.Filter:
                    return RecentFilters.Select(rf => rf.Name);

                default:
                    throw new Exception("Unknown file type");
            }
        }

        #endregion

        public ObservableCollection<RecentItemVM> RecentFiles { get; private set; }
        public ObservableCollection<RecentItemVM> RecentFilters { get; private set; }

        public void ClearRecentFiles()
        {
            RecentFiles.Clear();
        }

        public void ClearRecentFilters()
        {
            RecentFilters.Clear();
        }

        void LoadArchivedtems()
        {
            _persistence = new JsonFilePersistence();

            RecentFiles = new ObservableCollection<RecentItemVM>();
            RecentFilters = new ObservableCollection<RecentItemVM>();
            RecentItemVM.ItemToBeRemoved += OnRecentItemToBeRemoved;

            var parentFolder = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(parentFolder, "TextAnalyzer");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _preferenceArchivePath = Path.Combine(appFolder, "Preference.json");
            _preferences = _persistence.Load<Preferences>(_preferenceArchivePath) ?? new();
            ZoomRatio = _preferences.ZoomRatio;
            IsShowOnlyFilteredLines = _preferences.IsShowOnlyFilteredLines;
            IsHideEmptyLines = _preferences.IsHideEmptyLines;
            _defaultForeground = new SolidColorBrush(
                _preferences.DefaultForegroundColor);
            _defaultBackground = new SolidColorBrush(
                _preferences.DefaultBackgroundColor);
            _excludedTextForeground = new SolidColorBrush(
                _preferences.DefaultExcludedTextColor);

            _recentFilesArchivePath = Path.Combine(appFolder, "RecentFiles");
            _recentFiltersArchivePath = Path.Combine(appFolder, "RecentFilters");

            var recentFiles =
                _persistence.Load<List<string>>(_recentFilesArchivePath);
            if (recentFiles != null)
            {
                foreach (var file in recentFiles)
                {
                    RecentFiles.Add(new RecentItemVM(file));
                }
            }

            var recentFilters =
                _persistence.Load<List<string>>(_recentFiltersArchivePath);
            if (recentFilters != null)
            {
                foreach (var file in recentFilters)
                {
                    RecentFilters.Add(new RecentItemVM(file));
                }
            }

            MonitorRecentItemChanges();
        }

        void OnRecentItemToBeRemoved(RecentItemVM recentItem)
        {
            if (!RecentFiles.Remove(recentItem))
                RecentFilters.Remove(recentItem);
        }

        void MonitorRecentItemChanges()
        {
            var isMac = OperatingSystem.IsMacOS();

            RecentFiles.CollectionChanged += (s, e) =>
            {
                if (isMac)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        OnRecentFileCleared?.Invoke(FileType.Text);
                    }
                    else
                    {
                        if (e.NewItems?.Count > 0)
                        {
                            foreach (var item in e.NewItems)
                            {
                                OnRecentFileAdded?.Invoke(FileType.Text, ((RecentItemVM)item).Name);
                            }
                        }

                        if (e.OldItems?.Count > 0)
                        {
                            foreach (var item in e.OldItems)
                            {
                                OnRecentFileRemoved?.Invoke(FileType.Text, ((RecentItemVM)item).Name);
                            }
                        }
                    }
                }

                _persistence.Save(
                    RecentFiles.Select(rf => rf.Name), _recentFilesArchivePath);
            };

            RecentFilters.CollectionChanged += (s, e) =>
            {
                if (isMac)
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        OnRecentFileCleared?.Invoke(FileType.Filter);
                    }
                    else
                    {
                        if (e.NewItems?.Count > 0)
                        {
                            foreach (var item in e.NewItems)
                            {
                                OnRecentFileAdded?.Invoke(FileType.Filter, ((RecentItemVM)item).Name);
                            }
                        }

                        if (e.OldItems?.Count > 0)
                        {
                            foreach (var item in e.OldItems)
                            {
                                OnRecentFileRemoved?.Invoke(FileType.Filter, ((RecentItemVM)item).Name);
                            }
                        }
                    }
                }

                _persistence.Save(
                    RecentFilters.Select(rf => rf.Name), _recentFiltersArchivePath);
            };
        }

        void AddRecentFile(string filePath)
        {
            Debug.Assert(File.Exists(filePath));

            foreach (var file in RecentFiles)
            {
                if (file.Name == filePath)
                {
                    RecentFiles.Remove(file);
                    break;
                }
            }

            if (RecentFiles.Count >= MaxRecentItemCount)
                RecentFiles.RemoveAt(RecentFiles.Count - 1);

            RecentFiles.Insert(0, new RecentItemVM(filePath));
        }

        void AddRecentFilter(string filePath)
        {
            Debug.Assert(File.Exists(filePath));

            foreach (var filter in RecentFilters)
            {
                if (filter.Name == filePath)
                {
                    RecentFilters.Remove(filter);
                    break;
                }
            }

            if (RecentFilters.Count >= MaxRecentItemCount)
                RecentFilters.RemoveAt(RecentFilters.Count - 1);

            RecentFilters.Insert(0, new RecentItemVM(filePath));
        }

        void SavePerference()
        {
            _preferences.IsShowOnlyFilteredLines = IsShowOnlyFilteredLines;
            _preferences.IsHideEmptyLines = IsHideEmptyLines;
            _preferences.ZoomRatio = ZoomRatio;
            _persistence.Save(_preferences, _preferenceArchivePath);
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            RecentItemVM.ItemToBeRemoved -= OnRecentItemToBeRemoved;
            _dispatcher.CancelAllTasks();
            SavePerference();

            FilterItemVM.EnablementChanged -= OnFilterStateChanged;
        }
        #endregion

        #region Help

        public async void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("- Open a text file by \"Open with\" or drag & drop to a running Text Analyzer app.");
            sb.AppendLine("- Create some filters based on text or marker (Ctrl+1~9 or right click a text line to toggle the marker).");
            sb.AppendLine("- You can toggle \"Show Only Filtered Lines\" (Ctrl+H) as needed.");
            sb.AppendLine("- Press \"a~z\" or \"F8\" to locate next match of the corresponding filter.");
            sb.AppendLine("- Move up/down the filters to change priority (drag & drop also works on Windows).");
            sb.AppendLine("- Double click the text line to view in an independent window which supports \"Find\" and \"Format\" functions (context menu).");
            sb.AppendLine("- Press \"F5\" to reload the text file.");
            sb.AppendLine("- Save the filter ([Ctrl/Cmd]+Shift+S) for next time use.");
            sb.AppendLine("- Check other usages from app menu or context menu.");
            await MessageBox(sb.ToString(), Icon.Question);
        }

        public void ShowAbout()
        {
            var wnd = new AboutWindow();
            var owner = _topLevel as Window;
            Debug.Assert(owner != null);
            wnd.ShowDialog(owner);
        }

        #endregion

        void UpdateStatus(string status, bool clear, int? timeoutMs = null)
        {
            if (clear)
                _statusList.Clear();

            _statusList.Add(status);
            OnPropertyChanged(nameof(Status));

            if (timeoutMs != null)
            {
                BackgroundDispatcher.NewInstance.BeginInvoke(() =>
                {
                    Thread.Sleep(timeoutMs.Value);
                    Dispatcher.UIThread.Post(() =>
                    {
                        _statusList.Remove(status);
                        OnPropertyChanged(nameof(Status));
                    });
                });
            }
        }

        async Task MessageBox(string message, Icon icon = Icon.None)
        {
            if (_topLevel is Window window)
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    AppName, message, icon: icon).ShowWindowDialogAsync(window);
            }
            else
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    AppName, message, icon: icon).ShowWindowAsync();
            }
        }

    }
}
