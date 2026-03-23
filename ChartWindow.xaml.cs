using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Surprise_Attack_test
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml.
    /// Represents the secondary window that displays the algorithm's convergence chart.
    /// </summary>
    public partial class ChartWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChartWindow"/> class.
        /// </summary>
        /// <param name="chartData">The dynamic chart data object containing the algorithm's convergence history.</param>
        public ChartWindow(DynamicChart chartData)
        {
            InitializeComponent();

            ConvergenceGraphUI.DataContext = chartData;
        }
    }
}