﻿namespace Gu.Wpf.ModernUI
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// ModernPresenter allows controls to have multiple parents.
    /// This is useful for performance as it allows aggressive caching
    /// </summary>
    public class ModernPresenter : FrameworkElement
    {
        public static readonly DependencyProperty ContentLoaderProperty = Modern.ContentLoaderProperty.AddOwner(
                typeof(ModernPresenter),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnContentLoaderChanged));

        public static readonly DependencyProperty CurrentSourceProperty = DependencyProperty.Register(
            "CurrentSource",
            typeof(Uri),
            typeof(ModernPresenter),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCurrentSourceChanged));

        private readonly WeakReference loaderReference = new WeakReference(null);
        private bool isLoading;
        private UIElement child;

        public ModernPresenter()
        {
            this.IsVisibleChanged += OnIsVisibleChanged;
        }

        /// <summary>
        /// Gets or sets the content loader.
        /// </summary>
        public IContentLoader ContentLoader
        {
            get { return (IContentLoader)GetValue(ContentLoaderProperty); }
            set { SetValue(ContentLoaderProperty, value); }
        }

        /// <summary>
        /// Gets or sets the source of the current content.
        /// </summary>
        public Uri CurrentSource
        {
            get { return (Uri)GetValue(CurrentSourceProperty); }
            set { SetValue(CurrentSourceProperty, value); }
        }

        /// <inheritdoc/>
        public UIElement Content
        {
            get
            {
                return this.Child;
            }
            protected set
            {
                var parent = GetPresenterParent(value);
                if (parent != null)
                {
                    parent.Content = null;
                }
                this.Child = value;
            }
        }

        /// <inheritdoc/>
        protected virtual UIElement Child
        {
            get
            {
                return this.child;
            }

            set
            {
                if (this.child != value)
                {
                    // notify the visual layer that the old child has been removed.
                    RemoveVisualChild(this.child);

                    //need to remove old element from logical tree
                    RemoveLogicalChild(this.child);

                    this.child = value;

                    AddLogicalChild(value);
                    // notify the visual layer about the new child.
                    AddVisualChild(value);

                    InvalidateMeasure();
                }
            }
        }

        /// <inheritdoc/>
        protected virtual async void RefreshContent()
        {
            try
            {
                this.isLoading = true;
                this.Content = (UIElement)await this.ContentLoader.LoadContentAsync(this.CurrentSource, CancellationToken.None);
            }
            catch (Exception e)
            {
                this.Content = new ContentPresenter { Content = e };
            }
            finally
            {
                this.isLoading = false;
            }
        }

        /// <inheritdoc/>
        protected override int VisualChildrenCount => (this.child == null) ? 0 : 1;

        /// <inheritdoc/>
        protected override Visual GetVisualChild(int index)
        {
            if ((this.child == null) || (index != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.child;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = this.Child;
            if (child != null)
            {
                child.Measure(constraint);
                return (child.DesiredSize);
            }
            return (new Size());
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            this.Child?.Arrange(new Rect(arrangeSize));
            return (arrangeSize);
        }

        private static void OnContentLoaderChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (ModernPresenter)o;
            if (e.NewValue != null &&
                e.NewValue != presenter.loaderReference.Target && 
                presenter.CurrentSource != null)
            {
                presenter.loaderReference.Target = e.NewValue;
                presenter.RefreshContent();
            }
        }

        private static void OnCurrentSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (ModernPresenter)o;
            if (e.NewValue != null &&
                presenter.ContentLoader != null)
            {
                presenter.RefreshContent();
            }
        }

        private static ModernPresenter GetPresenterParent(UIElement e)
        {
            if (e == null)
            {
                return null;
            }
            var parent = VisualTreeHelper.GetParent(e);
            if (parent == null)
            {
                return null;
            }
            var presenter = parent as ModernPresenter;
            if (presenter == null)
            {
                throw new ArgumentException(string.Format("Only ModernPresenters can share children. Other parent was {0}", parent.GetType().Name));
            }
            return presenter;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (this.ContentLoader == null)
            {
                return;
            }

            if (this.CurrentSource == null)
            {
                return;
            }

            if (this.Content != null)
            {
                return;
            }

            if (this.isLoading)
            {
                return;
            }

            if (this.IsVisible)
            {
                RefreshContent();
            }
        }
    }
}
