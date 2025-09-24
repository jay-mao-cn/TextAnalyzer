using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia.Data;
using TextAnalyzer.Helpers;
using TextAnalyzer.Interfaces;
using TextAnalyzer.Models;

namespace TextAnalyzer.Views
{
    partial class MainWindow : Window, IFocusMonitor
    {
        bool _filteringStarted = false;
        double? _oldSelTextOffsetY = null;

        public MainWindow()
        {
            InitializeComponent();
            if (OperatingSystem.IsMacOS())
                _AppMenu.IsVisible = false;

            Loaded += (s, e) =>
            {
                if (OperatingSystem.IsMacOS())
                    InitAppMenuForMac();

                ForceCommandInitialization();

                if (DataContext is IFilteringObserver observer)
                {
                    observer.FilteringStarted += OnFilteringStarted;
                    observer.FilteringEnded += OnFilteringEnded;
                }

                _Texts.RowSelection!.SelectionChanged += (s, e) =>
                {
                    if (!_filteringStarted && e.SelectedItems.Count == 1)
                    {
                        _Texts.RowsPresenter?.BringIntoView(
                            e.SelectedIndexes[0].Last());
                    }
                };
            };

            Closed += (s, e) => { (DataContext as IDisposable)?.Dispose(); };

            _Texts.AddHandler(DragDrop.DropEvent, On_Texts_Drop);
            _Texts.AddHandler(DragDrop.DragOverEvent, On_Texts_DragOver);
            _Texts.AddHandler(PointerWheelChangedEvent,
                On_Texts_PointerWheelChanged,
                RoutingStrategies.Tunnel);
        }

        #region IFocusMonitor

        public FocusedArea GetFocusedArea()
        {
            if (_Texts.IsFocused)
            {
                return FocusedArea.Texts;
            }
            else if (_Filters.IsFocused)
            {
                return FocusedArea.Filters;
            }
            else if (FocusManager?.GetFocusedElement() is TreeDataGridCell cell)
            {
                var container = FindContainer(cell);
                if (container == _Texts)
                {
                    return FocusedArea.Texts;
                }
                else if (container == _Filters)
                {
                    return FocusedArea.Filters;
                }
                else
                {
                    return FocusedArea.None;
                }
            }
            else
            {
                return FocusedArea.None;
            }
        }

        #endregion

        #region Mac Specific

        void InitAppMenuForMac()
        {
            var nativeMenu = NativeMenu.GetMenu(this);

            // Find the "File" menu
            var fileMenu = nativeMenu?.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("File") == true);

            var recentFileMenu = fileMenu!.Menu!.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Recent Files") == true);
            Debug.Assert(recentFileMenu != null);

            var recentFilterMenu = fileMenu!.Menu!.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Recent Filters") == true);
            Debug.Assert(recentFilterMenu != null);

            if (DataContext is IRecentFileManager recentFileManager)
            {
                var recentFiles = recentFileManager.GetRecentFiles(FileType.Text);
                foreach (var file in recentFiles)
                {
                    AddRecentItem(recentFileMenu!, _OpenTextFile.Command!, file);
                }

                recentFileMenu!.Menu!.Items.Add(new NativeMenuItemSeparator() { Header = "-" });
                var clearRecentFiles = new NativeMenuItem() { Header = "Clear Recent Files List" };
                clearRecentFiles.Bind(NativeMenuItem.CommandProperty,
                    new Binding("ClearRecentFiles") { Source = DataContext });
                recentFileMenu.Menu.Items.Add(clearRecentFiles);

                var recentFilters = recentFileManager.GetRecentFiles(FileType.Filter);
                foreach (var filter in recentFilters)
                {
                    AddRecentItem(recentFilterMenu, _LoadFilters.Command!, filter);
                }

                recentFilterMenu!.Menu!.Items.Add(new NativeMenuItemSeparator() { Header = "-" });
                var clearRecentFilters = new NativeMenuItem() { Header = "Clear Recent Filters List" };
                clearRecentFilters.Bind(NativeMenuItem.CommandProperty, 
                    new Binding("ClearRecentFilters") { Source = DataContext });
                recentFilterMenu.Menu.Items.Add(clearRecentFilters);

                recentFileManager.OnRecentFileAdded += (type, name) =>
                {
                    switch (type)
                    {
                        case FileType.Text:
                            AddRecentItem(recentFileMenu!, _OpenTextFile.Command!, name, true);
                            break;

                        case FileType.Filter:
                            AddRecentItem(recentFilterMenu!, _LoadFilters.Command!, name, true);
                            break;

                        default:
                            break;
                    }
                };

                recentFileManager.OnRecentFileRemoved += (type, name) =>
                {
                    switch (type)
                    {
                        case FileType.Text:
                            RemoveRecentItem(recentFileMenu, name);
                            break;

                        case FileType.Filter:
                            RemoveRecentItem(recentFilterMenu, name);
                            break;

                        default:
                            break;
                    }
                };

                recentFileManager.OnRecentFileCleared += (type) =>
                {
                    switch (type)
                    {
                        case FileType.Text:
                            ClearRecentItems(recentFileMenu);
                            break;

                        case FileType.Filter:
                            ClearRecentItems(recentFilterMenu);
                            break;

                        default:
                            break;
                    }
                };
            }

