﻿using System.Windows;
using ModernUI.Presentation;
using ModernUI.Windows.Controls;

namespace ModernUI.App
{
    /// <summary>
    ///     An ICommand implementation displaying a message box.
    /// </summary>
    public class SampleMsgBoxCommand
        : CommandBase
    {
        /// <summary>
        ///     Executes the command.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected override void OnExecute(object parameter)
        {
            ModernDialog.ShowMessage("A messagebox triggered by selecting a hyperlink", "Messagebox",
                MessageBoxButton.OK);
        }
    }
}