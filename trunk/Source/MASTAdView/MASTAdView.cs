﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using com.moceanmobile.mast.mraid;

namespace com.moceanmobile.mast
{
    /// <summary>
    /// Possible log levels for SDK logging.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs nothing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Logs only errors.
        /// </summary>
        Error,

        /// <summary>
        /// Logs everything.
        /// Not recommended for production builds.
        /// </summary>
        Debug,
    }

    /// <summary>
    /// The placement type of the instance.
    /// Ads can either be inline with content or external and separate from content.
    /// </summary>
    public enum PlacementType
    {
        /// <summary>
        /// The ad instance is configured for inline placement.
        /// </summary>
        Inline,

        /// <summary>
        /// The ad instance is configured for interstitial use and should not be placed/viewed inline with content.
        /// </summary>
        Interstitial,
    };

    /// <summary>
    /// 
    /// </summary>
    public class MASTAdView : Canvas, BridgeHandler, IDisposable
    {
        private static string UserAgent = null;
        private static int CloseAreaSize = 50;
        
        private PhoneApplicationPage phoneApplicationPage = null;

        /// <summary>
        /// Default inline ad constructor.
        /// Created instance to be used inline with other application content.
        /// </summary>
        public MASTAdView() : this(false)
        {

        }

        /// <summary>
        /// Constructor allowing creation of inline or interstitial instances.
        /// Instances created for inline are used inline with other application content.
        /// Instance created for interstitial are used outside of existing content (hidden if from XAML, not placed in view with code).
        /// </summary>
        /// <param name="interstitial">Controls created instance placement intent.  True for interstitial, false for inline.</param>
        public MASTAdView(bool interstitial)
        {
            base.Loaded += MASTAdView_Loaded;
            base.SizeChanged += MASTAdView_SizeChanged;
            base.Tap += MASTAdView_Tap;

            InitializeAdContainers();

            if (string.IsNullOrEmpty(UserAgent))
            {
                WebBrowser uaBrowser = new WebBrowser();
                uaBrowser.LoadCompleted += UABrowser_LoadCompleted;
                uaBrowser.NavigationFailed += UABrowser_NavigationFailed;
                uaBrowser.IsScriptEnabled = true;
                uaBrowser.NavigateToString("<!DOCTYPE html><html><head><script type='text/javascript'/></head><body></body></html>");
            }

            if (interstitial)
                this.placementType = mast.PlacementType.Interstitial;
        }

        /// <summary>
        /// Returns the SDK version.
        /// </summary>
        public static string Version
        {
            get { return Defaults.VERSION; }
        }

        #region Events

        /// <summary>
        /// Delegate for the AdFailed event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void AdFailedEventHandler(object sender, AdFailedEventArgs e);

        /// <summary>
        /// Delegate for the OpeningURL event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void OpeningURLEventHandler(object sender, OpeningURLEventArgs e);

        /// <summary>
        /// Delegate for the AdResized event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void AdResizedEventHandler(object sender, AdResizedEventArgs e);

        /// <summary>
        /// Delegate for the LoggingEvent event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void LoggingEventEventHandler(object sender, LoggingEventEventArgs e);

        /// <summary>
        /// Delegate for the ReceivedThirdPartyRequest event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void ReceivedThirdPartyRequestEventHandler(object sender, ThirdPartyRequestEventArgs e);

        /// <summary>
        /// Delegate for the PlayingVideo event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void PlayingVideoEventHandler(object sender, PlayingVideoEventArgs e);

        /// <summary>
        /// Delegate for the SavingCalendarEvent event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void SavingCalendarEventEventHandler(object sender, SavingCalendarEventEventArgs e);

        /// <summary>
        /// Delegate for the SavingPhoto event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void SavingPhotoEventHandler(object sender, SavingPhotoEventArgs e);

        /// <summary>
        /// Delegate for the ProcessedRichmediaRequest event.
        /// Note: The delegate may be invoked on a non-UI thread.
        /// </summary>
        /// <param name="sender">Instance invoking the delegate.</param>
        /// <param name="e">Event args providing event information.</param>
        public delegate void ProcessedRichmediaRequestEventHandler(object sender, ProcessedRichmediaRequestEventArgs e);

        /// <summary>
        /// Event triggered when the SDK receives and renders an ad.
        /// </summary>
        public event EventHandler AdReceived;

        /// <summary>
        /// Event triggered when the SDK fails to receive or render an ad.
        /// </summary>
        public event AdFailedEventHandler AdFailed;

        /// <summary>
        /// Event triggered when user action with the SDK will invoke opening a URL.
        /// </summary>
        public event OpeningURLEventHandler OpeningURL;

        /// <summary>
        /// Event triggered when the SDK opens the internal browser.
        /// </summary>
        public event EventHandler InternalBrowserOpened;

        /// <summary>
        /// Event triggered when the SDK closes the internal browser.
        /// </summary>
        public event EventHandler InternalBrowserClosed;

        /// <summary>
        /// Event triggered when the close button is pressed and is not handled by the SDK.
        /// Ex: An expanded rich media ad close button will not trigger this event, however using the close button for inline banner ads will.
        /// </summary>
        public event EventHandler CloseButtonPressed;

        /// <summary>
        /// Event triggered when a rich media ad is expanded.
        /// </summary>
        public event EventHandler AdExpanded;

        /// <summary>
        /// Event triggered when a rich media ad is resized.
        /// </summary>
        public event EventHandler AdResized;

        /// <summary>
        /// Event triggered when a  rich media ad that is expanded or resized is collapsed.
        /// </summary>
        public event EventHandler AdCollapsed;

        /// <summary>
        /// Event triggerd when user interaction with an ad causes the SDK to invoke a Windows Phone task.
        /// </summary>
        public event EventHandler LeavingApplication;

        /// <summary>
        /// Event triggered when a logging event occurs.
        /// </summary>
        public event LoggingEventEventHandler LoggingEvent;

        /// <summary>
        /// Event triggered when the SDK receives a third party request from the ad network.
        /// In this case no ad rendering is done.
        /// </summary>
        public event ReceivedThirdPartyRequestEventHandler ReceivedThirdPartyRequest;

        /// <summary>
        /// Event triggered when a rich media ad requests playing of a video.
        /// </summary>
        public event PlayingVideoEventHandler PlayingVideo;

        /// <summary>
        /// Event triggered when a rich media ad requests saving a calendar event to the user's calendar.
        /// Note: This isn't currently possible with WP7/8.
        /// </summary>
        public event SavingCalendarEventEventHandler SavingCalendarEvent;

        /// <summary>
        /// Event triggered when a rich media ad rquests saving a photo to the user's media library.
        /// </summary>
        public event SavingPhotoEventHandler SavingPhoto;

        /// <summary>
        /// Event triggered when a rich media ad request is processed.
        /// Note: The event will be handled by the SDK and isn't meant to be overloaded by the developer consuming the event.
        /// </summary>
        public event ProcessedRichmediaRequestEventHandler ProcessedRichmediaRequest;

        #endregion

        #region Event Args

        /// <summary>
        /// AdFailed EventArgs
        /// </summary>
        public class AdFailedEventArgs : EventArgs
        {
            public AdFailedEventArgs(Exception execption)
            {
                this.exception = execption;
            }

            private readonly Exception exception;
            /// <summary>
            /// The exception, if any, that lead to the failure.
            /// </summary>
            public Exception Exception
            {
                get { return this.exception; }
            }
        }
        
        /// <summary>
        /// OpeningURL CancelEventArgs
        /// Note:  The SDK opening of the URL can be canceled through the Cancel property.
        /// If canceled, the application is responsible for opening the URL or notifying the user that their
        /// interaction has been canceled.
        /// </summary>
        public class OpeningURLEventArgs : System.ComponentModel.CancelEventArgs
        {
            public OpeningURLEventArgs(string url)
            {
                this.url = url;
            }

            private readonly string url;
            /// <summary>
            ///  The URL being opened.
            /// </summary>
            public string URL
            {
                get { return this.url; }
            }
        }
                
        /// <summary>
        /// AdResized EventArgs
        /// </summary>
        public class AdResizedEventArgs : EventArgs
        {
            public AdResizedEventArgs(System.Windows.Rect rect)
            {
                this.rect = rect;
            }

            private readonly System.Windows.Rect rect;
            /// <summary>
            /// The location of the screen where the ad has been resized too.
            /// Note that resizing doesn't affect inline layout and instead is rendered on top of existing content.
            /// </summary>
            public System.Windows.Rect Rect
            {
                get { return this.rect; }
            }
        }
        
        /// <summary>
        /// LoggingEvent EventArgs
        /// Note:  The SDK logging of the event can be canceled through the Cancel property.
        /// If canceled, the application is responsible for logging the event as necessary.
        /// </summary>
        public class LoggingEventEventArgs : System.ComponentModel.CancelEventArgs
        {
            public LoggingEventEventArgs(LogLevel logLevel, string entry)
            {
                this.logLevel = logLevel;
                this.entry = entry;
            }

            private readonly LogLevel logLevel;
            /// <summary>
            /// The LogLevel of the event.
            /// </summary>
            public object LogLevel
            {
                get { return this.logLevel; }
            }

            private readonly string entry;
            /// <summary>
            /// The logging event entry.
            /// </summary>
            public string Entry
            {
                get { return this.entry; }
            }
        }
        
        /// <summary>
        /// ThirdPartyRequest EventArgs
        /// </summary>
        public class ThirdPartyRequestEventArgs : EventArgs
        {
            public ThirdPartyRequestEventArgs(Dictionary<string, string> properties, Dictionary<string, string> parameters)
            {
                this.properties = properties;
                this.parameters = parameters;
            }

            private readonly Dictionary<string, string> properties;
            /// <summary>
            /// Properties from the ad server describing the third party request.
            /// </summary>
            public Dictionary<string, string> Properties
            {
                get { return this.properties; }
            }

            private readonly Dictionary<string, string> parameters;
            /// <summary>
            /// Parameters from the ad server for the third party SDK.
            /// These parameters are intended to be passed to the third party SDK.
            /// </summary>
            public Dictionary<string, string> Parameters
            {
                get { return this.parameters; }
            }
        }

        /// <summary>
        /// PlayingVideo EventArgs
        /// Note:  The SDK opening/playing of the URL can be canceled through the Cancel property.
        /// If canceled, the application is responsible for opening/playing the URL or notifying the user that their
        /// interaction has been canceled.
        /// </summary>
        public class PlayingVideoEventArgs : System.ComponentModel.CancelEventArgs
        {
            public PlayingVideoEventArgs(string url)
            {
                this.url = url;
            }

            private readonly string url;
            /// <summary>
            /// The URL of the video.
            /// </summary>
            public string URL
            {
                get { return this.url; }
            }
        }
        
        /// <summary>
        /// SavingCalendarEvent EventArgs
        /// Note:  The SDK saving of the event can be canceled through the Cancel property.
        /// Note:  Windows Phone 7/8 does not currenltly offer an API for modifying the user's calendar.
        /// This event can be used to provide the application the ability to do something with the calendar
        /// if the application has it's own calendar.  It could otherwise render a UI where the user could
        /// possibly copy the caledar event and later paste it to a new calendar entry.
        /// </summary>
        public class SavingCalendarEventEventArgs : System.ComponentModel.CancelEventArgs
        {
            public SavingCalendarEventEventArgs(string calendarEvent)
            {
                this.calendarEvent = calendarEvent;
            }

            private readonly string calendarEvent;
            /// <summary>
            /// The calendar event information.
            /// </summary>
            public string CalendarEvent
            {
                get { return this.calendarEvent; }
            }
        }

        /// <summary>
        /// SavingPhoto EventArgs
        /// Note:  The SDK saving of the event can be canceled through the Cancel property.
        /// If canceled, the application is responsible for saving the photo or notifying the user that their
        /// interaction has been canceled.
        /// </summary>
        public class SavingPhotoEventArgs : System.ComponentModel.CancelEventArgs
        {
            public SavingPhotoEventArgs(string url)
            {
                this.url = url;
            }

            private readonly string url;
            /// <summary>
            /// URL of the photo to save.
            /// </summary>
            public string URL
            {
                get { return this.url; }
            }
        }
        
        /// <summary>
        /// ProcessedRichmediaRequest EventArgs
        /// </summary>
        public class ProcessedRichmediaRequestEventArgs : EventArgs
        {
            public ProcessedRichmediaRequestEventArgs(Uri uri, bool handled)
            {
                this.uri = uri;
                this.handled = handled;
            }

            private readonly Uri uri;
            /// <summary>
            /// The URI used to communicate the rich media request from JavaScript to the SDK.
            /// This provides the request and it's arguments.
            /// </summary>
            public Uri URI
            {
                get { return this.uri; }
            }

            private readonly bool handled;
            /// <summary>
            /// Result of the request.  If parsed and handled by the SDK returns true.
            /// </summary>
            public bool Handled
            {
                get { return this.handled; }
            }
        }

        #endregion

        #region Event Sourcing

        private void OnAdReceived()
        {
            if (this.AdReceived != null)
                this.AdReceived(this, EventArgs.Empty);
        }

        private void OnAdFailed(Exception exception)
        {
            if (this.AdFailed != null)
            {
                AdFailedEventArgs args = new AdFailedEventArgs(exception);
                this.AdFailed(this, args);
            }
        }

        private bool OnOpeningURL(string url)
        {
            OpeningURLEventArgs args = new OpeningURLEventArgs(url);

            if (this.OpeningURL != null)
            {
                this.OpeningURL(this, args);
            }

            return args.Cancel == false;
        }

        private void OnInternalBrowserOpened()
        {
            if (this.InternalBrowserOpened != null)
            {
                this.InternalBrowserOpened(this, EventArgs.Empty);
            }
        }

        private void OnInternalBrowserClosed()
        {
            if (this.InternalBrowserClosed != null)
            {
                this.InternalBrowserClosed(this, EventArgs.Empty);
            }
        }

        private void OnCloseButtonPressed()
        {
            if (this.CloseButtonPressed != null)
                this.CloseButtonPressed(this, EventArgs.Empty);
        }

        private void OnAdExpanded()
        {
            if (this.AdExpanded != null)
                this.AdExpanded(this, EventArgs.Empty);
        }

        private void OnAdResized(System.Windows.Rect rect)
        {
            if (this.AdResized != null)
            {
                AdResizedEventArgs args = new AdResizedEventArgs(rect);
                this.AdResized(this, args);
            }
        }

        private void OnAdCollapsed()
        {
            if (this.AdCollapsed != null)
                this.AdCollapsed(this, EventArgs.Empty);
        }

        private void OnLeavingApplication()
        {
            if (this.LeavingApplication != null)
                this.LeavingApplication(this, EventArgs.Empty);
        }

        private bool OnLoggingEvent(LogLevel logLevel, string entry)
        {
            LoggingEventEventArgs args = new LoggingEventEventArgs(logLevel, entry);

            if (this.LoggingEvent != null)
            {
                this.LoggingEvent(this, args);
            }

            return args.Cancel == false;
        }

        private void OnReceivedThirdPartyRequest(Dictionary<string, string> properties, Dictionary<string, string> parameters)
        {
            if (this.ReceivedThirdPartyRequest != null)
            {
                ThirdPartyRequestEventArgs args = new ThirdPartyRequestEventArgs(properties, parameters);
                this.ReceivedThirdPartyRequest(this, args);
            }
        }

        private bool OnPlayingVideo(string url)
        {
            PlayingVideoEventArgs args = new PlayingVideoEventArgs(url);

            if (this.PlayingVideo != null)
            {
                this.PlayingVideo(this, args);
            }

            return args.Cancel == false;
        }

        private bool OnSavingCalendarEvent(string calendarEvent)
        {
            SavingCalendarEventEventArgs args = new SavingCalendarEventEventArgs(calendarEvent);

            if (this.SavingCalendarEvent != null)
            {
                this.SavingCalendarEvent(this, args);
            }

            return args.Cancel == false;
        }

        private bool OnSavingPhoto(string url)
        {
            SavingPhotoEventArgs args = new SavingPhotoEventArgs(url);

            if (this.SavingPhoto != null)
            {
                this.SavingPhoto(this, args);
            }

            return args.Cancel == false;
        }

        private void OnProcessedRichMediaRequest(Uri uri, bool handled)
        {
            if (this.ProcessedRichmediaRequest != null)
            {
                ProcessedRichmediaRequestEventArgs args = new ProcessedRichmediaRequestEventArgs(uri, handled);
                this.ProcessedRichmediaRequest(this, args);
            }
        }

        #endregion

        #region Logging

        private LogLevel logLevel = LogLevel.Error;
        /// <summary>
        /// Sets the log level of the SDK.  Logging is done through the system console only.
        /// Developers desiring to record log data to a custom log file can be done through the LoggingEvent event.
        /// By default the SDK is set to LogLevel.Error.
        /// Production releases should not be set to LogLevel.Debug.
        /// </summary>
        public LogLevel LogLevel
        {
            get { return this.logLevel; }
            set { this.logLevel = value; }
        }

        private void LogEvent(LogLevel level, string entry)
        {
            if (level > this.LogLevel)
                return;

            bool shouldLog = OnLoggingEvent(level, entry);

            if (shouldLog == false)
                return;

            string line = string.Format("MASTAdView:{0}\n\tType:{1}\n\tEvent:{2}", this, level.ToString(), entry);
            System.Diagnostics.Debug.WriteLine(line);
        }

        #endregion

        #region Canvas

        /// <summary>
        /// Allows developers to remove any rendered ad content.
        /// This method will remove any rednered ad containers, close interstitial ads and collapse any expanded or resized rich media ads.
        /// </summary>
        public void RemoveContent()
        {
            if (IsInternalBrowserOpen)
                CloseInternalBrowser();

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    if (this.mraidBridge != null)
                    {
                        switch (this.mraidBridge.State)
                        {
                            case State.Loading:
                            case State.Default:
                            case State.Hidden:
                                break;

                            case State.Expanded:
                            case State.Resized:
                                ((BridgeHandler)this).mraidClose(this.mraidBridge);
                                break;
                        }
                    }
                    break;

                case mast.PlacementType.Interstitial:
                    CloseInterstitial();
                    break;
            }

            this.adDescriptor = null;

            base.Children.Clear();
        }

        private void UpdateLayouts()
        {
            if (this.phoneApplicationPage != null)
                this.phoneApplicationPage.UpdateLayout();

            if (this.resizePopup != null)
                this.resizePopup.UpdateLayout();

            if (this.expandPopup != null)
                this.expandPopup.UpdateLayout();
        }

        private void MASTAdView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Is this valid for all applications?
            this.phoneApplicationPage = (PhoneApplicationPage)((PhoneApplicationFrame)System.Windows.Application.Current.RootVisual).Content;
            this.phoneApplicationPage.OrientationChanged += Page_OrientationChanged;
            this.phoneApplicationPage.BackKeyPress += PhoneApplicationPage_BackKeyPress;

            PerformAdTracking();

            if (this.zone != 0)
            {
                Update();
            }
        }

        private void MASTAdView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            this.imageBorder.Width = e.NewSize.Width;
            this.imageBorder.Height = e.NewSize.Height;

            this.textBorder.Width = e.NewSize.Width;
            this.textBorder.Height = e.NewSize.Height;

            if ((this.IsExpanded == false) && (this.IsResized == false))
            {
                this.webBorder.Width = e.NewSize.Width;
                this.webBorder.Height = e.NewSize.Height;
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsInternalBrowserOpen)
            {
                e.Cancel = true;
                InternalBrowserBackOrClose();
                return;
            }

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    if (this.mraidBridge != null)
                    {
                        switch (this.mraidBridge.State)
                        {
                            case State.Expanded:
                            case State.Resized:
                                e.Cancel = true;
                                ((BridgeHandler)this).mraidClose(this.mraidBridge);
                                break;
                        }
                    }
                    break;

                case mast.PlacementType.Interstitial:
                    if (this.IsExpanded)
                    {
                        e.Cancel = true;

                        // Don't allow back close unless the timer has displayed the button.
                        if (this.expandCloseBorder.Parent != null)
                        {
                            CloseInterstitial();
                        }
                    }
                    break;
            }
        }

        private void Page_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (this.IsExpanded)
            {
                Bridge bridge = this.mraidBridge;
                Border border = this.webBorder;
                if (this.twoPartExpand)
                {
                    bridge = this.twoPartMraidBridge;
                    border = this.twoPartWebBorder;
                }

                if ((bridge != null) && (bridge.State == State.Expanded) &&
                    (bridge.OrientationProperties.AllowOrientationChange == false))
                {
                    return;
                }

                SetExpandOrientation(e.Orientation, bridge, border);
            }
        }

        private void SetExpandOrientation(Microsoft.Phone.Controls.PageOrientation orientation, Bridge bridge, Border border)
        {
            System.Windows.Media.RotateTransform transform = new System.Windows.Media.RotateTransform();
            switch (orientation)
            {
                case PageOrientation.LandscapeLeft:
                    transform.Angle = 90;
                    this.expandCanvas.RenderTransform = transform;
                    this.expandPopup.HorizontalOffset = System.Windows.Application.Current.Host.Content.ActualWidth;
                    this.expandPopup.VerticalOffset = 0;
                    this.expandCanvas.Width = this.expandPopup.Width = System.Windows.Application.Current.Host.Content.ActualHeight;
                    this.expandCanvas.Height = this.expandPopup.Height = System.Windows.Application.Current.Host.Content.ActualWidth;
                    break;
                case PageOrientation.LandscapeRight:
                    transform.Angle = -90;
                    this.expandCanvas.RenderTransform = transform;
                    this.expandPopup.HorizontalOffset = 0;
                    this.expandPopup.VerticalOffset = System.Windows.Application.Current.Host.Content.ActualHeight;
                    this.expandCanvas.Width = this.expandPopup.Width = System.Windows.Application.Current.Host.Content.ActualHeight;
                    this.expandCanvas.Height = this.expandPopup.Height = System.Windows.Application.Current.Host.Content.ActualWidth;
                    break;
                case PageOrientation.PortraitUp:
                    transform = null;
                    this.expandCanvas.RenderTransform = transform;
                    this.expandPopup.HorizontalOffset = 0;
                    this.expandPopup.VerticalOffset = 0;
                    this.expandCanvas.Width = this.expandPopup.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
                    this.expandCanvas.Height = this.expandPopup.Height = System.Windows.Application.Current.Host.Content.ActualHeight;
                    break;
                case PageOrientation.PortraitDown:
                    transform.Angle = 180;
                    this.expandCanvas.RenderTransform = transform;
                    this.expandPopup.HorizontalOffset = 0;
                    this.expandPopup.VerticalOffset = 0;
                    this.expandCanvas.Width = this.expandPopup.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
                    this.expandCanvas.Height = this.expandPopup.Height = System.Windows.Application.Current.Host.Content.ActualHeight;
                    break;
            }

            if (bridge != null)
            {
                MRAIDControllerLayoutUpdate(bridge, border);
            }
        }

        private void MASTAdView_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.adDescriptor == null)
                return;

            if (this.mraidBridge != null)
                return;

            string url = this.adDescriptor.URL;
            if (String.IsNullOrEmpty(url) == false)
            {
                Uri uri = new Uri(url);
                if (OnOpeningURL(url) && OpenURL(uri))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void UABrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            WebBrowser webBrowser = sender as WebBrowser;
            webBrowser.LoadCompleted -= UABrowser_LoadCompleted;
            webBrowser.NavigationFailed -= UABrowser_NavigationFailed;

            string ua = (string)webBrowser.InvokeScript("eval", "navigator.userAgent.toString()");
            if (string.IsNullOrEmpty(UserAgent))
                UserAgent = ua;

            ResumeInternalUpdate();
        }

        private void UABrowser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            WebBrowser webBrowser = sender as WebBrowser;
            webBrowser.LoadCompleted -= UABrowser_LoadCompleted;
            webBrowser.NavigationFailed -= UABrowser_NavigationFailed;
            
            if (string.IsNullOrEmpty(UserAgent))
                UserAgent = Defaults.ERROR_USER_AGENT;

            ResumeInternalUpdate();
        }

        #endregion

        #region Ad Containers

        private Border imageBorder = new Border();
        private Image imageControl = new Image();
        private ImageTools.Controls.AnimatedImage animatedImageControl = new ImageTools.Controls.AnimatedImage();

        /// <summary>
        /// Border used to wrap any image based ads.
        /// </summary>
        public Border ImageBorder
        {
            get { return this.imageBorder; }
        }

        /// <summary>
        /// Control used to render any image based ads (except GIFs).
        /// </summary>
        public Image ImageControl
        {
            get { return this.imageControl; }
        }

        /// <summary>
        /// Control used to render any GIF based image ads (providing animation support).
        /// </summary>
        public ImageTools.Controls.AnimatedImage AnimatedImageControl
        {
            get { return this.animatedImageControl; }
        }
        
        private Border textBorder = new Border();
        private TextBlock textControl = new TextBlock();

        /// <summary>
        /// Border used to wrap any text based ads.
        /// </summary>
        public Border TextBorder
        {
            get { return this.textBorder; }
        }
        
        /// <summary>
        /// Control used to render any text based ads (non-HTML).
        /// </summary>
        public TextBlock TextControl
        {
            get { return this.textControl; }
        }

        private Border webBorder = new Border();
        private WebBrowser webControl = new WebBrowser();

        /// <summary>
        /// Border used to wrap any web based ads.
        /// </summary>
        public Border WebBorder
        {
            get { return this.webBorder; }
        }

        /// <summary>
        /// Control used to render any web/rich media ads (HTML/JS/MRAID).
        /// </summary>
        public WebBrowser WebControl
        {
            get { return this.webControl; }
        }

        private Border inlineCloseBorder = new Border();
        /// <summary>
        /// Border containing the close control for inline ads.
        /// </summary>
        public Border InlineCloseBorder
        {
            get { return this.inlineCloseBorder; }
        }

        private void InitializeAdContainers()
        {
            this.imageControl.Stretch = System.Windows.Media.Stretch.Uniform;
            this.imageBorder.Child = this.imageControl;
            this.imageBorder.SizeChanged += border_SizeChanged;
            
            this.textControl.TextAlignment = System.Windows.TextAlignment.Center;
            this.textControl.TextWrapping = System.Windows.TextWrapping.Wrap;
            this.textBorder.Child = this.textControl;
            this.textBorder.SizeChanged += border_SizeChanged;

            this.webControl.IsGeolocationEnabled = this.allowBrowserGeolocation;
            this.webControl.IsScriptEnabled = true;
            this.webControl.NavigationFailed += webControl_NavigationFailed;
            this.webControl.Navigating += webControl_Navigating;
            this.webControl.Navigated += webControl_Navigated;
            this.webControl.LoadCompleted += webControl_LoadCompleted;
            this.webControl.ScriptNotify += webControl_ScriptNotify;
            this.webControl.SizeChanged += webControl_SizeChanged;
            this.webBorder.Child = this.webControl;
            this.webBorder.SizeChanged += border_SizeChanged;

            Canvas.SetZIndex(this.inlineCloseBorder, byte.MaxValue);
            this.inlineCloseBorder.SizeChanged += border_SizeChanged;
            this.inlineCloseBorder.Tap += closeControl_Tap;
        }

        private void border_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            Border border = sender as Border;
            if (border != null)
            {
                System.Windows.FrameworkElement borderChild = border.Child as System.Windows.FrameworkElement;
                if (borderChild != null)
                {
                    borderChild.Width = border.ActualWidth;
                    borderChild.Height = border.ActualHeight;
                }

                Bridge bridge = this.mraidBridge;
                if (border == this.twoPartWebBorder)
                {
                    bridge = this.twoPartMraidBridge;
                }

                if ((border.Child is WebBrowser) && (bridge != null) && (bridge.State != State.Loading))
                {
                    MRAIDControllerLayoutUpdate(bridge, border);
                }
            }
        }

        #endregion

        #region Expand Popup Control

        private System.Windows.Controls.Primitives.Popup expandPopup = null;
        private Canvas expandCanvas = new Canvas();
        private Border expandCloseBorder = new Border();
        private bool expandPopupHidesTray = false;
        private bool expandPopupHidesBar = false;
        
        /// <summary>
        /// Determines if the instance is currently expanded.
        /// If expanded then there is an SDK controlled full sized popop control rendered on top of application content.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                if (this.expandPopup == null)
                    return false;

                return this.expandPopup.IsOpen;               
            }
        }

        private void OpenExpandPopup(Border border)
        {
            if (expandPopup == null)
            {
                this.expandPopup = new System.Windows.Controls.Primitives.Popup();
                this.expandPopup.HorizontalOffset = 0;
                this.expandPopup.VerticalOffset = 0;
                
                this.expandCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.expandCanvas.VerticalAlignment = VerticalAlignment.Stretch;
                this.expandCanvas.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                this.expandCanvas.SizeChanged += expandCanvas_SizeChanged;
                this.expandCanvas.Tap += MASTAdView_Tap;

                this.expandCloseBorder.Width = CloseAreaSize;
                this.expandCloseBorder.Height = CloseAreaSize;
                this.expandCloseBorder.Padding = new Thickness(5);
                this.expandCloseBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
                this.expandCloseBorder.SizeChanged += expandCloseBorder_SizeChanged;
                this.expandCloseBorder.Tap += closeControl_Tap;
                Canvas.SetZIndex(this.expandCloseBorder, byte.MaxValue);

                this.expandPopup.Child = this.expandCanvas;
            }

            this.expandPopupHidesTray = Microsoft.Phone.Shell.SystemTray.IsVisible;
            if (this.expandPopupHidesTray)
                Microsoft.Phone.Shell.SystemTray.IsVisible = false;

            if ((this.phoneApplicationPage.ApplicationBar != null) && this.phoneApplicationPage.ApplicationBar.IsVisible)
            {
                this.expandPopupHidesBar = true;
                this.phoneApplicationPage.ApplicationBar.IsVisible = false;
            }

            if (border != null)
            {
                this.expandCanvas.Children.Add(border);
                this.expandCanvas.Children.Add(this.expandCloseBorder);
            }

            this.expandCanvas.Width = this.expandPopup.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
            this.expandCanvas.Height = this.expandPopup.Height = System.Windows.Application.Current.Host.Content.ActualHeight;

            this.expandPopup.IsOpen = true;
        }

        private Border CollapseExpandPopup()
        {
            if (expandPopup == null)
                return null;

            Border border = null;

            if (this.expandCanvas.Children.Count > 0)
                border = (Border)expandCanvas.Children[0];

            if (this.expandPopupHidesTray)
                Microsoft.Phone.Shell.SystemTray.IsVisible = this.expandPopupHidesTray;

            if (this.expandPopupHidesBar)
                this.phoneApplicationPage.ApplicationBar.IsVisible = this.expandPopupHidesBar;
            
            this.expandPopup.IsOpen = false;
            this.expandCanvas.Children.Clear();

            if (this.updateDeferred)
                Update();

            return border;
        }

        private void expandCanvas_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            if (canvas == null)
                return;

            foreach (System.Windows.FrameworkElement element in this.expandCanvas.Children)
            {
                if (element == this.expandCloseBorder)
                    continue;

                element.Width = e.NewSize.Width;
                element.Height = e.NewSize.Height;
            }

            Canvas.SetLeft(this.expandCloseBorder, e.NewSize.Width - this.expandCloseBorder.Width);
            Canvas.SetTop(this.expandCloseBorder, 0);
        }

        private void expandCloseBorder_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            Border border = sender as Border;
            FrameworkElement child = border.Child as FrameworkElement;

            if (child == null)
                return;

            child.Width = e.NewSize.Width - border.Padding.Left - border.Padding.Right;
            child.Height = e.NewSize.Height - border.Padding.Top - border.Padding.Bottom;
        }
        
        #endregion

        #region Internal Browser

        private Border previousExpandBorder = null;
        private Border internalBrowserBorder = null;
        private WebBrowser internalBrowser = null;
        private int internalBrowserNavigationSteps = 0;

        /// <summary>
        /// Determines if the instance currently has it's internal browser open.
        /// If the browser is open then there is an SDK controlled full sized popop control rendered on top of application content.
        /// </summary>
        private bool IsInternalBrowserOpen
        {
            get
            {
                if (IsExpanded && (internalBrowserBorder != null) && (internalBrowserBorder.Parent != null))
                {
                    return true;
                }

                return false;
            }
        }

        private void ShowInternalBrowser(string url)
        {
            if (internalBrowserBorder == null)
            {
                internalBrowser = new WebBrowser();
                internalBrowser.IsScriptEnabled = true;

                internalBrowser.Navigated += delegate(object sender, System.Windows.Navigation.NavigationEventArgs args)
                {
                    switch (args.NavigationMode)
                    {
                        case System.Windows.Navigation.NavigationMode.New:
                        case System.Windows.Navigation.NavigationMode.Forward:
                            ++internalBrowserNavigationSteps;
                            break;
                    }
                };

                internalBrowserBorder = new Border();
                internalBrowserBorder.SizeChanged += border_SizeChanged;
                internalBrowserBorder.Child = internalBrowser;
            }

            if (IsExpanded && (this.expandCanvas.Children.Count > 0))
            {
                previousExpandBorder = (Border)this.expandCanvas.Children[0];
                this.expandCanvas.Children.Clear();

                if (mraidBridge != null)
                    mraidBridge.SetViewable(false);

                this.expandCanvas.Children.Add(internalBrowserBorder);
            }
            else
            {
                OpenExpandPopup(internalBrowserBorder);
            }

            OnInternalBrowserOpened();

            internalBrowserNavigationSteps = 0;
            internalBrowser.Navigate(new Uri(url));
        }

        private void InternalBrowserBackOrClose()
        {
            --internalBrowserNavigationSteps;

            if (internalBrowserNavigationSteps <= 0)
            {
                CloseInternalBrowser();
                return;
            }

            try
            {
                internalBrowser.InvokeScript("eval", "history.go(-1)");
                --internalBrowserNavigationSteps;
            }
            catch (Exception)
            {
                LogEvent(mast.LogLevel.Error, "Unable to navigate the internal browser back page.");
                CloseInternalBrowser();
            }
        }

        private void CloseInternalBrowser()
        {
            if (IsExpanded == false)
                return;

            if (previousExpandBorder != null)
            {
                this.expandCanvas.Children.Clear();
                this.expandCanvas.Children.Add(previousExpandBorder);
                this.expandCanvas.Children.Add(expandCloseBorder);

                if (mraidBridge != null)
                    mraidBridge.SetViewable(true);
            }
            else
            {
                CollapseExpandPopup();
            }

            OnInternalBrowserClosed();
        }

        #endregion

        #region Resize Popup Control

        private System.Windows.Controls.Primitives.Popup resizePopup = null;
        private Canvas resizeCanvas = new Canvas();
        private Border resizeCloseBorder = new Border();

        /// <summary>
        /// Determines if the instance is currently resized.
        /// If resized then there is an SDK controlled variable sized popop control rendered on top of application content.
        /// </summary>
        public bool IsResized
        {
            get
            {
                if (this.resizePopup == null)
                    return false;

                return this.resizePopup.IsOpen;
            }
        }

        private void OpenResizePoup(Border border, double x, double y, double width, double height)
        {
            if (resizePopup == null)
            {
                this.resizePopup = new System.Windows.Controls.Primitives.Popup();
                this.Children.Add(this.resizePopup);

                this.resizeCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.resizeCanvas.VerticalAlignment = VerticalAlignment.Stretch;
                this.resizeCanvas.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                this.resizeCanvas.SizeChanged += resizeCanvas_SizeChanged;

                this.resizeCloseBorder.Width = CloseAreaSize;
                this.resizeCloseBorder.Height = CloseAreaSize;
                this.resizeCloseBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
                this.resizeCloseBorder.Tap += closeControl_Tap;
                Canvas.SetZIndex(this.resizeCloseBorder, byte.MaxValue);

                this.resizePopup.Child = this.resizeCanvas;
            }

            this.resizePopup.HorizontalOffset = x;
            this.resizePopup.VerticalOffset = y;
            this.resizePopup.Width = width;
            this.resizePopup.Height = height;

            if (border != null)
            {
                this.resizeCanvas.Children.Add(border);
                this.resizeCanvas.Children.Add(this.resizeCloseBorder);
            }

            this.resizeCanvas.Width = this.resizePopup.ActualWidth;
            this.resizeCanvas.Height = this.resizePopup.ActualHeight;

            this.resizePopup.IsOpen = true;
        }

        private Border CollapseResizePopup()
        {
            if (this.resizePopup == null)
                return null;

            Border border = null;
            if (this.resizeCanvas.Children.Count > 0)
                border = (Border)this.resizeCanvas.Children[0];

            this.resizePopup.IsOpen = false;
            this.resizeCanvas.Children.Clear();

            if (this.updateDeferred)
                Update();

            return border;
        }

        private void resizeCanvas_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            if (canvas == null)
                return;

            foreach (System.Windows.FrameworkElement element in this.resizeCanvas.Children)
            {
                if (element == this.resizeCloseBorder)
                    continue;

                element.Width = e.NewSize.Width;
                element.Height = e.NewSize.Height;
            }
        }

        #endregion

        #region Close Control

        private System.Threading.Timer closeButtonDelayTimer = null;

        private void closeControl_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    if (this.mraidBridge != null)
                    {
                        switch (this.mraidBridge.State)
                        {
                            case State.Loading:
                            case State.Default:
                            case State.Hidden:
                                break;

                            case State.Expanded:
                            case State.Resized:
                                ((BridgeHandler)this).mraidClose(this.mraidBridge);
                                return;
                        }
                    }
                    OnCloseButtonPressed();
                    break;

                case mast.PlacementType.Interstitial:
                    CloseInterstitial();
                    break;
            }
        }

        private void PrepareCloseButton()
        {
            if (this.closeButtonDelayTimer != null)
            {
                this.closeButtonDelayTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

            // Either way, both this and the two part "share" the same state info.
            Bridge bridge = this.mraidBridge;
            if (bridge != null)
            {
                switch (bridge.State)
                {
                    case State.Expanded:
                        // For expanded ads, ignore the timer.
                        if (bridge.ExpandProperties.UseCustomClose == false)
                        {
                            RenderCloseButton();
                        }
                        return;

                    case State.Resized:
                        // The ad creative MUST supply it's own close button.
                        return;
                }
            }

            if (this.showCloseButton)
            {
                if (this.closeButtonDelay <= 0)
                {
                    RenderCloseButton();
                }
                else
                {
                    if (this.closeButtonDelayTimer == null)
                    {
                        this.closeButtonDelayTimer = new System.Threading.Timer(new System.Threading.TimerCallback(OnCloseButtonDelayTimer));
                    }

                    this.closeButtonDelayTimer.Change(this.closeButtonDelay * 1000, System.Threading.Timeout.Infinite);
                }
            }
        }

        private void RenderCloseButton()
        {
            System.Windows.Media.ImageSource imageSource = this.closeButtonCustomSource;

            if (imageSource == null)
            {
                System.IO.Stream resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(Defaults.CLOSE_BUTTON_RESOURCE);
                System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.SetSource(resourceStream);
                imageSource = bitmapImage;
            }
            
            Image buttonImage = new Image();
            buttonImage.Stretch = System.Windows.Media.Stretch.Uniform;
            buttonImage.Source = imageSource;
            
            if (this.mraidBridge != null)
            {
                switch (this.mraidBridge.State)
                {
                    case State.Loading:
                    case State.Default:
                    case State.Hidden:
                        break;

                    case State.Expanded:
                        this.expandCloseBorder.Child = buttonImage;
                        return;

                    case State.Resized:
                        // The creative must supply it's own close button.
                        return;
                }
            }

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    this.inlineCloseBorder.Child = buttonImage;
                    this.inlineCloseBorder.Width = Defaults.INLINE_CLOSE_BUTTON_SIZE;
                    this.inlineCloseBorder.Height = Defaults.INLINE_CLOSE_BUTTON_SIZE;
                    Canvas.SetLeft(this.inlineCloseBorder, this.ActualWidth - this.inlineCloseBorder.Width);
                    Canvas.SetTop(this.inlineCloseBorder, 0);
                    this.Children.Add(this.inlineCloseBorder);
                    break;

                case mast.PlacementType.Interstitial:
                    this.expandCloseBorder.Child = buttonImage;
                    this.expandCanvas.Children.Add(this.expandCloseBorder);
                    break;
            }
        }

        private void OnCloseButtonDelayTimer(object state)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                RenderCloseButton();
            });
        }

        #endregion

        #region Configuration

        private PlacementType placementType = PlacementType.Inline;
        /// <summary>
        /// Returns the current placement type of the instance.
        /// </summary>
        public PlacementType PlacementType
        {
            get { return this.placementType; }
        }

        private string adServerURL = Defaults.AD_SERVER_URL;
        /// <summary>
        /// The current ad server/network URL.  This defaults to the mOcean ad network.
        /// </summary>
        public string AdServerURL
        {
            get { return this.adServerURL; }
            set { this.adServerURL = value; }
        }

        private Dictionary<string, string> adRequestParameters = new Dictionary<string,string>();
        /// <summary>
        /// The current set of ad request parameters.
        /// The SDK will set various parameters based on configuration and other options.
        /// For more information see http://developer.moceanmobile.com/Mocean_Ad_Request_API.
        /// </summary>
        public Dictionary<string, string> AdRequestParameters
        {
            get { return this.adRequestParameters; }
        }

        private int updateInterval = 0;
        /// <summary>
        /// The delay between requesting updates after the initial update.
        /// By default the value is 0 meaning application developers control ad updates by invoking Update.
        /// </summary>
        public int UpdateInterval
        {
            get { return this.updateInterval; }
            set { this.updateInterval = value; }
        }

        private int zone = 0;
        /// <summary>
        /// Specifies the zone for the ad network.  This property is required and has no default.
        /// </summary>
        public int Zone
        {
            get { return this.zone; }
            set { this.zone = value; }
        }

        private bool useInternalBrowser = false;
        /// <summary>
        /// Set to enable the use of the internal browser for opening ad content.  Defaults to false.
        /// </summary>
        public bool UseInteralBrowser
        {
            get { return this.useInternalBrowser; }
            set { this.useInternalBrowser = value; }
        }

        private bool showCloseButton = false;
        /// <summary>
        /// Set to show a close button after the ad is rendered.
        /// 
        /// This can be used for both inline/banner/custom and interstitial ads.  For most cases this should not
        /// be required since banner ads don't usually have a need for a close button and rich media ads that expand
        /// or resize will off their own close button.
        /// 
        /// This SHOULD be used for interstitial ads that are known to not be rich media as they will not have a built
        /// in close button.
        /// 
        /// This setting applies for all subsequent updates.  The button can be customized using the 
        /// CloseButtonCustomSource property.
        /// </summary>
        public bool ShowCloseButton
        {
            get { return this.showCloseButton; }
            set { this.showCloseButton = value; }
        }

        private int closeButtonDelay = 0;
        /// <summary>
        /// The delay between ad rendering and displaying of the close button if ShowCloseButton is true.  Defaults to 0.
        /// </summary>
        public int CloseButtonDelay
        {
            get { return this.closeButtonDelay; }
            set { this.closeButtonDelay = value; }
        }

        private System.Windows.Media.ImageSource closeButtonCustomSource = null;
        /// <summary>
        /// The custom image source to use for customizing the close button.
        /// </summary>
        public System.Windows.Media.ImageSource CloseButtonCustomSource
        {
            get { return this.closeButtonCustomSource; }
            set { this.closeButtonCustomSource = value; }
        }

        public bool test = false;
        /// <summary>
        /// Instructs the ad server to return test ads for the configured zone (if the zone has configured test ads).
        /// This should never be set to true for application releases.
        /// </summary>
        public bool Test
        {
            get { return this.test; }
            set { this.test = value; }
        }

        private bool allowBrowserGeolocation = false;
        /// <summary>
        /// Sets the IsGeolocationEnabled flag for WebBrowser based ads.
        /// </summary>
        public bool AllowBrowserGeolocation
        {
            get { return this.allowBrowserGeolocation; }
            set 
            {
                this.allowBrowserGeolocation = value;
                   
                if (this.webControl != null)
                    this.webControl.IsGeolocationEnabled = true;
            }
        }

        #endregion

        #region URL Handling

        private bool OpenURL(Uri uri)
        {
            if (uri == null)
                return false;

            string scheme = uri.Scheme;
            if (string.IsNullOrWhiteSpace(scheme))
                return false;

            scheme = scheme.ToLower();
            switch (scheme)
            {
                case "http":
                case "https":
                    if (this.useInternalBrowser == true)
                    {
                        ShowInternalBrowser(this.adDescriptor.URL);
                    }
                    else
                    {
                        OnLeavingApplication();

                        Microsoft.Phone.Tasks.WebBrowserTask task = new Microsoft.Phone.Tasks.WebBrowserTask();
                        task.Uri = uri;
                        task.Show();
                    }
                    return true;

                case "mailto":
                    {
                        string to = uri.UserInfo + "@" + uri.Host;
                        string subject = string.Empty;
                        string body = string.Empty;

                        string[] info = uri.Query.Trim(new char[] { '?' }).Split(new char[] { '&' });
                        foreach (string infoPart in info)
                        {
                            string[] infoParts = infoPart.Split(new char[] { '=' });
                            if (infoParts.Length != 2)
                                continue;

                            string key = Uri.UnescapeDataString(infoParts[0]).ToLower();
                            string value = Uri.UnescapeDataString(infoParts[1]);

                            switch (key)
                            {
                                case "subject":
                                    subject = value;
                                    break;
                                case "body":
                                    body = value;
                                    break;
                            }
                        }

                        OnLeavingApplication();

                        Microsoft.Phone.Tasks.EmailComposeTask task = new Microsoft.Phone.Tasks.EmailComposeTask();
                        task.To = to;
                        task.Subject = subject;
                        task.Body = body;
                        task.Show();
                    }
                    return true;

                case "tel":
                    {
                        string path = uri.AbsolutePath;
                        string[] pathParts = path.Split(new char[] { ';' });
                        string number = pathParts[0];

                        OnLeavingApplication();

                        Microsoft.Phone.Tasks.PhoneCallTask task = new Microsoft.Phone.Tasks.PhoneCallTask();
                        task.PhoneNumber = uri.UserInfo;
                        task.Show();
                    }
                    return true;

                case "sms":
                    {
                        string number = uri.AbsolutePath;
                        string body = string.Empty;

                        string[] info = uri.Query.Trim(new char[] { '?' }).Split(new char[] { '&' });
                        foreach (string infoPart in info)
                        {
                            string[] infoParts = infoPart.Split(new char[] { '=' });
                            if (infoParts.Length != 2)
                                continue;

                            string key = Uri.UnescapeDataString(infoParts[0]).ToLower();
                            string value = Uri.UnescapeDataString(infoParts[1]);

                            if ("body" == key)
                                body = value;
                        }

                        OnLeavingApplication();

                        Microsoft.Phone.Tasks.SmsComposeTask task = new Microsoft.Phone.Tasks.SmsComposeTask();
                        task.To = number;
                        task.Body = body;
                        task.Show();
                    }
                    return true;

                default:
                    break;
            }

            return false;
        }

        #endregion

        #region Location Services

        System.Device.Location.GeoCoordinateWatcher geoCoordinateWatcher = null;

        /// <summary>
        /// Enables or disables SDK location detection support using the most battery efficient options.
        /// Use the EnableLocationDetection method for more fined tuned detection options during enablement.
        /// In either case setting this property to false turns off SDK based location detection.
        /// 
        /// See GeoCoordinateWatcher MSDN documentation for more information on how location is determined.
        /// 
        /// Note that this functionality is only used to set the lat and long parameters in the AdRequestParameters 
        /// dictionary and may also override anything set manually.  If manually setting lat and long parameters
        /// then then location detection should be disabled.
        /// 
        /// Requires these capabilities:
        /// ID_CAP_NETWORKING -Windows Phone 8, Windows Phone OS 7.1
        /// ID_CAP_LOCATION - Windows Phone 8
        /// </summary>
        public bool LocationDetectionEnabled
        {
            get
            {
                if (this.geoCoordinateWatcher == null)
                    return false;

                if (this.geoCoordinateWatcher.Permission == System.Device.Location.GeoPositionPermission.Denied)
                    return false;

                if (this.geoCoordinateWatcher.Status == System.Device.Location.GeoPositionStatus.Disabled)
                    return false;

                return true;
            }

            set
            {
                if (value)
                {
                    EnableLocationDetection(System.Device.Location.GeoPositionAccuracy.Default, Defaults.LOCATION_DETECTION_MOVEMENT_THRESHOLD, false);
                }
                else
                {
                    if (this.geoCoordinateWatcher != null)
                    {
                        this.geoCoordinateWatcher.Stop();
                        this.geoCoordinateWatcher.Dispose();
                        this.geoCoordinateWatcher = null;
                    }
                }
            }
        }

        /// <summary>
        /// Enables location detection using the specified configuration.  To disable use the LocationDetectionEnabled property.
        /// 
        /// See the EnableLocationDetection for more information.
        /// </summary>
        /// <param name="desiredAccuracy">Sets the desired accuracy for the GeoCoordinateWatcher.</param>
        /// <param name="movementThreshold">Sets the movement threshold for the GeoCoordinateWatcher.</param>
        /// <param name="suppressPermissionPrompt">Determines if the permission prompt should be suprressed.</param>
        public void EnableLocationDetection(System.Device.Location.GeoPositionAccuracy desiredAccuracy, double movementThreshold, bool suppressPermissionPrompt)
        {
            if (this.geoCoordinateWatcher != null)
            {
                this.geoCoordinateWatcher.Stop();
                this.geoCoordinateWatcher.Dispose();
                this.geoCoordinateWatcher = null;
            }

            this.geoCoordinateWatcher = new System.Device.Location.GeoCoordinateWatcher(desiredAccuracy);
            this.geoCoordinateWatcher.MovementThreshold = movementThreshold;
            
            this.geoCoordinateWatcher.StatusChanged += geoCoordinateWatcher_StatusChanged;
            this.geoCoordinateWatcher.PositionChanged += geoCoordinateWatcher_PositionChanged;

            this.geoCoordinateWatcher.Start(suppressPermissionPrompt);
        }

        private void geoCoordinateWatcher_StatusChanged(object sender, System.Device.Location.GeoPositionStatusChangedEventArgs e)
        {
            if (sender != this.geoCoordinateWatcher)
                return;

            if (e.Status == System.Device.Location.GeoPositionStatus.Ready)
                return;
            
            if (e.Status == System.Device.Location.GeoPositionStatus.Disabled)
            {
                this.geoCoordinateWatcher.Stop();
                this.geoCoordinateWatcher.Dispose();
                this.geoCoordinateWatcher = null;
            }

            // Anything else remove the current values if set.
            this.AdRequestParameters.Remove("lat");
            this.AdRequestParameters.Remove("long");
        }

        private void geoCoordinateWatcher_PositionChanged(object sender, System.Device.Location.GeoPositionChangedEventArgs<System.Device.Location.GeoCoordinate> e)
        {
            if (sender != this.geoCoordinateWatcher)
                return;

            if (e.Position.Location.IsUnknown)
            {
                this.AdRequestParameters.Remove("lat");
                this.AdRequestParameters.Remove("long");
                return;
            }

            this.AdRequestParameters["lat"] = e.Position.Location.Latitude.ToString();
            this.AdRequestParameters["long"] = e.Position.Location.Longitude.ToString();
        }

        #endregion

        #region Updating

        private bool userAgentResume = false;
        private System.Threading.Timer updateTimer = null;
        private AdRequest adRequest = null;
        private AdDescriptor adDescriptor = null;
        private bool invokeAdTracking = false;
        private bool updateDeferred = false;

        /// <summary>
        /// Initiates an ad content update on the instance.
        /// Will defer the update if the user is interacting with the ad until after the user is done interacting with the ad.
        /// </summary>
        public void Update()
        {
            Update(false);
        }

        /// <summary>
        /// Initiates an ad content update on the instance.
        /// Pass true to force an update even if the user is interacting with the ad.
        /// </summary>
        /// <param name="force"></param>
        public void Update(bool force)
        {
            if (updateTimer == null)
            {
                updateTimer = new System.Threading.Timer(new System.Threading.TimerCallback(UpdateTimerCallback),
                    null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

            if (this.updateInterval == 0)
            {
                updateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
            else
            {
                updateTimer.Change(0, this.updateInterval * 1000);
            }

            if (force)
            {
                if (IsInternalBrowserOpen)
                    CloseInternalBrowser();

                if (this.mraidBridge != null)
                {
                    switch (this.mraidBridge.State)
                    {
                        case State.Loading:
                        case State.Default:
                        case State.Hidden:
                            break;

                        case State.Expanded:
                        case State.Resized:
                            ((BridgeHandler)this).mraidClose(this.mraidBridge);
                            break;
                    }
                }

                if (IsExpanded)
                    CollapseExpandPopup();

                if (IsResized)
                    CollapseResizePopup();
            }
            
            InternalUpdate();
        }

        /// <summary>
        /// Resets the instance to it's default state.
        /// - Stops any updates and update interval.
        /// - Stops location detection.
        /// - Removes ad content (see RemoveContent)
        /// </summary>
        public void Reset()
        {
            this.updateDeferred = false;

            if (updateTimer != null)
            {
                updateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

            this.LocationDetectionEnabled = false;

            RemoveContent();
        }

        private void UpdateTimerCallback(object state)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(InternalUpdate);
        }

        private void InternalUpdate()
        {
            if (IsExpanded || IsResized)
            {
                this.updateDeferred = true;
                return;
            }

            CancelUpdate();

            if (string.IsNullOrEmpty(UserAgent))
            {
                this.userAgentResume = true;
                return;
            }

            if (this.zone == 0)
            {
                string message = "Can not update without a configured zone.";
                LogEvent(LogLevel.Error, message);
                OnAdFailed(new InvalidOperationException(message));
                return;
            }
            
            Dictionary<string, string> args = new Dictionary<string, string>();

            System.Windows.Size size = base.RenderSize;
            if (this.placementType == mast.PlacementType.Interstitial)
            {
                size.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
                size.Height = System.Windows.Application.Current.Host.Content.ActualHeight;
            }

            // These can be overridden but set to good defaults.
            args.Add("size_x", size.Width.ToString());
            args.Add("size_y", size.Height.ToString());

            foreach (KeyValuePair<string, string> kvp in this.adRequestParameters)
            {
                args[kvp.Key] = kvp.Value;
            }

            // Don't allow these to be overridden.
            args.Add("ua", UserAgent);
            args.Add("version", Defaults.VERSION);
            args.Add("count", "1");
            args.Add("key", "3");
            args.Add("zone", this.zone.ToString());

            if (this.test)
                args.Add("test", "1");

            string url = this.AdServerURL;
            int timeout = Defaults.NETWORK_TIMEOUT_MILLISECONDS;

            this.adRequest = AdRequest.Create(timeout, url, UserAgent, args,
                new AdRequestCompleted(adRequestCompleted),
                new AdRequestError(adRequestError),
                new AdRequestFailed(adRequestFailed));

            LogEvent(LogLevel.Debug, "Ad request:" + this.adRequest.URL);
        }

        // Invoked after determining the user agent.
        private void ResumeInternalUpdate()
        {
            if (this.userAgentResume == false)
                return;

            this.userAgentResume = false;
            InternalUpdate();
        }

        private void CancelUpdate()
        {
            if (this.adRequest != null)
            {
                this.adRequest.Cancel();
                this.adRequest = null;
            }
        }

        private void adRequestFailed(AdRequest request, Exception exception)
        {
            LogEvent(LogLevel.Error, exception.Message);
            OnAdFailed(exception);
        }

        private void adRequestError(AdRequest request, string errorCode, string errorMessage)
        {
            string message = string.Format("Error response from server.  Error code:{0} - Error message:{1} - Request:{2}",
                errorCode, errorMessage, request.URL);

            LogLevel level = mast.LogLevel.Error;
            if (errorCode == "404")
                level = mast.LogLevel.Debug;

            LogEvent(level, message);
            OnAdFailed(new Exception(message));
        }

        private void adRequestCompleted(AdRequest adRequest, AdDescriptor adDescriptor)
        {
            if (adRequest != this.adRequest)
                return;

            if (adDescriptor == null)
            {
                string message = "No ad available in response.";
                LogEvent(LogLevel.Error, message);
                OnAdFailed(new Exception(message));
                return;
            }

            RenderWithAdDescriptor(adDescriptor);
        }

        private void PerformAdTracking()
        {
            if (this.invokeAdTracking == false)
                return;

            if (this.adDescriptor == null)
                return;

            this.invokeAdTracking = false;

            string url = this.adDescriptor.Track;
            if (string.IsNullOrEmpty(url))
                return;

            AdTracking.InvokeTrackingURL(url, UserAgent);
        }

        #endregion

        #region Interstitial

        private System.Threading.Timer interstitialCloseTimer = null;

        /// <summary>
        /// Shows the interstitial ad container.
        /// If not yet received and rendered by be blank until the ad is rendered.
        /// 
        /// Can not be used on inline instances.
        /// </summary>
        public void ShowInterstitial()
        {
            if (this.placementType != mast.PlacementType.Interstitial)
                return;

            OpenExpandPopup(null);

            PerformAdTracking();

            if (this.mraidBridge != null)
            {
                this.mraidBridge.SetViewable(true);
                this.mraidBridge.SetState(State.Default);
            }

            if (interstitialCloseTimer == null)
            {
                interstitialCloseTimer = new System.Threading.Timer(new System.Threading.TimerCallback(InterstitialCloseTimerCallback),
                    null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }

            PrepareCloseButton();

            if (this.interstitialDuration < 1)
            {
                interstitialCloseTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                return;
            }

            interstitialCloseTimer.Change(this.interstitialDuration * 1000, System.Threading.Timeout.Infinite);
        }

        private int interstitialDuration = 0;
        /// <summary>
        /// The amount of time to show the interstitial after invoking ShowInterstitial.
        /// </summary>
        public int InterstitialDuration
        {
            get { return this.interstitialDuration; }
            set { this.interstitialDuration = value; }
        }

        /// <summary>
        /// Closes the interstitial ad container.
        /// </summary>
        public void CloseInterstitial()
        {
            if (interstitialCloseTimer != null)
                interstitialCloseTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

            CollapseExpandPopup();

            if (this.mraidBridge != null)
            {
                this.mraidBridge.SetViewable(false);
                this.mraidBridge.SetState(State.Hidden);
            }
        }

        private void InterstitialCloseTimerCallback(object state)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(CloseInterstitial);
        }

        #endregion

        #region Ad Rendering

        /// <summary>
        /// For SDK testing.  Allows rendering an AdDescriptor without obtaining one from the ad network.
        /// Can be invoked from a background thread.
        /// </summary>
        /// <param name="adDescriptor">The AdDescriptor to render.</param>
        public void RenderWithAdDescriptor(AdDescriptor adDescriptor)
        {
            if (adDescriptor.Type.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(LoadImageAd), adDescriptor);
                return;
            }

            if (adDescriptor.Type.StartsWith("text", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke((Action<AdDescriptor>)RenderTextAd,
                    new object[] { adDescriptor });

                return;
            }

            if (adDescriptor.Type.StartsWith("thirdparty", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(adDescriptor.URL) == false)
                {
                    if (string.IsNullOrWhiteSpace(adDescriptor.Image) == false)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(LoadImageAd), adDescriptor);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(adDescriptor.Text) == false)
                    {
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke((Action<AdDescriptor>)RenderTextAd,
                            new object[] { adDescriptor });

                        return;
                    }
                }
                else if (string.IsNullOrWhiteSpace(adDescriptor.Content) == false)
                {
                    if (adDescriptor.Content.Contains("client_side_external_campaign") == true)
                    {
                        ThirdPartyDescriptor thirdPartyDescriptor = 
                            ThirdPartyDescriptor.DescriptorFromClientSideExtrnalCampaign(adDescriptor.Content);

                        OnReceivedThirdPartyRequest(thirdPartyDescriptor.Properties, thirdPartyDescriptor.Params);
                        return;
                    }
                }
            }

            string contentString = adDescriptor.Content;

            if (string.IsNullOrWhiteSpace(contentString))
            {
                string message = "Ad descriptor missing ad content:" + adDescriptor;
                LogEvent(LogLevel.Error, message);
                OnAdFailed(new Exception(message));
                return;
            }

            // any other content is to be rendered as html/rich media.
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke((Action<AdDescriptor, string>)RenderMRAIDAd,
                    new object[] { adDescriptor, contentString });
        }

        #endregion

        #region Ad Rendering - Image

        // should be in the background
        private void LoadImageAd(object state)
        {
            AdDescriptor adDescriptor = state as AdDescriptor;

            try
            {
                System.Net.WebRequest request = System.Net.WebRequest.Create(adDescriptor.Image);

                if (request is System.Net.HttpWebRequest)
                    ((System.Net.HttpWebRequest)request).UserAgent = UserAgent;

                request.BeginGetResponse(new AsyncCallback(LoadImageAdResponse), new object[]{request, adDescriptor});
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void LoadImageAdResponse(IAsyncResult ar)
        {
            System.Net.WebRequest request = ((object[])ar.AsyncState)[0] as System.Net.WebRequest;
            AdDescriptor adDescriptor = ((object[])ar.AsyncState)[1] as AdDescriptor;

            System.Net.WebResponse response = request.EndGetResponse(ar);
            System.IO.Stream stream = response.GetResponseStream();

            bool isGIF = false;
            byte[] buffer = new byte[3];
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                if ((buffer[0] == 'G') && (buffer[1] == 'I') && (buffer[2] == 'F'))
                {
                    isGIF = true;
                }
            }
            stream.Seek(0, System.IO.SeekOrigin.Begin);

            if (isGIF)
            {
                ImageTools.IO.Decoders.AddDecoder<ImageTools.IO.Gif.GifDecoder>();

                ImageTools.ExtendedImage extendedImage = new ImageTools.ExtendedImage();
                extendedImage.LoadingCompleted += extendedImage_LoadingCompleted;
                extendedImage.LoadingFailed += extendedImage_LoadingFailed;
                extendedImage.SetSource(stream);
                return;
            }

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke((Action<AdDescriptor, System.IO.Stream>)RenderImageAd,
                new object[] { adDescriptor, stream });
        }

        private void extendedImage_LoadingFailed(object sender, UnhandledExceptionEventArgs e)
        {
            string message = "Can not load extended image (GIF).  Exception:" + e;
            LogEvent(LogLevel.Error, message);
            OnAdFailed(new InvalidOperationException(message));
        }

        private void extendedImage_LoadingCompleted(object sender, EventArgs e)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke((Action<AdDescriptor, ImageTools.ExtendedImage>)RenderImageAd,
                new object[] { adDescriptor, sender });
        }

        // called from main thread/dispatcher
        private void RenderImageAd(AdDescriptor adDescriptor, System.IO.Stream imageStream)
        {
            try
            {
                System.Windows.Media.Imaging.BitmapImage imageSource = new System.Windows.Media.Imaging.BitmapImage();
                imageSource.SetSource(imageStream);
                this.imageControl.Source = imageSource;
            }
            catch (Exception ex)
            {
                string message = "Can not load image.  Exception:" + ex;
                LogEvent(LogLevel.Error, message);
                OnAdFailed(new InvalidOperationException(message));
                return;
            }
            
            this.imageBorder.Child = this.imageControl;

            this.invokeAdTracking = true;
            this.mraidBridge = null;
            this.adDescriptor = adDescriptor;

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    base.Children.Clear();
                    base.Children.Add(this.imageBorder);
                    PrepareCloseButton();
                    break;

                case mast.PlacementType.Interstitial:
                    this.expandCanvas.Children.Clear();
                    this.expandCanvas.Children.Add(this.imageBorder);
                    break;
            }

            OnAdReceived();
        }

        // called from main thread/dispatcher
        private void RenderImageAd(AdDescriptor adDescriptor, ImageTools.ExtendedImage extendedImage)
        {
            this.animatedImageControl.Source = extendedImage;

            this.imageBorder.Child = this.animatedImageControl;

            this.invokeAdTracking = true;
            this.mraidBridge = null;
            this.adDescriptor = adDescriptor;

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    base.Children.Clear();
                    base.Children.Add(this.imageBorder);
                    PrepareCloseButton();
                    break;

                case mast.PlacementType.Interstitial:
                    this.expandCanvas.Children.Clear();
                    this.expandCanvas.Children.Add(this.imageBorder);
                    break;
            }

            OnAdReceived();
        }

        #endregion

        #region Ad Rendering - Text

        // called from the main thread/dispatcher
        private void RenderTextAd(AdDescriptor adDescriptor)
        {
            this.textControl.Text = adDescriptor.Text;

            this.invokeAdTracking = true;
            this.mraidBridge = null;
            this.adDescriptor = adDescriptor;

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    base.Children.Clear();
                    base.Children.Add(this.textBorder);
                    PrepareCloseButton();
                    break;

                case mast.PlacementType.Interstitial:
                    this.expandCanvas.Children.Clear();
                    this.expandCanvas.Children.Add(this.textBorder);
                    break;
            }

            OnAdReceived();
        }

        #endregion

        #region Ad Rendering - Rich Media

        private Bridge mraidBridge = null;

        // support for two part expand with url
        private Boolean twoPartExpand = false;
        private Border twoPartWebBorder = null;
        private WebBrowser twoPartWebBrowser = null;
        private Bridge twoPartMraidBridge = null;
        
        private bool RenderMRAIDTwoPartExpand(string url)
        {
            // TODO: For now commenting all of this out.  Need to find a better way of 
            // injecting the bridge without having to load the URL as a string.
            //// Fetch the URL contents and inject MRAID bridge code then load into the browser.
            //System.Net.HttpWebRequest twoPartRequest = null;
            //try
            //{
            //    twoPartRequest = System.Net.HttpWebRequest.CreateHttp(url);
            //    twoPartRequest.AllowAutoRedirect = true;
            //    twoPartRequest.UserAgent = UserAgent;

            //    twoPartRequest.BeginGetResponse(delegate(IAsyncResult ar)
            //    {
            //        try
            //        {
            //            System.Net.WebResponse response = twoPartRequest.EndGetResponse(ar);
            //            System.IO.Stream stream = response.GetResponseStream();

            //            System.IO.StreamReader streamReader = new System.IO.StreamReader(stream);
            //            string content = streamReader.ReadToEnd();

            //            int insertScriptAt = -1;

            //            int htmlIndex = content.IndexOf("<html");
            //            if (htmlIndex > -1)
            //            {
            //                int htmlEndIndex = content.IndexOf('>', htmlIndex);
            //                if (htmlEndIndex > -1)
            //                {
            //                    insertScriptAt = htmlEndIndex + 1;

            //                    int headIndex = content.IndexOf("<head", htmlEndIndex);
            //                    if (headIndex > -1)
            //                    {
            //                        int headEndIndex = content.IndexOf('>', headIndex);
            //                        if (headEndIndex > -1)
            //                        {
            //                            insertScriptAt = headEndIndex + 1;
            //                        }
            //                    }
            //                }
            //            }

            //            if (insertScriptAt > -1)
            //            {
            //                System.IO.Stream resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(Defaults.RICHMEDIA_SCRIPT_RESOURCE);
            //                System.IO.StreamReader resourceStreamReader = new System.IO.StreamReader(resourceStream);
            //                string mraidScript = resourceStreamReader.ReadToEnd();

            //                mraidScript = "\n<script>" + mraidScript + "</script>\n";

            //                content = content.Insert(insertScriptAt, mraidScript);

            //                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate
            //                {
            //                    this.twoPartWebBrowser.NavigateToString(content);
            //                });
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate
            //            {
            //                ((BridgeHandler)this).mraidClose(this.mraidBridge);
            //            });
                        
            //            LogEvent(mast.LogLevel.Error, "Exception while trying to load two part creative. Ex:" + ex.Message);
            //        }
            //    }, null);
            //}
            //catch (Exception ex)
            //{
            //    LogEvent(mast.LogLevel.Error, "Exception while trying to load two part creative. Ex:" + ex.Message);
            //    return false;
            //}

            this.twoPartExpand = true;
            
            this.twoPartWebBrowser = new WebBrowser();
            this.twoPartWebBrowser.IsGeolocationEnabled = this.allowBrowserGeolocation;
            this.twoPartWebBrowser.IsScriptEnabled = true;
            this.twoPartWebBrowser.NavigationFailed += webControl_NavigationFailed;
            this.twoPartWebBrowser.Navigating += webControl_Navigating;
            this.twoPartWebBrowser.Navigated += webControl_Navigated;
            this.twoPartWebBrowser.LoadCompleted += webControl_LoadCompleted;
            this.twoPartWebBrowser.ScriptNotify += webControl_ScriptNotify;
            this.twoPartWebBrowser.SizeChanged += webControl_SizeChanged;
            
            this.twoPartWebBorder = new Border();
            this.twoPartWebBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            this.twoPartWebBorder.Child = this.twoPartWebBrowser;
            this.twoPartWebBorder.SizeChanged += border_SizeChanged;

            OpenExpandPopup(twoPartWebBorder);

            this.twoPartMraidBridge = new Bridge(this, this.twoPartWebBrowser);

            // TODO: Simple load vs. the above commented out chunk.
            this.twoPartWebBrowser.Navigate(new Uri(url));

            return true;
        }

        // called from the main thread/dispatcher
        private void RenderMRAIDAd(AdDescriptor adDescriptor, string mraidHtml)
        {
            System.IO.Stream resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(Defaults.RICHMEDIA_SCRIPT_RESOURCE);
            System.IO.StreamReader resourceStreamReader = new System.IO.StreamReader(resourceStream);
            string mraidScript = resourceStreamReader.ReadToEnd();

            string htmlContent = string.Format(Defaults.RICHMEDIA_FORMAT, mraidScript, mraidHtml);
            
            this.webControl.Base = string.Empty;
            this.webControl.NavigateToString(htmlContent);

            this.mraidBridge = new Bridge(this, this.webControl);

            this.invokeAdTracking = true;
            this.adDescriptor = adDescriptor;

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    base.Children.Clear();
                    base.Children.Add(this.webBorder);
                    break;

                case mast.PlacementType.Interstitial:
                    this.expandCanvas.Children.Clear();
                    this.expandCanvas.Children.Add(this.webBorder);
                    break;
            }

            OnAdReceived();
        }

        private void webControl_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {

        }

        private void webControl_Navigating(object sender, NavigatingEventArgs e)
        {
            WebBrowser webBrowser = (WebBrowser)sender;

            bool isTwoPart = false;
            Bridge bridge = this.mraidBridge;
            if (webBrowser == this.twoPartWebBrowser)
            {
                isTwoPart = true;
                bridge = this.twoPartMraidBridge;
            }

            Uri uri = e.Uri;
            if ((uri == null) || string.IsNullOrEmpty(uri.AbsolutePath))
                return;

            string scheme = uri.Scheme.ToLower();
            if (string.IsNullOrEmpty(scheme))
                return;

            if (("javascript" == scheme) || ("about" == scheme))
                return;

            if (isTwoPart && scheme.StartsWith("http"))
            {
                if (this.twoPartExpand && (this.twoPartMraidBridge.State == State.Loading))
                    return;
            }

            string url = uri.ToString();
            if (OnOpeningURL(url) && OpenURL(uri))
            {
                e.Cancel = true;
                return;
            }

            // TODO: What about iframes?

            // Don't let the browser navigate to some other page.
            if (scheme.StartsWith("http"))
                e.Cancel = true;
        }
        
        private void webControl_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //WebBrowser webBrowser = (WebBrowser)sender;
            //string content = webBrowser.SaveToString();

            //Bridge bridge = this.mraidBridge;
            //if (webBrowser == this.twoPartWebBrowser)
            //    bridge = this.twoPartMraidBridge;
        }

        private void webControl_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            WebBrowser webBrowser = (WebBrowser)sender;
            //string content = webBrowser.SaveToString();

            Bridge bridge = this.mraidBridge;
            if (webBrowser == this.twoPartWebBrowser)
                bridge = this.twoPartMraidBridge;

            // TODO: Not using MRAID with two part since the bridge code can't be injected into the page.
            // Remove when a solution is found/implemented that will result in MRAID being injected.
            if (bridge == this.twoPartMraidBridge)
                return;

            MRAIDControllerInit(webBrowser, bridge);
        }

        private void webControl_ScriptNotify(object sender, Microsoft.Phone.Controls.NotifyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("ScriptNotify:" + e.Value);

            WebBrowser webBrowser = (WebBrowser)sender;
            //string content = webBrowser.SaveToString();

            Bridge bridge = this.mraidBridge;
            if (webBrowser == this.twoPartWebBrowser)
                bridge = this.twoPartMraidBridge;

            if (bridge == null)
                return;

            Uri uri = new Uri(e.Value);

            if (bridge.State != State.Loading)
            {
                if (uri.Scheme == mraid.Const.Scheme)
                {
                    bool handled = bridge.ParseRequest(uri);
                    OnProcessedRichMediaRequest(uri, handled);
                }
            }
        }

        private void webControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                WebBrowser browser = sender as WebBrowser;
                browser.InvokeScript("eval", new string[] { "window.scrollTo(0,0);" });
            }
            catch (Exception)
            {
                // TODO: Bother with these?
            }
        }

        private void MRAIDControllerInit(WebBrowser webBrowser, Bridge bridge)
        {
            if (bridge == null)
                return;

            SetMRAIDSupportedFeatures(bridge);

            switch (this.placementType)
            {
                case mast.PlacementType.Inline:
                    bridge.SetPlacementType(mraid.PlacementType.Inline);
                    break;

                case mast.PlacementType.Interstitial:
                    bridge.SetPlacementType(mraid.PlacementType.Interstitial);
                    break;
            }

            ExpandProperties expandProperties = new ExpandProperties();
            expandProperties.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
            expandProperties.Height = System.Windows.Application.Current.Host.Content.ActualHeight;
            bridge.SetExpandProperties(expandProperties);

            OrientationProperties orientationProperties = new OrientationProperties();
            bridge.SetOrientationProperties(orientationProperties);

            ResizeProperties resizeProperties = new ResizeProperties();
            bridge.SetResizeProperties(resizeProperties);

            MRAIDControllerLayoutUpdate(bridge, (Border)webBrowser.Parent);

            if (this.twoPartExpand == false)
            {
                bridge.SetState(State.Default);
            }
            else
            {
                ((BridgeHandler)this).mraidExpand(bridge, null);
            }

            bridge.SendReady();
        }

        private void MRAIDControllerLayoutUpdate(Bridge bridge, Border border)
        {
            UpdateLayouts();

            double screenWidth = System.Windows.Application.Current.Host.Content.ActualWidth;
            double screenHeight = System.Windows.Application.Current.Host.Content.ActualHeight;
            double pageWidth = this.phoneApplicationPage.RenderSize.Width;
            double pageHeight = this.phoneApplicationPage.RenderSize.Height;

            // Represents the entire screen.
            bridge.SetScreenSize(screenWidth, screenHeight);           

            // When expanded the full screen is the max size, else the max size is constrained
            // to the page since the status bar (tray) and application bar may take up space.
            System.Windows.Point currentPoint = new System.Windows.Point(0, 0);
            if (bridge.State == State.Expanded)
            {
                bridge.SetMaxSize(screenWidth, screenHeight);

                // The currentPoint for expand is 0,0 here since it's full screen.
            }
            else
            {
                bridge.SetMaxSize(pageWidth, pageHeight);

                // The currentPoint needs to be based off the page in this case.
                currentPoint = border.TransformToVisual(this.phoneApplicationPage).Transform(currentPoint);
            }

            // The currentPosition is relative to the maxSize.
            double currentWidth = this.webBorder.Width;
            double currentHeight = this.webBorder.Height;
            bridge.SetCurrentPosition(currentPoint.X, currentPoint.Y, currentWidth, currentHeight);

            // Unlike currentPosition, default point represents where the developer placed the ad reagardless of state.
            System.Windows.Point defaultPoint = base.TransformToVisual(this.phoneApplicationPage).Transform(new System.Windows.Point(0, 0));
            double defaultWidth = base.ActualWidth;
            double defaultHeight = base.ActualHeight;
            bridge.SetDefaultPosition(defaultPoint.X, defaultPoint.Y, defaultWidth, defaultHeight);

            // TODO: May need to move this logic to post-load or elsewhere... not sure if it's proper to invoke
            // as often as this may be called for size updates.
            bool viewable = base.Visibility == System.Windows.Visibility.Visible;
            if (this.placementType == mast.PlacementType.Interstitial)
                viewable = this.IsExpanded;

            bridge.SetViewable(viewable);
        }

        private void SetMRAIDSupportedFeatures(Bridge bridge)
        {
            bridge.SetSupportedFeature(Feature.SMS, true);
            bridge.SetSupportedFeature(Feature.Tel, true);
            bridge.SetSupportedFeature(Feature.Calendar, false);
            bridge.SetSupportedFeature(Feature.StorePicture, true);
            bridge.SetSupportedFeature(Feature.InlineVideo, true);
        }

        #endregion

        #region Bridge Handler

        void BridgeHandler.mraidClose(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;
            
            if (this.placementType == mast.PlacementType.Interstitial)
            {
                OnCloseButtonPressed();
                return;
            }

            if (this.IsExpanded)
            {
                CollapseExpandPopup();

                if (this.twoPartExpand == false)
                {
                    this.webBorder.Width = base.ActualWidth;
                    this.webBorder.Height = base.ActualHeight;

                    base.Children.Add(border);

                    MRAIDControllerLayoutUpdate(bridge, border);
                    bridge.SetState(State.Default);
                }
                else
                {
                    this.twoPartExpand = false;
                    this.twoPartWebBorder = null;
                    this.twoPartWebBrowser = null;
                    this.twoPartMraidBridge = null;

                    this.mraidBridge.SetState(State.Default);
                }

                OnAdCollapsed();
            }

            if (this.IsResized)
            {
                CollapseResizePopup();

                this.webBorder.Width = base.ActualWidth;
                this.webBorder.Height = base.ActualHeight;

                base.Children.Add(border);

                MRAIDControllerLayoutUpdate(bridge, border);
                bridge.SetState(State.Default);

                OnAdCollapsed();
                return;
            }

            // Nothing to close but reset state since the ad creative wanted a close.
            MRAIDControllerLayoutUpdate(bridge, border);
            bridge.SetState(State.Default);
        }

        void BridgeHandler.mraidOpen(Bridge bridge, string url)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (OnOpeningURL(url))
            {
                Uri uri = new Uri(url);
                OpenURL(uri);
            }
        }

        void BridgeHandler.mraidUpdateCurrentPosition(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            MRAIDControllerLayoutUpdate(bridge, border);
        }

        void BridgeHandler.mraidUpdatedExpandProperties(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            // Nothing to do since properties only affect pre-expand anyway.
        }

        void BridgeHandler.mraidExpand(Bridge bridge, string url)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (this.placementType == mast.PlacementType.Interstitial)
            {
                bridge.SendErrorMessage("Can not expand with placementType interstitial.", Const.CommandExpand);
                return;
            }

            bool hasUrl = !string.IsNullOrEmpty(url);

            switch (bridge.State)
            {
                case State.Loading:
                    if (this.twoPartExpand && (hasUrl == false))
                        // This is the two part expand after load, which is expected.
                        break;

                    bridge.SendErrorMessage("Can not expand while state is loading.", Const.CommandExpand);
                    return;
                
                case State.Default:
                case State.Hidden:
                    break;
                
                case State.Expanded:
                    bridge.SendErrorMessage("Can not expand while state is expanded.", Const.CommandExpand);
                    return;

                case State.Resized:
                    if (this.IsResized)
                    {
                        CollapseResizePopup();
                    }
                    break;
            }

            // The first time through for a two part expand this block is necessary, else don't do it.
            if (this.twoPartExpand == false)
            {
                if (hasUrl == false)
                {
                    base.Children.Remove(border);
                    OpenExpandPopup(border);

                    MRAIDControllerLayoutUpdate(bridge, border);
                }
                else
                {
                    if (RenderMRAIDTwoPartExpand(url) == false)
                    {
                        bridge.SendErrorMessage("Unable to retreive specified URL.", Const.CommandExpand);
                        return;
                    }
                }
            }

            bridge.SetState(State.Expanded);

            PrepareCloseButton();

            OnAdExpanded();
        }

        void BridgeHandler.mraidUpdatedOrientationProperties(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (bridge.State != State.Expanded)
                return;

            switch (bridge.OrientationProperties.ForceOrientation)
            {
                case ForceOrientation.None:
                    break;

                case ForceOrientation.Portrait:
                    SetExpandOrientation(PageOrientation.PortraitUp, bridge, border);
                    break;

                case ForceOrientation.Landscape:
                    SetExpandOrientation(PageOrientation.LandscapeLeft, bridge, border);
                    break;
            }
        }

        void BridgeHandler.mraidUpdatedResizeProperties(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;
        }

        void BridgeHandler.mraidResize(Bridge bridge)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (this.placementType == mast.PlacementType.Interstitial)
            {
                mraidBridge.SendErrorMessage("Can not resize with placementType interstitial.", Const.CommandResize);
                return;
            }

            switch (bridge.State)
            {
                case State.Loading:
                case State.Hidden:
                case State.Expanded:
                    bridge.SendErrorMessage("Can not resize loading, hidden or expanded.", Const.CommandResize);
                    return;

                case State.Default:
                case State.Resized:
                    break;
            }

            double screenWidth = this.phoneApplicationPage.RenderSize.Width;
            double screenHeight = this.phoneApplicationPage.RenderSize.Height;

            double x = bridge.ResizeProperties.OffsetX;
            double y = bridge.ResizeProperties.OffsetY;
            double width = bridge.ResizeProperties.Width;
            double height = bridge.ResizeProperties.Height;

            if ((width >= screenWidth) || (height >= screenHeight))
            {
                bridge.SendErrorMessage("Size must be smaller than the max size.", Const.CommandResize);
                return;
            }
            else if ((width < CloseAreaSize) || (height < CloseAreaSize))
            {
                bridge.SendErrorMessage("Size must be at least the minimum close area size.", Const.CommandResize);
                return;
            }

            if (bridge.ResizeProperties.AllowOffscreen == false)
            {
                System.Windows.Point currentPoint = base.TransformToVisual(this.phoneApplicationPage).Transform(new System.Windows.Point(0, 0));
                double desiredScreenX = currentPoint.X + x;
                double desiredScreenY = currentPoint.Y + y;
                double resultingScreenX = desiredScreenX;
                double resultingScreenY = desiredScreenY;

                if (width > screenWidth)
                    width = screenWidth;

                if (height > screenHeight)
                    height = screenHeight;

                if (desiredScreenX < 0)
                {
                    resultingScreenX = 0;
                }
                else if ((desiredScreenX + width) > screenWidth)
                {
                    double diff = desiredScreenX + width - screenWidth;
                    resultingScreenX -= diff;
                }

                if (desiredScreenY < 0)
                {
                    resultingScreenY = 0;
                }
                else if ((desiredScreenY + height) > screenHeight)
                {
                    double diff = desiredScreenY + height - screenHeight;
                    resultingScreenY -= diff;
                }

                double adjustedX = desiredScreenX - resultingScreenX;
                double adjustedY = desiredScreenY - resultingScreenY;
                x -= adjustedX;
                y -= adjustedY;
            }

            // Determine where the close control area will be.  This MUST be on screen.
            // By default it is in the top right but the ad can specify where it should be.
            // The ad MUST provide the graphic for it or some other means to close the resize.
            Rect closeControlRect = new Rect(x + width - CloseAreaSize, y, 
                CloseAreaSize, CloseAreaSize);

            switch (bridge.ResizeProperties.CustomClosePosition)
            {
                case CustomClosePosition.TopRight:
                    // Already configured above.
                    break;

                case CustomClosePosition.TopCenter:
                    closeControlRect = new Rect(width/2 - CloseAreaSize/2, 0, 
                        CloseAreaSize, CloseAreaSize);
                    break;

                case CustomClosePosition.TopLeft:
                    closeControlRect = new Rect(0, 0, CloseAreaSize, CloseAreaSize);
                    break;

                case CustomClosePosition.BottomLeft:
                    closeControlRect = new Rect(0, height - CloseAreaSize, 
                        CloseAreaSize, CloseAreaSize);
                    break;

                case CustomClosePosition.BottomCenter:
                    closeControlRect = new Rect(width/2 - CloseAreaSize/2, height - CloseAreaSize, 
                        CloseAreaSize, CloseAreaSize);
                    break;

                case CustomClosePosition.BottomRight:
                    closeControlRect = new Rect(width - CloseAreaSize, height - CloseAreaSize, 
                        CloseAreaSize, CloseAreaSize);
                    break;

                case CustomClosePosition.Center:
                    closeControlRect = new Rect(width / 2 - CloseAreaSize / 2, height/2 - CloseAreaSize/2, 
                        CloseAreaSize, CloseAreaSize);
                    break;
            }

            // TODO: Verify resize close control area will be on screen, if not error.

            if (this.IsResized == false)
            {
                base.Children.Remove(border);
                OpenResizePoup(border, x, y, width, height);
            }
            else
            {
                this.resizePopup.HorizontalOffset = x;
                this.resizePopup.VerticalOffset = y;
                this.resizePopup.Width = width;
                this.resizePopup.Height = height;
                border.Width = resizePopup.ActualWidth;
                border.Height = resizePopup.ActualHeight;
            }

            Canvas.SetLeft(this.resizeCloseBorder, closeControlRect.Left);
            Canvas.SetTop(this.resizeCloseBorder, closeControlRect.Top);

            MRAIDControllerLayoutUpdate(bridge, border);

            bridge.SetState(State.Resized);

            PrepareCloseButton();

            OnAdResized(new Rect(x, y, width, height));
        }

        void BridgeHandler.mraidPlayVideo(Bridge bridge, string url)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (OnPlayingVideo(url) == false)
                return;

            OnLeavingApplication();

            // TODO: Would use the media player but that seems to only support local media, not web URIs.
            Microsoft.Phone.Tasks.WebBrowserTask task = new Microsoft.Phone.Tasks.WebBrowserTask();
            task.Uri = new Uri(url);
            task.Show();
        }

        void BridgeHandler.mraidCreateCalendarEvent(Bridge bridge, string calendarEvent)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (OnSavingCalendarEvent(calendarEvent) == false)
                return;

            bridge.SendErrorMessage("Not supported by platform.", Const.CommandCreateCalendarEvent);

            LogEvent(mast.LogLevel.Error, "MRAID CreateCalendarEvent not supported by platform.");
        }

        // Requires capabilities
        // 8 - ID_CAP_MEDIALIB_PHOTO
        // 7.1 - ID_CAP_MEDIALIB
        void BridgeHandler.mraidStorePicture(Bridge bridge, string url)
        {
            Border border = this.webBorder;
            if (bridge == this.twoPartMraidBridge)
                border = this.twoPartWebBorder;

            if (OnSavingPhoto(url) == false)
                return;

            try
            {
                System.Net.HttpWebRequest request = System.Net.WebRequest.CreateHttp(url);
                request.BeginGetResponse(delegate(IAsyncResult ar)
                {
                    try
                    {
                        System.Net.WebResponse response = request.EndGetResponse(ar);
                        System.IO.Stream stream = response.GetResponseStream();

                        Microsoft.Xna.Framework.Media.MediaLibrary mediaLibrary = new Microsoft.Xna.Framework.Media.MediaLibrary();
                        mediaLibrary.SavePicture(string.Empty, stream);
                    }
                    catch (Exception ex)
                    {
                        bridge.SendErrorMessage("Error saving picture to device.", Const.CommandStorePicture);
                        LogEvent(mast.LogLevel.Error, "Exception while trying to save photo. Ex:" + ex.Message);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                bridge.SendErrorMessage("Network error connecting to url.", Const.CommandStorePicture);
                LogEvent(mast.LogLevel.Error, "Exception while trying to download photo. Ex:" + ex.Message);
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// See IDisposable.
        /// 
        /// Kills off timers and other WP APIs such as the GeoCoordinateWatcher.
        /// </summary>
        public void Dispose()
        {
            if (this.geoCoordinateWatcher != null)
            {
                this.geoCoordinateWatcher.Stop();
                this.geoCoordinateWatcher.Dispose();
                this.geoCoordinateWatcher = null;
            }

            if (this.interstitialCloseTimer != null)
            {
                this.interstitialCloseTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                this.interstitialCloseTimer.Dispose();
                this.interstitialCloseTimer = null;
            }

            if (this.closeButtonDelayTimer != null)
            {
                this.closeButtonDelayTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                this.closeButtonDelayTimer.Dispose();
                this.closeButtonDelayTimer = null;
            }

            if (this.updateTimer != null)
            {
                this.updateTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                this.updateTimer.Dispose();
                this.updateTimer = null;
            }
        }

        #endregion
    }
}