            var editMenu = nativeMenu?.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Edit") == true);

            var copyMenuItem = editMenu!.Menu!.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Copy") == true);
            Debug.Assert(copyMenuItem != null);

            var pasteMenuItem = editMenu!.Menu!.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Paste") == true);
            Debug.Assert(pasteMenuItem != null);

            var pasteFiltersMenuItem = editMenu!.Menu!.Items.OfType<NativeMenuItem>()
                .FirstOrDefault(item => item.Header?.Equals("Paste Filters") == true);
            Debug.Assert(pasteFiltersMenuItem != null);

            editMenu.Menu.Opening += (s, e) =>
            {
                (copyMenuItem.Command as IRelayCommand)?.NotifyCanExecuteChanged();
                (pasteMenuItem.Command as IRelayCommand)?.NotifyCanExecuteChanged();
                (pasteFiltersMenuItem.Command as IRelayCommand)?.NotifyCanExecuteChanged();
            };
        }

        void AddRecentItem(
            NativeMenuItem parent, ICommand command, string itemName, bool atHead = false)
        {
            var newItem = new NativeMenuItem(itemName)
            {
                Command = command,
                CommandParameter = itemName,
            };

            if (atHead)
            {
                parent.Menu!.Items.Insert(0, newItem);
            }
            else
            {
                parent.Menu!.Items.Add(newItem);
            }
        }

        void RemoveRecentItem(NativeMenuItem parent, string itemName)
        {
            var itemToRemove = parent.Menu!.Items
                .OfType<NativeMenuItem>()
                .FirstOrDefault(i => i.Header == itemName);
            if (itemToRemove != null)
                parent.Menu.Items.Remove(itemToRemove);
        }

        void ClearRecentItems(NativeMenuItem parent)
        {
            // Keep last two items (separator and clear)
            for (int i = parent.Menu!.Items.Count - 3; i >= 0; i--)
            {
                parent.Menu.Items.RemoveAt(i);
            }
        }

        #endregion

        void ForceCommandInitialization()
        {
            // In avalonia, menu is delay initialized, so the command and gesture
            // inside a menu cannot work until the menu is first opened.

            if (_AppMenu.IsVisible)
            {
                foreach (var item in _AppMenu.Items)
                {
                    if (item is MenuItem mi)
                        RecursivelyInitMenuItemCmdAndGesture(mi);
                }
            }

            Interaction.SetBehaviors(_TextItemMenu, new BehaviorCollection
            {
                new InitMenuCommandBehavior()
            });

            foreach (var item in _TextItemMenu.Items)
            {
                if (item is MenuItem mi)
                    RecursivelyInitMenuItemCmdAndGesture(mi);
            }

            Interaction.SetBehaviors(_FilterItemMenu, new BehaviorCollection
            {
                new InitMenuCommandBehavior()
            });

            foreach (var item in _FilterItemMenu.Items)
            {
                if (item is MenuItem mi)
                    RecursivelyInitMenuItemCmdAndGesture(mi);
            }
        }

