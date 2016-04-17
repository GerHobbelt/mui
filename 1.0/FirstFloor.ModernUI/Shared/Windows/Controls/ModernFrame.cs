namespace FirstFloor.ModernUI.Windows.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using FirstFloor.ModernUI.Windows.Media;
    using FirstFloor.ModernUI.Windows.Navigation;

    /// <summary>
    /// A simple content frame implementation with navigation support.
    /// </summary>
    public class ModernFrame
        : ContentControl
    {
        /// <summary>
        /// Identifies the KeepAlive attached dependency property.
        /// </summary>
        public static readonly DependencyProperty KeepAliveProperty = DependencyProperty.RegisterAttached("KeepAlive", typeof(bool?), typeof(ModernFrame), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the KeepContentAlive dependency property.
        /// </summary>
        public static readonly DependencyProperty KeepContentAliveProperty = DependencyProperty.Register("KeepContentAlive", typeof(bool), typeof(ModernFrame), new PropertyMetadata(true, OnKeepContentAliveChanged));

        /// <summary>
        /// Identifies the ContentLoader dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentLoaderProperty = DependencyProperty.Register("ContentLoader", typeof(IContentLoader), typeof(ModernFrame), new PropertyMetadata(new DefaultContentLoader(), OnContentLoaderChanged));

        /// <summary>
        /// The is loading content property key (readonly). Value: DependencyProperty.RegisterReadOnly("IsLoadingContent", typeof(bool), typeof(ModernFrame), new PropertyMetadata(false)).
        /// </summary>
        private static readonly DependencyPropertyKey IsLoadingContentPropertyKey = DependencyProperty.RegisterReadOnly("IsLoadingContent", typeof(bool), typeof(ModernFrame), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the IsLoadingContent dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLoadingContentProperty = IsLoadingContentPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the Source dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(ModernFrame), new PropertyMetadata(OnSourceChanged));

        /// <summary>
        /// Occurs when navigation to a content fragment begins.
        /// </summary>
        public event EventHandler<FragmentNavigationEventArgs> FragmentNavigation;

        /// <summary>
        /// Occurs when a new navigation is requested.
        /// </summary>
        /// <remarks>
        /// The navigating event is also raised when a parent frame is navigating. This allows for cancelling parent navigation.
        /// </remarks>
        public event EventHandler<NavigatingCancelEventArgs> Navigating;

        /// <summary>
        /// Occurs when navigation to new content has completed.
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigated;

        /// <summary>
        /// Occurs when navigation has failed.
        /// </summary>
        public event EventHandler<NavigationFailedEventArgs> NavigationFailed;

        /// <summary>
        /// The history (readonly). Value: new Stack&lt;Uri&gt;().
        /// </summary>
        private readonly Stack<Uri> history = new Stack<Uri>();

        /// <summary>
        /// The content cache (readonly). Value: new Dictionary&lt;Uri, object&gt;().
        /// </summary>
        private readonly Dictionary<Uri, object> contentCache = new Dictionary<Uri, object>();
#if NET4
        /// <summary>
        /// The child frames (readonly). Value: new List&lt;WeakReference&gt;().
        /// </summary>
        private readonly List<WeakReference> childFrames = new List<WeakReference>();        // list of registered frames in sub tree
#else
        private List<WeakReference<ModernFrame>> childFrames = new List<WeakReference<ModernFrame>>();        // list of registered frames in sub tree
#endif
        /// <summary>
        /// The token source.
        /// </summary>
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// The is navigating history.
        /// </summary>
        private bool isNavigatingHistory;

        /// <summary>
        /// The is reset source.
        /// </summary>
        private bool isResetSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernFrame"/> class.
        /// </summary>
        public ModernFrame()
        {
            DefaultStyleKey = typeof(ModernFrame);

            // associate application and navigation commands with this instance
            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseBack, OnBrowseBack, OnCanBrowseBack));
            CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, OnGoToPage, OnCanGoToPage));
            CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, OnRefresh, OnCanRefresh));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopy, OnCanCopy));

            Loaded += OnLoaded;
        }

        /// <summary>
        /// Raises the keep content alive changed event.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnKeepContentAliveChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ModernFrame)o).OnKeepContentAliveChanged((bool)e.NewValue);
        }

        /// <summary>
        /// Raises the keep content alive changed event.
        /// </summary>
        /// <param name="keepAlive">The keepAlive.</param>
        // ReSharper disable once UnusedParameter.Local
        private void OnKeepContentAliveChanged(bool keepAlive)
        {
            // clear content cache
            contentCache.Clear();
        }

        /// <summary>
        /// Raises the content loader changed event.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        /// <exception cref="ArgumentNullException">ContentLoader</exception>
        private static void OnContentLoaderChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                // null values for content loader not allowed
                throw new ArgumentNullException("ContentLoader");
            }
        }

        /// <summary>
        /// Raises the source changed event.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ModernFrame)o).OnSourceChanged((Uri)e.OldValue, (Uri)e.NewValue);
        }

        /// <summary>
        /// Raises the source changed event.
        /// </summary>
        /// <param name="oldValue">The oldValue.</param>
        /// <param name="newValue">The newValue.</param>
        private void OnSourceChanged(Uri oldValue, Uri newValue)
        {
            // if resetting source or old source equals new, don't do anything
            if (isResetSource || (newValue != null && newValue.Equals(oldValue)))
            {
                return;
            }

            // handle fragment navigation
            string newFragment;
            var oldValueNoFragment = NavigationHelper.RemoveFragment(oldValue);
            var newValueNoFragment = NavigationHelper.RemoveFragment(newValue, out newFragment);

            if (newValueNoFragment != null && newValueNoFragment.Equals(oldValueNoFragment))
            {
                // fragment navigation
                var args = new FragmentNavigationEventArgs
                {
                    Fragment = newFragment
                };

                OnFragmentNavigation(Content as IContent, args);
            }
            else
            {
                var navType = isNavigatingHistory ? NavigationType.Back : NavigationType.New;

                // only invoke CanNavigate for new navigation
                if (!isNavigatingHistory && !CanNavigate(oldValue, newValue, navType))
                {
                    return;
                }

                Navigate(oldValue, newValue, navType);
            }
        }

        /// <summary>
        /// Can navigate.
        /// </summary>
        /// <param name="oldValue">The oldValue.</param>
        /// <param name="newValue">The newValue.</param>
        /// <param name="navigationType">The navigationType.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool CanNavigate(Uri oldValue, Uri newValue, NavigationType navigationType)
        {
            var cancelArgs = new NavigatingCancelEventArgs
            {
                Frame = this,
                Source = newValue,
                IsParentFrameNavigating = true,
                NavigationType = navigationType,
                Cancel = false,
            };
            OnNavigating(Content as IContent, cancelArgs);

            // check if navigation cancelled
            if (cancelArgs.Cancel)
            {
                Debug.WriteLine("Cancelled navigation from '{0}' to '{1}'", oldValue, newValue);

                if (Source != oldValue)
                {
                    // enqueue the operation to reset the source back to the old value
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        isResetSource = true;
                        SetCurrentValue(SourceProperty, oldValue);
                        isResetSource = false;
                    }));
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Navigate.
        /// </summary>
        /// <param name="oldValue">The oldValue.</param>
        /// <param name="newValue">The newValue.</param>
        /// <param name="navigationType">The navigationType.</param>
        private void Navigate(Uri oldValue, Uri newValue, NavigationType navigationType)
        {
            Debug.WriteLine("Navigating from '{0}' to '{1}'", oldValue, newValue);

            // set IsLoadingContent state
            SetValue(IsLoadingContentPropertyKey, true);

            // cancel previous load content task (if any)
            // note: no need for thread synchronization, this code always executes on the UI thread
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource = null;
            }

            // push previous source onto the history stack (only for new navigation types)
            if (oldValue != null && navigationType == NavigationType.New)
            {
                history.Push(oldValue);
            }

            object newContent = null;

            if (newValue != null)
            {
                // content is cached on uri without fragment
                var newValueNoFragment = NavigationHelper.RemoveFragment(newValue);

                if (navigationType == NavigationType.Refresh || !contentCache.TryGetValue(newValueNoFragment, out newContent))
                {
                    var localTokenSource = new CancellationTokenSource();
                    tokenSource = localTokenSource;

                    // load the content (asynchronous!)
                    var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    var task = ContentLoader.LoadContentAsync(newValue, tokenSource.Token);

                    task.ContinueWith(
                        t =>
                    {
                        try
                        {
                            if (t.IsCanceled || localTokenSource.IsCancellationRequested)
                            {
                                Debug.WriteLine("Cancelled navigation to '{0}'", newValue);
                            }
                            else if (t.IsFaulted)
                            {
                                // raise failed event
                                var failedArgs = new NavigationFailedEventArgs
                                {
                                    Frame = this,
                                    Source = newValue,
                                    Error = t.Exception.InnerException,
                                    Handled = false
                                };

                                OnNavigationFailed(failedArgs);

                                // if not handled, show error as content
                                newContent = failedArgs.Handled ? null : failedArgs.Error;

                                SetContent(newValue, navigationType, newContent, true);
                            }
                            else
                            {
                                newContent = t.Result;
                                if (ShouldKeepContentAlive(newContent))
                                {
                                    // keep the new content in memory
                                    contentCache[newValueNoFragment] = newContent;
                                }

                                SetContent(newValue, navigationType, newContent, false);
                            }
                        }
                        finally
                        {
                            // clear global tokenSource to avoid a Cancel on a disposed object
                            if (tokenSource == localTokenSource)
                            {
                                tokenSource = null;
                            }

                            // and dispose of the local tokensource
                            localTokenSource.Dispose();
                        }
                    }, scheduler);
                    return;
                }
            }

            // newValue is null or newContent was found in the cache
            SetContent(newValue, navigationType, newContent, false);
        }

        /// <summary>
        /// Set content.
        /// </summary>
        /// <param name="newSource">The newSource.</param>
        /// <param name="navigationType">The navigationType.</param>
        /// <param name="newContent">The newContent.</param>
        /// <param name="contentIsError">The contentIsError.</param>
        private void SetContent(Uri newSource, NavigationType navigationType, object newContent, bool contentIsError)
        {
            var oldContent = Content as IContent;

            // assign content
            Content = newContent;

            // do not raise navigated event when error
            if (!contentIsError)
            {
                var args = new NavigationEventArgs
                {
                    Frame = this,
                    Source = newSource,
                    Content = newContent,
                    NavigationType = navigationType
                };

                OnNavigated(oldContent, newContent as IContent, args);
            }

            // set IsLoadingContent to false
            SetValue(IsLoadingContentPropertyKey, false);

            if (!contentIsError)
            {
                // and raise optional fragment navigation events
                string fragment;
                NavigationHelper.RemoveFragment(newSource, out fragment);
                if (fragment != null)
                {
                    // fragment navigation
                    var fragmentArgs = new FragmentNavigationEventArgs
                    {
                        Fragment = fragment
                    };

                    OnFragmentNavigation(newContent as IContent, fragmentArgs);
                }
            }
        }

        /// <summary>
        /// Get child frames.
        /// </summary>
        /// <returns>The <see cref="T:IEnumerable{ModernFrame}"/>.</returns>
        private IEnumerable<ModernFrame> GetChildFrames()
        {
            var refs = childFrames.ToArray();
            foreach (var r in refs)
            {
                var valid = false;
                ModernFrame frame;

#if NET4
                if (r.IsAlive)
                {
                    frame = (ModernFrame)r.Target;
#else
                if (r.TryGetTarget(out frame)) {
#endif
                    //// check if frame is still an actual child (not the case when child is removed, but not yet garbage collected)
                    if (Equals(NavigationHelper.FindFrame(null, frame), this))
                    {
                        valid = true;
                        yield return frame;
                    }
                }

                if (!valid)
                {
                    childFrames.Remove(r);
                }
            }
        }

        /// <summary>
        /// Raises the fragment navigation event.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="e">The fragment navigation event arguments.</param>
        private void OnFragmentNavigation(IContent content, FragmentNavigationEventArgs e)
        {
            // invoke optional IContent.OnFragmentNavigation
            content?.OnFragmentNavigation(e);

            // raise the FragmentNavigation event
            FragmentNavigation?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the navigating event.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="e">The navigating cancel event arguments.</param>
        private void OnNavigating(IContent content, NavigatingCancelEventArgs e)
        {
            // first invoke child frame navigation events
            foreach (var f in GetChildFrames())
            {
                f.OnNavigating(f.Content as IContent, e);
            }

            e.IsParentFrameNavigating = !Equals(e.Frame, this);

            // invoke IContent.OnNavigating (only if content implements IContent)
            content?.OnNavigatingFrom(e);

            // raise the Navigating event
            Navigating?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the navigated event.
        /// </summary>
        /// <param name="oldContent">The oldContent.</param>
        /// <param name="newContent">The newContent.</param>
        /// <param name="e">The navigation event arguments.</param>
        private void OnNavigated(IContent oldContent, IContent newContent, NavigationEventArgs e)
        {
            // invoke IContent.OnNavigatedFrom and OnNavigatedTo
            oldContent?.OnNavigatedFrom(e);

            newContent?.OnNavigatedTo(e);

            // raise the Navigated event
            Navigated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the navigation failed event.
        /// </summary>
        /// <param name="e">The navigation failed event arguments.</param>
        private void OnNavigationFailed(NavigationFailedEventArgs e)
        {
            NavigationFailed?.Invoke(this, e);
        }

        /// <summary>
        /// Determines whether the routed event args should be handled.
        /// </summary>
        /// <param name="args">The can execute routed event arguments.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        /// <remarks>This method prevents parent frames from handling routed commands.</remarks>
        private bool HandleRoutedEvent(CanExecuteRoutedEventArgs args)
        {
            var originalSource = args.OriginalSource as DependencyObject;

            if (originalSource == null)
            {
                return false;
            }

            return Equals(originalSource.AncestorsAndSelf().OfType<ModernFrame>().FirstOrDefault(), this);
        }

        /// <summary>
        /// Raises the can browse back event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The can execute routed event arguments.</param>
        private void OnCanBrowseBack(object sender, CanExecuteRoutedEventArgs e)
        {
            // only enable browse back for source frame, do not bubble
            if (HandleRoutedEvent(e))
            {
                e.CanExecute = history.Count > 0;
            }
        }

        /// <summary>
        /// Raises the can copy event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The can execute routed event arguments.</param>
        private void OnCanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            if (HandleRoutedEvent(e))
            {
                e.CanExecute = Content != null;
            }
        }

        /// <summary>
        /// Raises the can go to page event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The can execute routed event arguments.</param>
        private void OnCanGoToPage(object sender, CanExecuteRoutedEventArgs e)
        {
            if (HandleRoutedEvent(e))
            {
                e.CanExecute = e.Parameter is string || e.Parameter is Uri;
            }
        }

        /// <summary>
        /// Raises the can refresh event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The can execute routed event arguments.</param>
        private void OnCanRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            if (HandleRoutedEvent(e))
            {
                e.CanExecute = Source != null;
            }
        }

        /// <summary>
        /// Raises the browse back event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The executed routed event arguments.</param>
        private void OnBrowseBack(object target, ExecutedRoutedEventArgs e)
        {
            if (history.Count > 0)
            {
                var oldValue = Source;
                var newValue = history.Peek();     // do not remove just yet, navigation may be cancelled

                if (CanNavigate(oldValue, newValue, NavigationType.Back))
                {
                    isNavigatingHistory = true;
                    SetCurrentValue(SourceProperty, history.Pop());
                    isNavigatingHistory = false;
                }
            }
        }

        /// <summary>
        /// Raises the go to page event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The executed routed event arguments.</param>
        private void OnGoToPage(object target, ExecutedRoutedEventArgs e)
        {
            var newValue = NavigationHelper.ToUri(e.Parameter);
            SetCurrentValue(SourceProperty, newValue);
        }

        /// <summary>
        /// Raises the refresh event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The executed routed event arguments.</param>
        private void OnRefresh(object target, ExecutedRoutedEventArgs e)
        {
            if (CanNavigate(Source, Source, NavigationType.Refresh))
            {
                Navigate(Source, Source, NavigationType.Refresh);
            }
        }

        /// <summary>
        /// Raises the copy event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The executed routed event arguments.</param>
        private void OnCopy(object target, ExecutedRoutedEventArgs e)
        {
            // copies the string representation of the current content to the clipboard
            Clipboard.SetText(Content.ToString());
        }

        /// <summary>
        /// Raises the loaded event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var parent = NavigationHelper.FindFrame(NavigationHelper.FrameParent, this);
            parent?.RegisterChildFrame(this);
        }

        /// <summary>
        /// Register child frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        private void RegisterChildFrame(ModernFrame frame)
        {
            // do not register existing frame
            if (!GetChildFrames().Contains(frame))
            {
#if NET4
                var r = new WeakReference(frame);
#else
                var r = new WeakReference<ModernFrame>(frame);
#endif
                childFrames.Add(r);
            }
        }

        /// <summary>
        /// Determines whether the specified content should be kept alive.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        private bool ShouldKeepContentAlive(object content)
        {
            var o = content as DependencyObject;
            if (o != null)
            {
                var result = GetKeepAlive(o);

                // if a value exists for given content, use it
                if (result.HasValue)
                {
                    return result.Value;
                }
            }

            // otherwise let the ModernFrame decide
            return KeepContentAlive;
        }

        /// <summary>
        /// Gets a value indicating whether to keep specified object alive in a ModernFrame instance.
        /// </summary>
        /// <param name="o">The target dependency object.</param>
        /// <returns>Whether to keep the object alive. Null to leave the decision to the ModernFrame.</returns>
        public static bool? GetKeepAlive(DependencyObject o)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return (bool?)o.GetValue(KeepAliveProperty);
        }

        /// <summary>
        /// Sets a value indicating whether to keep specified object alive in a ModernFrame instance.
        /// </summary>
        /// <param name="o">The target dependency object.</param>
        /// <param name="value">Whether to keep the object alive. Null to leave the decision to the ModernFrame.</param>
        public static void SetKeepAlive(DependencyObject o, bool? value)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            o.SetValue(KeepAliveProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a value whether content should be kept in memory.
        /// </summary>
        public bool KeepContentAlive
        {
            get { return (bool)GetValue(KeepContentAliveProperty); }
            set { SetValue(KeepContentAliveProperty, value); }
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
        /// Gets a value indicating whether this instance is currently loading content.
        /// </summary>
        public bool IsLoadingContent => (bool)GetValue(IsLoadingContentProperty);

        /// <summary>
        /// Gets or sets the source of the current content.
        /// </summary>
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
    }
}
