using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.GenericMotorCLI;
using Thorlabs.MotionControl.GenericMotorCLI.ControlParameters;
using Thorlabs.MotionControl.GenericMotorCLI.Settings;
using Thorlabs.MotionControl.KCube.DCServoCLI;

namespace WPFKcubeUI
{
    
    /// <summary>
    /// Logic part for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Defines the positionTimer used to to update the position of the motor in real-time
        /// </summary>
        private DispatcherTimer positionTimer;

        /// <summary>
        /// Defines the fileWriter used to write to a file(.txt) the motor position and power reading.
        /// </summary>
        private StreamWriter fileWriter;

        /// <summary>
        /// Defines the PM100 powerMeter used to read the power cominig from a laser.
        /// </summary>
        internal PM100 powerMeter;


        /// <summary>
        /// Defines the _kCubeDCServo the motor.
        /// </summary>
        private static KCubeDCServo _kCubeDCServo;

        /// <summary>
        /// Defines the moveThread used to move the motor whilst the UI keeps updating.
        /// </summary>
        private Thread moveThread;


        private GraphWindow graphWindow;

        /// <summary>
        /// The MainWindow_OnLoaded occurs when the window gets loaded.
        /// It is used to build the motor, start the position timer and initialize the move thread
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            {
                BuildMotor();
                powerMeter = new PM100();
                string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                String hour = DateTime.Now.ToString("yyyyMMdd_HH");
                string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\output\" + hour + @"\" + date + "_Movement.txt";
                if (!Directory.Exists(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\output\" + hour))
                    Directory.CreateDirectory(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\output\" + hour);
                fileWriter = File.CreateText(path);
                positionTimer = new DispatcherTimer();
                positionTimer.Interval = TimeSpan.FromMilliseconds(50); // Set the interval as needed
                positionTimer.Tick += PositionTimer_Tick;

                // Start the timer
                positionTimer.Start();
                moveThread = new Thread(MoveLoop);
            }
        }

        /// <summary>
        /// The BuildMotor bulds the motor.
        /// </summary>
        public void BuildMotor()
        {
            //The line underneath can be commented if you are using a real motor instead of a simulated one
            MotorSimulator.StartMotor("27000001");

            // This instructs the DeviceManager to build and maintain the list of
            // devices connected. We then print a list of device name strings called
            // “devices” which contain the prefix “27”
            DeviceManagerCLI.BuildDeviceList();
            List<string> devices = DeviceManagerCLI.GetDeviceList(27);
            // IF statement – if the number of devices connected is zero, the Window
            // will display “No Devices”.
            if (devices.Count == 0)
            {
                MessageBox.Show("No Devices");
                return;
            }
            // Selects the first device serial number from “devices” list.
            string serialNo = devices[0];
            // Creates the device. We assign an instance of the device to _kCubeDCServo
            _kCubeDCServo = KCubeDCServo.CreateKCubeDCServo(serialNo);
            // Connect to the device & wait for initialisation. This is contained in a
            // Try/Catch Error Handling Statement.
            try
            {
                _kCubeDCServo.Connect(serialNo);
                if (!_kCubeDCServo.IsSettingsInitialized())
                {
                    _kCubeDCServo.WaitForSettingsInitialized(3000);
                }
            }// wait for settings to be initialized
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            _kCubeDCServo.StartPolling(50);
            Thread.Sleep(100);
            _kCubeDCServo.EnableDevice();
            Thread.Sleep(100);
            MotorConfiguration config = _kCubeDCServo.LoadMotorConfiguration(serialNo, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            config.DeviceSettingsName = "PRM1-MZ8";
            config.UpdateCurrentConfiguration();
            _kCubeDCServo.SetSettings(_kCubeDCServo.MotorDeviceSettings, true, false);
            JogParameters jogParams = _kCubeDCServo.GetJogParams();
            jogParams.StepSize = (decimal)10;
            jogParams.VelocityParams.MaxVelocity = (decimal)10;
            jogParams.JogMode = JogParametersBase.JogModes.SingleStep;
            _kCubeDCServo.SetJogParams(jogParams);
            _kCubeDCServo.Home(100000);
        }

        /// <summary>
        /// The MainWindow_OnClosed occurs when the window is closed.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            // Disconnect device after closing the Window.
            if ((_kCubeDCServo != null) && _kCubeDCServo.IsConnected)
            {
                _kCubeDCServo.StopPolling();
                _kCubeDCServo.Disconnect(true);
            }
            WritePositionToFile();
            fileWriter.Close();
            try { graphWindow.Close(); } catch { }
            powerMeter.CloseMeter();
        }

        /// <summary>
        /// The Button_Click occurs when the "StartIteration" button is pressed.
        /// This method starts the moveLoop thread
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            moveThread.Start(Tuple.Create(int.Parse(_startPosition.Text), int.Parse(_finalPosition.Text), int.Parse(_stepSize.Text), int.Parse(_delayStep.Text)));
        }

        /// <summary>
        /// The PositionTimer_Tick used to update the text block labelled "Position" with the current position of the motor.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            // Perform continuous work on the secondary thread
            if (_kCubeDCServo.IsConnected)
            {
                _position.Text = _kCubeDCServo.Position.ToString();
            }
        }

