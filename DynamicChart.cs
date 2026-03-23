using LiveCharts;
using LiveCharts.Wpf;

namespace Surprise_Attack_test
{
    public class DynamicChart
    {
        public SeriesCollection SeriesCollection { get; set; }

        private ChartValues<double> distances;
        public Func<double, string> YFormatter { get; set; }

        public DynamicChart()
        {
            distances = new ChartValues<double>();
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Distance",
                    Values = distances,
                    PointGeometry = null,
                    LineSmoothness = 0,
                    StrokeThickness = 2,
                    Fill = System.Windows.Media.Brushes.Transparent 
                }
            };
            YFormatter = value =>
            {
                if (value >= 1000000)
                    return (value / 1000000D).ToString("0.##") + "M"; // e.g., 1.5M

                if (value >= 1000)
                    return (value / 1000D).ToString("0.##") + "k"; // e.g., 15k

                return value.ToString("N0"); 
            };
        }
        public void AddGenToChart(double bestDistance)
        {
            
            this.distances.Add(bestDistance);

        }
        public void ClearChart()
        {
            this.distances.Clear();
        }
    }
}