﻿namespace Gu.ModernUI.Windows.Controls
{
    using System.Windows;

    /// <summary>
    /// A DataGrid text column using default Modern UI element styles.
    /// </summary>
    public class DataGridTextColumn
        : System.Windows.Controls.DataGridTextColumn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridTextColumn"/> class.
        /// </summary>
        public DataGridTextColumn()
        {
            this.ElementStyle = Application.Current.Resources["DataGridTextStyle"] as Style;
            this.EditingElementStyle = Application.Current.Resources["DataGridEditingTextStyle"] as Style;
        }
    }
}