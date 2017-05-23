﻿using System.Windows.Controls;

namespace ModernUI.App.Content
{
    /// <summary>
    ///     Interaction logic for SettingsAppearance.xaml
    /// </summary>
    public partial class SettingsAppearance : UserControl
    {
        public SettingsAppearance()
        {
            InitializeComponent();

            // a simple view model for appearance configuration
            DataContext = new SettingsAppearanceViewModel();
        }
    }
}