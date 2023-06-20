using Thorlabs.MotionControl.DeviceManagerCLI;

namespace WPFKcubeUI
{

    /// <summary>
    /// Defines the <see cref="MotorSimulator" /> used to simulate the motor in case of testing. Needs to be used while the Thorlabs Simulator app is running and with a device simulating
    /// </summary>
    internal static class MotorSimulator
    {
        /// <summary>
        /// The StartMotor.
        /// </summary>
        /// <param name="serialNo">The serialNo<see cref="string"/> of the simulated device.</param>
        public static void StartMotor(string serialNo)
        {
            //Constructor initializing an object of SimulationManager. This needs to be used while using the Thorlabs simulator app
            SimulationManager sm = SimulationManager.Instance;
            //Creates a device with the specified serial number, which needs tobe the same one used in Thorlabs simulator
            sm.StartDevice(serialNo);
            //Initializes the simulation
            sm.InitializeSimulations();
        }
    }
}
