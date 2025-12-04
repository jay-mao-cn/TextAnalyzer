using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace TextAnalyzer.Helpers
{
    internal class MessageBoxHelper
    {
        const string AppName = "Text Analyzer";

        internal static async Task Show(
            TopLevel topLevel, string message, Icon icon = Icon.None)
        {
            if (topLevel is Window window)
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
