using System;
namespace WPFKcubeUI
{
    /// <summary>
    /// Defines the <see cref="MeasureModel" />. used in the graph to represent the x and y axis
    /// </summary>
    public class MeasureModel
    {
        /// <summary>
        /// Gets or sets the DateTime. Used in the graph as the x axis
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the Value. Used in the graph as the y axis
        /// </summary>
        public double Value { get; set; }
    }
}
