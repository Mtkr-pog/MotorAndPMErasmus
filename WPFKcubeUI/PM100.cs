using System.ComponentModel;
using System.Text;
using System.Windows;
using Thorlabs.TLPM_64.Interop;

namespace WPFKcubeUI
{


    /// <summary>
    /// Defines the class for the managment of the PM100USB interface
    /// </summary>
    internal class PM100 
    {
        /// <summary>
        /// Defines the TLPM (ThorLabsPowerMeter) in this case it should be the PM100USB
        /// </summary>
        private readonly TLPM Tlpm;

        /// <summary>
        /// Initializes a new instance of the <see cref="PM100"/> class.
        /// </summary>
        public PM100()
        {
            Tlpm = new TLPM(new System.IntPtr());
            Tlpm.findRsrc(out uint device_count);
            if (device_count == 0)
            {
                MessageBox.Show("No power meters connected");
                return;
            }
            StringBuilder resource_name = new StringBuilder(1024);
            Tlpm.getRsrcName(0, resource_name);
            Tlpm = new TLPM(resource_name.ToString(), true, true);
            Tlpm.setWavelength(635.0);
        }

        /// <summary>
        /// The MeasurePower returns the power coming out of the PM100.
        /// </summary>
        /// <returns>The current power reading of the PM100 in <see cref="double"/>.</returns>
        public double MeasurePower()
        {
            Tlpm.measPower(out double power);
            return power;
        }

        /// <summary>
        /// The CloseMeter closes the PowerMeter.
        /// </summary>
        public void CloseMeter()
        {
            Tlpm.Dispose();
        }

    }
}
