using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TextAnalyzer;

public partial class GoToWindow : Window
{
    public GoToWindow()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            _LineNum.Focus();
        };
    }

    void ButtonOK_Click(object? sender, RoutedEventArgs args)
    {
        Close(true);
    }

    void ButtonCancel_Click(object? sender, RoutedEventArgs args)
    {
        Close(false);
    }
}