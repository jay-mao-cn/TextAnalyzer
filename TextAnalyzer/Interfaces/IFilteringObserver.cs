using System;

namespace TextAnalyzer.Interfaces
{
    internal interface IFilteringObserver
    {
        event Action FilteringStarted;
        event Action FilteringEnded;
    }
}
