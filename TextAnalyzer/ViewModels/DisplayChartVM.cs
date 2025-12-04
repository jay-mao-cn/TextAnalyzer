using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using System.Collections.Generic;
using System.Linq;

namespace TextAnalyzer.ViewModels
{
    internal class DisplayChartVM
    {
        public string Title { get; private set; }
        public ISeries[] Series { get; private set; } = [];
        public ICartesianAxis[] XAxes { get; private set; } = [];

        internal DisplayChartVM(
            string title, IEnumerable<double> values, IEnumerable<string>? labels = null)
        {
            Title = title.Length > 0 ? title : "Chart";
            Series =
            [
                new LineSeries<double>
                {
                    Values = values.ToArray(),
                }
            ];

            if (labels != null)
            {
                XAxes =
                [
                    new XamlAxis
                    {
                        Labels = labels.ToArray(),
                    }
                ];
            }
        }
    }
}
