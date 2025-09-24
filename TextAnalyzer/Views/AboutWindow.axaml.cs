using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;

namespace TextAnalyzer;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    void On_SrcCodeLink_Click(object? sender, RoutedEventArgs e)
    {
        var url = (sender as Button)!.Content as string;

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", url!);
        }
    }

    void On_OK_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}