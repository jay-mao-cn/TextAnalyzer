using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace TextAnalyzer;

public partial class EditFilterWindow : Window
{
    public EditFilterWindow()
    {
        InitializeComponent();

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Escape),
            Command = new RelayCommand(() => Close(false))
        });

        Loaded += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _Text.Focus();
                _Text.SelectAll();
            });
        };
    }

    void ButtonOK_Click(object sender, RoutedEventArgs args)
    {
        this.Close(true);
    }

    void ButtonCancel_Click(object sender, RoutedEventArgs args)
    {
        this.Close(false);
    }
}