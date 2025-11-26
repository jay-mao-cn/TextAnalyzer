using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TextAnalyzer;

public partial class FindWindow : Window
{
    public FindWindow()
    {
        InitializeComponent();
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