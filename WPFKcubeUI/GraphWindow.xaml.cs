using Aspose.Cells;
using Aspose.Cells.Charts;
using LiveCharts;
using LiveCharts.Configurations;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFKcubeUI
{


    /// <summary>
    /// Defines the <see cref="ZoomingModeCoverter" /> that manages the zoom and the padding of the graph in the xaml.
    /// </summary>
    public class ZoomingModeCoverter : IValueConverter
    {
        /// <summary>
        /// Object with which we control the axes of the zooming and the padding in the xaml.
        /// </summary>
        /// <param name="value">The value<see cref="object"/>.</param>
        /// <param name="targetType">The targetType<see cref="Type"/>.</param>
        /// <param name="parameter">The parameter<see cref="object"/>.</param>
        /// <param name="culture">The culture<see cref="CultureInfo"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((ZoomingOptions)value)
            {
                case ZoomingOptions.None:
                    return "None";
                case ZoomingOptions.X:
                    return "X";
                case ZoomingOptions.Y:
                    return "Y";
                case ZoomingOptions.Xy:
                    return "XY";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Object with which we can convert back the mode of the zooming and the padding.
        /// </summary>
        /// <param name="value">The value<see cref="object"/>.</param>
        /// <param name="targetType">The targetType<see cref="Type"/>.</param>
        /// <param name="parameter">The parameter<see cref="object"/>.</param>
        /// <param name="culture">The culture<see cref="CultureInfo"/>.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class that manages the window with the graph.
    /// </summary>
    public partial class GraphWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Defines the _axisMax which is the maximum value that the X axis can have.
        /// </summary>
        private double _axisMax;

        /// <summary>
        /// Defines the _axisMin which is the minimum value that the X axis can have.
        /// </summary>
        private double _axisMin;

        /// <summary>
        /// Defines the _trend which is the series of values thatthe graph represents.
        /// </summary>
        private double _trend;

        /// <summary>
        /// Defines the ReadThread which is theThread where we read the data from the PM100.
        /// </summary>
        private readonly Thread ReadThread;

        /// <summary>
        /// Defines the meter which is the object that represents the PM100.
        /// </summary>
        private readonly PM100 Meter;

        /// <summary>
        /// Defines the _zoomingMode which is the zooming mode chosen.
        /// </summary>
        private ZoomingOptions _zoomingMode;

        /// <summary>
        /// Defines the reading which is a boolean used for stopping and restarting the representation of the data in the graph.
        /// </summary>
        private bool reading = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphWindow"/> class.
        /// </summary>
        /// <param name="meterTuple">The meterTuple<see cref="object"/>.</param>
        public GraphWindow(object meterTuple)
        {
            InitializeComponent();
            Tuple<PM100> meter = (Tuple<PM100>)meterTuple;
            Meter = meter.Item1;

            CartesianMapper<MeasureModel> mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y



            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array
            ChartValues = new ChartValues<MeasureModel>();

            ZoomingMode = ZoomingOptions.X;

            DateTimeFormatter = value => new DateTime((long)value).ToString("mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms

            IsReading = false;

            DataContext = this;

            ReadThread = new Thread(Read);
        }

        /// <summary>
        /// Gets or sets the ChartValues.
        /// </summary>
        public ChartValues<MeasureModel> ChartValues { get; set; }

        /// <summary>
        /// Gets or sets the DateTimeFormatter.
        /// </summary>
        public Func<double, string> DateTimeFormatter { get; set; }

        /// <summary>
        /// Gets or sets the AxisStep.
        /// </summary>
        public double AxisStep { get; set; }

        /// <summary>
        /// Gets or sets the AxisUnit.
        /// </summary>
        public double AxisUnit { get; set; }

        /// <summary>
        /// Gets or sets the AxisMax.
        /// </summary>
        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }

        /// <summary>
        /// Gets or sets the AxisMin.
        /// </summary>
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsReading.
        /// </summary>
        public bool IsReading { get; set; }

        /// <summary>
        /// Gets or sets the ZoomingMode.
        /// </summary>
        public ZoomingOptions ZoomingMode
        {
            get { return _zoomingMode; }
            set
            {
                _zoomingMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The ToogleZoomingMode, it changes the zooming mode every time the relative button is pressed. There are 3 zooming mode that work on the axis.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void ToogleZoomingMode(object sender, RoutedEventArgs e)
        {
            switch (ZoomingMode)
            {
                case ZoomingOptions.None:
                    ZoomingMode = ZoomingOptions.X;
                    break;
                case ZoomingOptions.X:
                    ZoomingMode = ZoomingOptions.Y;
                    break;
                case ZoomingOptions.Y:
                    ZoomingMode = ZoomingOptions.Xy;
                    break;
                case ZoomingOptions.Xy:
                    ZoomingMode = ZoomingOptions.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// The Save method which saves the graph on an excel file.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void SaveExcel(object sender, RoutedEventArgs e)
        {
            Workbook excelBook = new Workbook();
            Worksheet worksheet = excelBook.Worksheets[0];

            worksheet.Cells["A1"].PutValue("Time");
            worksheet.Cells["B1"].PutValue("Power(mW)");

            int i;
            for (i = 2; i < ChartValues.Count; i++)
            {
                worksheet.Cells["A" + i].PutValue(ChartValues[i].DateTime.Ticks);
                worksheet.Cells["B" + i].PutValue(ChartValues[i].Value * 1000);
            }

            int chartIndex = worksheet.Charts.Add(ChartType.Line, 5, 5, 25, 15);

            // Access the instance of the newly added chart
            Chart chart = worksheet.Charts[chartIndex];

            // Set chart data source as the range  "A1:C4"
            chart.SetChartDataRange("B1:B" + (i - 1), true);

            excelBook.RemoveDigitalSignature();

            string date = DateTime.Now.ToString("_yyyyMMdd_HHmmss");
            string hour = DateTime.Now.ToString("yyyyMMdd_HH");
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\output\" +hour+ @"\" + date + "_Graph.xls";

            // Save the Excel file
            excelBook.Save(path);

            // Show a message box indicating the successful save
            MessageBox.Show("Chart data saved successfully.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// The SaveImage. Saves the graph as an image
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void SaveImage(object sender, RoutedEventArgs e)
        {
            string date = DateTime.Now.ToString("_yyyyMMdd_HHmmss");
            string hour = DateTime.Now.ToString("yyyyMMdd_HH");
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\output\" + hour + @"\" + date + "_Graph.png";

            // Render the Grid as a visual
            var gridVisual = new DrawingVisual();
            using (DrawingContext context = gridVisual.RenderOpen())
            {
                context.DrawRectangle(new VisualBrush(grid), null, new Rect(new Point(), new Size(grid.ActualWidth, grid.ActualHeight)));
            }

            // Create a RenderTargetBitmap and render the grid visual onto it
            var renderBitmap = new RenderTargetBitmap((int)grid.ActualWidth, (int)grid.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(gridVisual);

            // Save the RenderTargetBitmap as an image file
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            // Show a message box indicating the successful save
            MessageBox.Show("Chart saved as an image successfully.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// The Read method reads the values from the PM100 and puts them on the graph
        /// </summary>
        private void Read()
        {

            while (true)
            {
                if (reading)
                {
                    try { Thread.Sleep(100); } catch { }
                    var now = DateTime.Now;

                    _trend = Meter.MeasurePower();

                    ChartValues.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = _trend
                    });

                    SetAxisLimits(now);
                }
            }
        }

        /// <summary>
        /// The SetAxisLimits keeps the graph inside the window so that we can see the values from 10 seconds behind and 1 second ahead
        /// </summary>
        /// <param name="now">The now<see cref="DateTime"/>.</param>
        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(10).Ticks; // and 10 seconds behind
        }

        /// <summary>
        /// The StopStartReading, it lets you stop and start reading from the PM100
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void StopStartReading(object sender, RoutedEventArgs e)
        {
            reading = !reading;
        }

        #region INotifyPropertyChanged implementation

        /// <summary>
        /// See <see cref="INotifyPropertyChanged"/> interface
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// See <see cref="INotifyPropertyChanged"/> interface
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            reading = false;
            ReadThread.Interrupt();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            reading = true;
            ReadThread.Start();
        }
    }
}
