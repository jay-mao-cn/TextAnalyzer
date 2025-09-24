using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace TextAnalyzer.Helpers
{
    internal class InitMenuCommandBehavior : AvaloniaObject, IBehavior
    {
        public AvaloniaObject? AssociatedObject { get; private set; }

        public void Attach(AvaloniaObject? associatedObject)
        {
            AssociatedObject = associatedObject;
            if (associatedObject is ContextMenu menu)
            {
                // As a workaround to initialize the Command binding
                menu.Open(null);
                menu.Close();
            }
        }

        public void Detach()
        {
            AssociatedObject = null;
        }
    }
}
