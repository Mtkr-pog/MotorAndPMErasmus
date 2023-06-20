# MotorAndPMErasmus
This is a project created by two students during the Erasmus+ period in Portugal: Matteo Trolese and Giovanni Possenti.
## Code
Our code is open source so anyone can feel free to download it and modify it to adapt it to one's case. The project was built in Visual Studio 2019.
## Use case
This program is used to move a servo motor (Thorlabs PRM1/M-Z8 using a KCD101 servo motor controller) and measure the reading from a photodetector (Thorlabs S120VC using a PM100USB PowerMeter) and plot them in a graph.
The program can also be used with a simulation of the motor, there is a line of code that can be commented or uncommented to activate or deactivate the simulation. Note that when simulating the motor the app Kinesis Simulator (https://www.thorlabs.com/software_pages/ViewSoftwarePage.cfm?Code=Motion_Control&viewtab=0) needs to be runnig with a simulation of the KDC101 DC Servo Drive and the PRM1/M-Z8. By default the serial number should be 27000001 so it should connect to code right away. Just in case you are able to change it quickly directly from the code in the `MainWindow.xaml.cs`.
## Tools used
For the main programming language we used C#, to plot the graph in WPF we used `LiveCharts.Wpf` (https://v0.lvcharts.com/) and to export the graph to excel we used `Aspose.Cells` (https://products.aspose.com/cells)
