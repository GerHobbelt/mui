﻿namespace Gu.Wpf.ModernUI.Navigation
{
    using System.Windows.Input;
    using Properties;

    /// <summary>
    /// The routed link commands.
    /// </summary>
    public static class LinkCommands
    {
        private static readonly RoutedUICommand navigateLink = new RoutedUICommand(Resources.NavigateLink, "NavigateLink", typeof(LinkCommands));

        /// <summary>
        /// Gets the navigate link routed command.
        /// </summary>
        public static RoutedUICommand NavigateLink
        {
            get { return navigateLink; }
        }
    }
}