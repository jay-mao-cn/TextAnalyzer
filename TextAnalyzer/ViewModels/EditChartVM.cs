using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TextAnalyzer.Helpers;
using TextAnalyzer.Interfaces;
using TextAnalyzer.Models;

namespace TextAnalyzer.ViewModels
{
    internal partial class EditChartVM : ViewModelBase, IDisposable
    {
        const string ChartConfigFileExtension = ".ccf";
        const int MaxRecentItemCount = 10;

        TopLevel _topLevel;
        IFilePersistence _persistence = new JsonFilePersistence();
        string _recentConfigsArchivePath = string.Empty;

        [ObservableProperty]
        string _title = string.Empty;

        string _filter = string.Empty;
        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    OnPropertyChanged(nameof(Filter));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        [ObservableProperty]
        bool _isCaseSensitive = false;
        [ObservableProperty]
        bool _isRegularExpression = false;
        [ObservableProperty]
        bool _isLogicOperation = false;

        string _key = string.Empty;
        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value;
                    OnPropertyChanged(nameof(Key));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public IEnumerable<string> EndChars { get; private set; }
        string _endChar = string.Empty;
        public string EndChar
        {
            get => _endChar;
            set
            {
                if (_endChar != value)
                {
                    _endChar = value;
                    OnPropertyChanged(nameof(EndChar));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        [ObservableProperty]
        int _labelStartPos = 0;
        [ObservableProperty]
        int _labelLength = 0;

        public int StartLine { get; set; } = 0;
        public int EndLine { get; set; } = -1;

        public bool IsValid => !string.IsNullOrEmpty(Filter)
            && !string.IsNullOrEmpty(Key)
            && !string.IsNullOrEmpty(EndChar);

        public ObservableCollection<RecentItemVM> RecentChartConfigs { get; private set; }

        internal bool NeedLabel => LabelStartPos >= 0 && LabelLength > 0;

        IReadOnlyList<FilePickerFileType>? FileTypeFilters =>
            [
                new FilePickerFileType("Chart Config")
                {
                    Patterns = [$"*{ChartConfigFileExtension}"]
                }
            ];

        internal EditChartVM(TopLevel topLevel)
        {
            _topLevel = topLevel;
            Title = string.Empty;
            Filter = string.Empty;
            Key = string.Empty;
            EndChars = [",", ";", ".", "Space", "EOF"];
            EndChar = ",";
            RecentChartConfigs = new ObservableCollection<RecentItemVM>();

            RecentItemVM.ItemToBeRemoved += OnRecentItemToBeRemoved;

            Dispatcher.UIThread.Post(() =>
            {
                LoadRecentConfigs();

                RecentChartConfigs.CollectionChanged += (s, e) =>
                {
                    _persistence.Save(
                        RecentChartConfigs.Select(rf => rf.Name), _recentConfigsArchivePath);
                };
            });
        }

        internal FilterBase GetFilter()
        {
            Debug.Assert(IsValid);
            return new FilterBase()
            {
                FilterType = FilterType.Text,
                FilterText = Filter,
                IsCaseSensitive = IsCaseSensitive,
                IsRegularExpression = IsRegularExpression,
                IsLogicOperation = IsLogicOperation,
            };
        }

        public async void SaveChartConfig()
        {
            try
            {
                var file = await _topLevel.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions()
                    {
                        Title = "Save Chart Config",
                        FileTypeChoices = FileTypeFilters,
                        DefaultExtension = ChartConfigFileExtension
                    });

                if (file != null)
                {
                    var filePath = file.Path.LocalPath;
                    _persistence.Save(ToModel(), filePath);
                    AddRecentConfig(filePath);
                }
            }
            catch (Exception ex)
            {
                await MessageBoxHelper.Show(_topLevel,
                   $"Save chart config failed. Exception: ${ex.Message}",
                   icon: Icon.Error);
            }
        }

        public async void LoadChartConfig(object param)
        {
            if (param == null)
            {
                var files = await _topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions()
                    {
                        Title = "Load Chart Config",
                        FileTypeFilter = FileTypeFilters,
                        AllowMultiple = false,
                    });

                if (files.Count > 0)
                    LoadChartConfigFile(files[0].Path.LocalPath);
            }
            else
            {
                var filePath = param as string;
                Debug.Assert(filePath != null);
                LoadChartConfigFile(filePath);
            }
        }

        async void LoadChartConfigFile(string filePath)
        {
            var chartConfig = _persistence.Load<ChartConfigModel>(filePath);
            if (chartConfig == null)
            {
                await MessageBoxHelper.Show(
                    _topLevel, $"Failed to load {filePath}", Icon.Error);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    FromModel(chartConfig);
                    AddRecentConfig(filePath);
                });
            }
        }

        void LoadRecentConfigs()
        {
            var appFolder = AppStorageHelper.GetAppArchiveDir();
            _recentConfigsArchivePath = Path.Combine(appFolder, "RecentChartConfigs");

            var recentConfigs =
                _persistence.Load<List<string>>(_recentConfigsArchivePath);
            if (recentConfigs != null)
            {
                foreach (var file in recentConfigs)
                {
                    RecentChartConfigs.Add(new RecentItemVM(file));
                }
            }
        }

        void AddRecentConfig(string filePath)
        {
            Debug.Assert(File.Exists(filePath));

            foreach (var filter in RecentChartConfigs)
            {
                if (filter.Name == filePath)
                {
                    RecentChartConfigs.Remove(filter);
                    break;
                }
            }

            if (RecentChartConfigs.Count >= MaxRecentItemCount)
                RecentChartConfigs.RemoveAt(RecentChartConfigs.Count - 1);

            RecentChartConfigs.Insert(0, new RecentItemVM(filePath));
        }

        void OnRecentItemToBeRemoved(RecentItemVM recentItem)
        {
            RecentChartConfigs.Remove(recentItem);
        }

        ChartConfigModel ToModel()
        {
            var model = new ChartConfigModel();
            model.Title = Title;
            model.Filter = Filter;
            model.IsCaseSensitive = IsCaseSensitive;
            model.IsRegularExpression = IsRegularExpression;
            model.IsLogicOperation = !IsRegularExpression && IsLogicOperation;
            Debug.Assert(!(model.IsRegularExpression && model.IsLogicOperation));
            model.Key = Key;
            model.EndChar = EndChar;
            model.LabelStartPos = LabelStartPos;
            model.LabelLength = LabelLength;

            return model;
        }

        void FromModel(ChartConfigModel model)
        {
            Title = model.Title;
            Filter = model.Filter;
            IsCaseSensitive = model.IsCaseSensitive;
            IsRegularExpression = model.IsRegularExpression;
            IsLogicOperation = model.IsLogicOperation;
            Key = model.Key;
            EndChar = model.EndChar;
            LabelStartPos = model.LabelStartPos;
            LabelLength = model.LabelLength;
        }

        #region IDisposable

        public void Dispose()
        {
            RecentItemVM.ItemToBeRemoved -= OnRecentItemToBeRemoved;
        }

        #endregion
    }
}