        void RecursivelyInitMenuItemCmdAndGesture(MenuItem menuItem)
        {
            if (menuItem == null)
                return;

            foreach (var item in menuItem.Items)
            {
                if (item is MenuItem mi)
                    RecursivelyInitMenuItemCmdAndGesture(mi);
            }

            var cmd = menuItem.Command;
            if (cmd != null && menuItem.InputGesture is KeyGesture kg)
            {
                var keyBinding = new KeyBinding
                {
                    Command = cmd,
                    Gesture = kg
                };
                if (menuItem.CommandParameter != null)
                    keyBinding.CommandParameter = menuItem.CommandParameter;

                this.KeyBindings.Add(keyBinding);
            }
        }

        TreeDataGrid? FindContainer(TreeDataGridCell cell)
        {
            ILogical logicalItem = cell;
            while (true)
            {
                var parent = logicalItem.GetLogicalParent();
                if (parent is TreeDataGrid treeDataGrid)
                {
                    return treeDataGrid;
                }
                else if (parent == null)
                {
                    return null;
                }

                logicalItem = parent;
            }
        }

        double? GetSelectedTextOffsetY(bool bringToView = false)
        {
            if (_Texts.RowSelection!.SelectedIndexes.Count == 0)
                return null;

            var selIdx = _Texts.RowSelection!.SelectedIndex.First();
            Control? row = _Texts.TryGetRow(selIdx);
            if (row == null && bringToView)
            {
                row = _Texts.RowsPresenter?.BringIntoView(selIdx);
                if (row == null)
                    return null;
            }

            var itemPos = row!.TranslatePoint(new Point(0, 0), (_Texts.Scroll as Visual)!);

            return itemPos?.Y; // distance from top of scroll viewer's viewport
        }

        void OnFilteringStarted()
        {
            _filteringStarted = true;
            _oldSelTextOffsetY = GetSelectedTextOffsetY();
        }

        void OnFilteringEnded()
        {
            _filteringStarted = false;

            if (_oldSelTextOffsetY == null)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                var curOffsetY = GetSelectedTextOffsetY(true);
                Debug.Assert(curOffsetY != null);
                if (curOffsetY == null)
                    return;

                // Adjust offset so item stays at same place
                double delta = curOffsetY.Value - _oldSelTextOffsetY.Value;
                _Texts.Scroll!.Offset = new Vector(_Texts.Scroll.Offset.X, _Texts.Scroll.Offset.Y + delta);
            }, DispatcherPriority.Background);
        }

        void On_Texts_DragOver(object? sender, DragEventArgs e)
        {
            // Only allow files to be dropped
            e.DragEffects = e.Data.Contains(DataFormats.Files)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        void On_Texts_Drop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files != null)
                {
                    if (DataContext is IFileHandler handler)
                    {
                        foreach (var file in files)
                        {
                            if (file is IStorageFile storageFile)
                            {
                                handler.LoadFile(storageFile, FileType.Text);
                                // can only load one file
                                break;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_AppMenu.IsOpen || _TextItemMenu.IsOpen || _FilterItemMenu.IsOpen)
            {
                base.OnKeyDown(e);
                return;
            }

            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    if (_PreviousMatch.Command?.CanExecute(e.Key) == true)
                        _PreviousMatch.Command.Execute(e.Key);
                    e.Handled = true;
                }
                else if (e.KeyModifiers == KeyModifiers.None)
                {
                    if (_NextMatch.Command?.CanExecute(e.Key) == true)
                        _NextMatch.Command.Execute(e.Key);
                    e.Handled = true;
                }
            }
            else if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    var cmd = (_MarkerMenu.Items[0] as MenuItem)?.Command;
                    if (cmd?.CanExecute(null) == true)
                        cmd.Execute(e.Key - Key.D0);
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);
        }

        void On_Texts_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                if (e.Delta.Y < 0)
                {
                    _ZoomOut.Command?.Execute(null);
                }
                else
                {
                    _ZoomIn.Command?.Execute(null);
                }

                e.Handled = true;
            }
        }

        void On_Texts_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_ViewText.Command?.CanExecute(null) == true)
                _ViewText.Command.Execute(null);
        }

        void On_Filters_KeyUp(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Delete:
                    if (_RemoveFilter.Command?.CanExecute(null) == true)
                        _RemoveFilter.Command.Execute(null);
                    args.Handled = true;
                    break;
            }
        }

        void On_Filters_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_EditFilter.Command?.CanExecute(null) == true)
                _EditFilter.Command.Execute(null);
        }
    }
}