        /// <summary>
        /// The MoveLoop used to move the motor in a loop from one position to another stopping once every x degrees for some time.
        /// </summary>
        /// <param name="threadParams">The threadParams<see cref="object"/>.</param>
        public void MoveLoop(object threadParams)
        {
            Tuple<int, int, int, int> parameters = (Tuple<int, int, int, int>)threadParams;
            int startPosition = parameters.Item1;
            int finalPosition = parameters.Item2;
            int stepSize = parameters.Item3;
            int delayStep = parameters.Item4;
            while (_kCubeDCServo.IsDeviceBusy) { }
            _kCubeDCServo.MoveTo(startPosition, 0);
            while (_kCubeDCServo.IsDeviceBusy) { }
            WritePositionToFile();
            //if(startPosition >= finalPosition)
            //{
            //    finalPosition += 360;
            //}
            Thread.Sleep(delayStep);
            while (true)
            {
                while (_kCubeDCServo.IsDeviceBusy) { }

                if (_kCubeDCServo.Position + stepSize >= finalPosition)
                {
                    break; // Exit the while loop
                }

                _kCubeDCServo.MoveRelative(MotorDirection.Forward, stepSize, 0);

                while (_kCubeDCServo.IsDeviceBusy) { }
                WritePositionToFile();
                Thread.Sleep(delayStep);
            }

            decimal positionDifference = Math.Abs(_kCubeDCServo.Position - finalPosition);

            if (positionDifference > (decimal)0.05)
            {
                while (_kCubeDCServo.IsDeviceBusy) { }
                _kCubeDCServo.MoveTo(finalPosition, 0);
                while (_kCubeDCServo.IsDeviceBusy) { }
                WritePositionToFile();
            }
        }

        /// <summary>
        /// The WritePositionToFile used to write the current position and power reading to a file.
        /// </summary>
        public void WritePositionToFile()
        {
            string date = DateTime.Now.ToString("HH-mm-ss");
            try
            {
                fileWriter.WriteLine(_kCubeDCServo.Position.ToString() + "\ttime:" + date);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        /// <summary>
        /// The Home_Motor occurs when the "Home" button is pressed.
        /// Used to home the motor
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void Home_Motor(object sender, RoutedEventArgs e)
        {
            while (_kCubeDCServo.IsDeviceBusy) { }
            _kCubeDCServo.Home(0);
            while (_kCubeDCServo.IsDeviceBusy) { }
        }

        /// <summary>
        /// The ShowGraphWindow occurs when the button "Show Graph Window"
        /// Used to open a graph window eliminating the data collected before from the graph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowGraphWindow(object sender, RoutedEventArgs e)
        {
            graphWindow = new GraphWindow(Tuple.Create(powerMeter));
            graphWindow.Show();
        }
    }
}
