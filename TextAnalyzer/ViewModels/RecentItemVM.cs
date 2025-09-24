using System;

namespace TextAnalyzer.ViewModels
{
    internal class RecentItemVM
    {
        public static event Action<RecentItemVM>? ItemToBeRemoved;

        public string Name { get; private set; }

        public RecentItemVM(string name)
        {
            Name = name;
        }

        public void RemoveItem()
        {
            ItemToBeRemoved?.Invoke(this);
        }
    }
}
