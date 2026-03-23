using LiveCharts;
using LiveCharts.Wpf;

namespace Surprise_Attack_test
{
    /// <summary>
    /// Represents a dynamic chart that tracks and displays the convergence 
    /// of the ant colony algorithm over multiple generations.
    /// </summary>
    public class DynamicChart
    {
        /// <summary>
        /// Gets or sets the collection of series to be plotted on the chart.
        /// </summary>
        public SeriesCollection SeriesCollection { get; set; }

        /// <summary>
        /// A collection of distance values representing the algorithm's performance per generation.
        /// </summary>
        private ChartValues<double> distances;

        /// <summary>
        /// Gets or sets the formatter function used to format the Y-axis labels.
        /// </summary>
        public Func<double, string> YFormatter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicChart"/> class.
        /// Sets up the initial chart properties, series, and Y-axis label formatting.
        /// </summary>
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

        /// <summary>
        /// Adds a new best distance value to the chart for the current generation.
        /// </summary>
        /// <param name="bestDistance">The best distance achieved in the generation.</param>
        public void AddGenToChart(double bestDistance)
        {

            this.distances.Add(bestDistance);

        }

        /// <summary>
        /// Clears all existing distance data from the chart, resetting it.
        /// </summary>
        public void ClearChart()
        {
            this.distances.Clear();
        }
    }
}