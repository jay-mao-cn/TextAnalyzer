using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TextAnalyzer;

public partial class EditChartWindow : Window
{
    public EditChartWindow()
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