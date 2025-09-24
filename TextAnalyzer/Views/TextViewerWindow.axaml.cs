using Avalonia.Controls;
using Avalonia.Input;

namespace TextAnalyzer;

public partial class TextViewerWindow : Window
{
    public TextViewerWindow()
    {
        InitializeComponent();

        Loaded += (s, e) => _TextToFind.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F3)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if (_Find.Command!.CanExecute(null) == true)
                    _Find.Command.Execute(true);

                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.None)
            {
                if (_Find.Command!.CanExecute(null) == true)
                    _Find.Command.Execute(false);

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            this.Close();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

}