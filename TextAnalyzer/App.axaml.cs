using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using System.Runtime.InteropServices;
using TextAnalyzer.Mac;
using TextAnalyzer.ViewModels;
using TextAnalyzer.Views;

namespace TextAnalyzer
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                var mainWindow = new MainWindow();
                var vm = new MainWindowVM(mainWindow, mainWindow);
                if (desktop.Args?.Length == 1)
                {
                    Dispatcher.UIThread.Post(() => vm.OpenTextFile(desktop.Args[0]));
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    InitializeMacOs();
                }
                mainWindow.DataContext = vm;
                desktop.MainWindow = mainWindow;

                Dispatcher.UIThread.UnhandledException += (_, e) =>
                {
                    MessageBoxManager.GetMessageBoxStandard(
                        "Text Analyzer",
                        $"Unhandled exception: {e.Exception.Message}",
                        icon: Icon.Error).ShowWindowDialogAsync(mainWindow);
                    e.Handled = true; // Prevent app crash
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        
        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
        
        // Need a member variable to keep the callback alive
        MacOpenFileDelegate? _macOpenFileDelegate;
        void InitializeMacOs()
        {
            try
            {
                _macOpenFileDelegate = new MacOpenFileDelegate(HandleMacOsFileOpen);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize macOS delegate: {ex.Message}");
            }
        }

        private void HandleMacOsFileOpen(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                        {
                            MainWindow: MainWindow { DataContext: MainWindowVM vm }
                        })
                    {
                        vm.OpenTextFile(filePath);
                    }
                });
            }
        }
    }
}