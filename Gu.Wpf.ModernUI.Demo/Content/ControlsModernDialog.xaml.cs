﻿namespace Gu.Wpf.ModernUI.Demo.Content
{
    using System.Windows;
    using System.Windows.Controls;

    using Gu.Wpf.ModernUI;
    using ModernUi.Interfaces;

    /// <summary>
    /// Interaction logic for ControlsModernDialog.xaml
    /// </summary>
    public partial class ControlsModernDialog : UserControl
    {
        public ControlsModernDialog()
        {
            InitializeComponent();
        }

        private void CommonDialog_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ModernDialog {
                Title = "Common dialog",
                Content = new LoremIpsum1()
            };
            dlg.Buttons = new Button[] { dlg.OkButton, dlg.CancelButton};
            dlg.ShowDialog();

            this.dialogResult.Text = dlg.DialogResult.HasValue ? dlg.DialogResult.ToString() : "<null>";
            this.dialogMessageBoxResult.Text = dlg.MessageBoxResult.ToString();
        }

        private void MessageDialog_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxButton btn = MessageBoxButton.OK;
            if (true == ok.IsChecked) btn = MessageBoxButton.OK;
            else if (true == okcancel.IsChecked) btn = MessageBoxButton.OKCancel;
            else if (true == yesno.IsChecked) btn = MessageBoxButton.YesNo;
            else if (true == yesnocancel.IsChecked) btn = MessageBoxButton.YesNoCancel;

            var result = ModernDialog.ShowMessage("This is a simple Modern UI styled message dialog. Do you like it?", "Message Dialog", btn);

            this.msgboxResult.Text = result.ToString();
        }

        private void ModernPopup_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as ModernWindow;
            if (window == null)
            {
                return;
            }
            MessageBoxButtons btn = MessageBoxButtons.OK;
            if (true == ok.IsChecked) btn = MessageBoxButtons.OK;
            else if (true == okcancel.IsChecked) btn = MessageBoxButtons.OKCancel;
            else if (true == yesno.IsChecked) btn = MessageBoxButtons.YesNo;
            else if (true == yesnocancel.IsChecked) btn = MessageBoxButtons.YesNoCancel;

            var result = window.DialogHandler.Show("This is a simple Modern UI styled message dialog. Do you like it?", "Message Dialog", btn);

            this.msgboxResult.Text = result.ToString();
        }
    }
}
