﻿/****
 * © 2010-2011 mOcean Mobile. A subsidiary of Mojiva, Inc. All Rights Reserved.
 * */

using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;

namespace mOceanWindowsPhone
{
	public partial class AdView : UserControl, IDataRequestListener
	{
		#region "Constants"
		internal const string MOCEAN_SDK_DIR = "mocean";

		private const string SETTING_LAUNCHED = "moceanLaunched";
		private const string REG_LAUNCH_URL = "http://www.moceanmobile.com/appconversion.php";
		private const string REG_LAUNCH_RESPONSE = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><mojiva><result>OK</result></mojiva>";

		private const string DEFAULT_AD_SERVER = "http://ads.mocean.mobi/ad";
		//private const string CHECK_NEW_VERSION_URL = "http://www.moceanmobile.com/sdk_version.php?platform=wp7&version=" + SDK_VERSION;
		private const string NEW_VERSION_MESSAGE = "NEW VERSION MESSAGE";
		private const int DEFAULT_UPDATE_TIME = 120; // seconds
		private const string NO_SITE_ZONE_PARAMETERS_MESSAGE = "NO SITE ZONE PARAMETERS";
		private const string INVALID_PARAMETERS_RESPONSE = "<!-- invalid params -->";
		private const string INVALID_PARAMETERS_MESSAGE = "INVALID PARAMETERS";
		private const string EMPTY_CONTENT_MESSAGE = "";
		internal const int CUSTOM_CLOSE_BUTTON_SIZE = 50;
		#endregion

		#region Variables
		internal static volatile int adViewsCount = 0;
		private readonly int viewId = 0;
		private readonly string adFileName = String.Empty;
		private readonly string adExpandFileName = String.Empty;

		private string adContent = String.Empty;
		private readonly string version = String.Empty;

		private int site = 0;
		private int zone = 0;
		private string userAgent = null;
		private string deviceId = null;
		private int? advertiserId = null;
		private string groupCode = null;

		private AdserverRequest adserverRequest = new AdserverRequest();

		private Logger logger = null;
		private Logger.LogLevel logLevel = Logger.LogLevel.ErrorsOnly;

		private PhoneApplicationPage ownerPage = null;
		#endregion

		#region "Setting parameters"
		public int Site
		{
			get { return site; }
			set
			{
				if (value > 0)
				{
					site = value;
					adserverRequest.SetSite(site);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "site");
				}
			}
		}
		public int Zone
		{
			get { return zone; }
			set
			{
				if (value > 0)
				{
					zone = value;
					adserverRequest.SetZone(zone);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "zone");
				}
			}
		}

		private bool test = false;
		public bool Test
		{
			get { return test; }
			set
			{
				test = value;
				adserverRequest.SetTest(value);
			}
		}

		private int premium = 0;
		public int Premium
		{
			get { return premium; }
			set
			{
				if (value >= 0 && value <= 2)
				{
					premium = value;
					adserverRequest.SetPremium(value);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "premium");
				}
			}
		}

		private string keywords = null;
		public string Keywords
		{
			get { return keywords; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					keywords = value;
					adserverRequest.SetKeywords(keywords);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "keywords");
				}
			}
		}

		private int adsType = 0;
		public int AdsType
		{
			get { return adsType; }
			set
			{
				if (value == 1 || value == 2 || value == 3 || value == 6)
				{
					adsType = value;
					adserverRequest.SetAdsType(adsType);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "ads type");
				}
			}
		}

		private int type = 0;
		public int Type
		{
			get { return type; }
			set
			{
				if (value >= 1 && value <= 7)
				{
					type = value;
					adserverRequest.SetType(type);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "type");
				}
			}
		}

		private Size minSize = new Size(0, 0);
		public int MinSizeX
		{
			get { return (int)(minSize.Width); }
			set
			{
				if (value > 0)
				{
					minSize.Width = value;
					adserverRequest.SetMinSizeX(value);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "min size x");
				}
			}
		}
		public int MinSizeY
		{
			get { return (int)(minSize.Height); }
			set
			{
				if (value > 0)
				{
					minSize.Height = value;
					adserverRequest.SetMinSizeY(value);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "min size y");
				}
			}
		}

		private Size maxSize = new Size(0, 0);
		public int MaxSizeX
		{
			get { return (int)(maxSize.Width); }
			set
			{
				if (value > 0)
				{
					maxSize.Width = value;
					adserverRequest.SetMaxSizeX(value);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "max size x");
				}
			}
		}
		public int MaxSizeY
		{
			get { return (int)(maxSize.Height); }
			set
			{
				if (value > 0)
				{
					maxSize.Height = value;
					adserverRequest.SetMaxSizeY(value);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "max size y");
				}
			}
		}

		private Color backgroundColor = Colors.Transparent;
		public Color BackgroundColor
		{
			get { return backgroundColor; }
			set
			{
				backgroundColor = value;
				adserverRequest.SetBackgroundColor(ColorToRgb(backgroundColor));
			}
		}

		private Color textColor = Colors.Transparent;
		public Color TextColor
		{
			get { return textColor; }
			set
			{
				textColor = value;
				adserverRequest.SetTextColor(ColorToRgb(textColor));
			}
		}

		private string customParameters = null;
		public string CustomParameters
		{
			get { return customParameters; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					customParameters = value;
					adserverRequest.SetCustomParameters(customParameters);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "custom parameters");
				}
			}
		}

		private string adServerUrl = DEFAULT_AD_SERVER;
		public string AdServerURL
		{
			get { return adServerUrl; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					adServerUrl = value;
					adserverRequest.SetAdserverURL(adServerUrl);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "adserver url");
				}
			}
		}

		public Image DefaultImage
		{
			get { return defaultImage; }
			set
			{
				if (value != null)
				{
					defaultImage = value;
					defaultImage.HorizontalAlignment = HorizontalAlignment.Center;
					defaultImage.VerticalAlignment = VerticalAlignment.Center;

					ImageBrush bgImage = new ImageBrush();
					bgImage.ImageSource = defaultImage.Source;
					LayoutRoot.Background = bgImage;
				}
				else
				{
					LayoutRoot.Background = null;
				}
			}
		}
		public bool InternalBrowser { get; set; }

		public int AdvertiserId
		{
			get { return advertiserId.GetValueOrDefault(-1); }
			set
			{
				if (value > 0)
				{
					advertiserId = value;
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "advertiser id");
				}
			}
		}
		public string GroupCode
		{
			get { return groupCode; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					groupCode = value;
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "group code");
				}
			}
		}
		public int UpdateTime
		{
			get { return updateTime; }
			set { updateTime = value; }
		}

		private string latitude = null;
		public string Latitude
		{
			get { return latitude; }
			set
			{
				latitude = value;
				adserverRequest.SetLatitude(latitude);
			}
		}

		private string longitude = null;
		public string Longitude
		{
			get { return longitude; }
			set
			{
				longitude = value;
				adserverRequest.SetLongitude(longitude);
			}
		}

		private string country = null;
		public string Country
		{
			get { return country; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					country = value;
					adserverRequest.SetCountry(country);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "country");
				}
			}
		}

		private string region = null;
		public string Region
		{
			get { return region; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					region = value;
					adserverRequest.SetRegion(region);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "region");
				}
			}
		}

		private string city = null;
		public string City
		{
			get { return city; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					city = value;
					adserverRequest.SetCity(city);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "city");
				}
			}
		}

		private string area = null;
		public string Area
		{
			get { return area; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					area = value;
					adserverRequest.SetArea(area);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "area");
				}
			}
		}

		private string metro = null;
		public string Metro
		{
			get { return metro; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					metro = value;
					adserverRequest.SetMetro(metro);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "metro");
				}
			}
		}

		private string zip = null;
		public string Zip
		{
			get { return zip; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					zip = value;
					adserverRequest.SetZip(zip);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "zip");
				}
			}
		}

		private string carrier = null;
		public string Carrier
		{
			get { return carrier; }
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					carrier = value;
					adserverRequest.SetCarrier(carrier);
				}
				else
				{
					WriteLog(Logger.LogLevel.ErrorsOnly, Logger.WRONG_PARAMETER, "carrier");
				}
			}
		}

		private bool track = false;
		public bool Track
		{
			get { return track; }
			set
			{
				track = value;
				adserverRequest.SetTrack(track);
			}
		}

		public Logger Logger
		{
			get { return logger; }
			set { logger = value;  }
		}
		public Logger.LogLevel LogLevel
		{
			get { return logLevel; }
			set { logLevel = value; }
		}
		#endregion

		public AdView()
		{
			viewId = adViewsCount++;
			adFileName = MOCEAN_SDK_DIR + "\\" + "adview" + viewId.ToString() + ".html";
			adExpandFileName = MOCEAN_SDK_DIR + "\\" + "adviewexpand" + viewId.ToString() + ".html";
			PlacementType = "inline";
			UrlToLoad = null;
			DataRequest.RootDir = MOCEAN_SDK_DIR;

			AutoDetectParameters.AddReferense();

			Version sdkVersion = (new AssemblyName(Assembly.GetExecutingAssembly().FullName)).Version;
			version = String.Format("{0}.{1}.{2}", sdkVersion.Major, sdkVersion.Minor, sdkVersion.Revision);

			deviceId = AutoDetectParameters.GetDeviceId();
			try
			{
				deviceId = MD5Core.GetHashString(deviceId);
			}
			catch (Exception)
			{
				deviceId = String.Empty;
			}

			AutoDetectParameters.Instance.LocationChanged += new AutoDetectParameters.LocationChangedEventHandler(GpsLocationChanged);
			AutoDetectParameters.Instance.CourseChanged += new AutoDetectParameters.CourseChangedEventHandler(GpsCourseChanged);
			AutoDetectParameters.Instance.NetworkChanged += new AutoDetectParameters.NetworkChangedEventHandler(NetworkChanged);

			InitializeComponent();

			SetUpdatingParameters();
			InitWebBrowser();
			InitExpandView();

			InitMap(map);
			
			mediaElement.Visibility = Visibility.Collapsed;
			mediaElement.MediaOpened += mediaElement_MediaOpened;
			mediaElement.MediaEnded += mediaElement_MediaEnded;
			mediaElement.MediaFailed += mediaElement_MediaFailed;
			mediaElement.Stretch = Stretch.Uniform;
		}

		public AdView(int site, int zone) : this()
		{
			Site = site;
			Zone = zone;
		}

		~AdView()
		{
			AutoDetectParameters.Release();
		}

		#region Registration
		private void RegisterFirstLaunch()
		{
			if (advertiserId.HasValue && !String.IsNullOrEmpty(groupCode))
			{
				bool isFirstLaunch = !(IsolatedStorageSettings.ApplicationSettings.Contains(SETTING_LAUNCHED));

				if (isFirstLaunch)
				{
					try
					{
						StringBuilder registerUrl = new StringBuilder(REG_LAUNCH_URL);
						registerUrl.AppendFormat("?udid={0}", Uri.EscapeUriString(deviceId));
						registerUrl.AppendFormat("&advertiser_id={0}", Uri.EscapeUriString(advertiserId.Value.ToString()));
						registerUrl.AppendFormat("&group_code={0}", Uri.EscapeUriString(groupCode));
						DataRequest.RegisterAd(this, registerUrl.ToString());
					}
					catch (Exception)
					{ }
				}
			}
		}

		void IDataRequestListener.OnRegisterResponse(string response)
		{
			if (response == REG_LAUNCH_RESPONSE)
			{
				SaveLaunchedSetting();
			}
		}

		private void SaveLaunchedSetting()
		{
			try
			{
				if (IsolatedStorageSettings.ApplicationSettings.Contains(SETTING_LAUNCHED))
				{
					IsolatedStorageSettings.ApplicationSettings[SETTING_LAUNCHED] = true;
				}
				else
				{
					IsolatedStorageSettings.ApplicationSettings.Add(SETTING_LAUNCHED, true);
				}
			}
			catch (Exception)
			{ }
		}
		#endregion

		#region Updating
		private int updateTime = DEFAULT_UPDATE_TIME;
		private Timer updateTimer = null;
		private volatile bool updatingAdContent = false;

		private void SetUpdatingParameters()
		{
			adserverRequest.SetAdserverURL(DEFAULT_AD_SERVER);
			adserverRequest.SetAdsType(3);
			adserverRequest.SetVersion(version);
			adserverRequest.SetSizeRequired(true);
			adserverRequest.SetDeviceId(deviceId);

			updateTimer = new Timer(new TimerCallback(UpdateTimerTick));
		}

		private void UpdateTimerTick(Object stateInfo)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => Update());
		}

		public void Update()
		{
			if (isClosed ||
				userAgent == null || deviceId == null)
			{
				return;
			}

			if (site > 0 && zone > 0)
			{
				string uri = adserverRequest.ToString();

				mediaElement.Stop();

				PauseUpdate();
				OnAdDownloadBegin();
				DataRequest.RequestAd(this, uri);
			}
			else
			{
				((IDataRequestListener)this).OnAdResponse(NO_SITE_ZONE_PARAMETERS_MESSAGE);
			}
		}

		private void ResumeUpdate()
		{
			if (updateTime == 0)
			{
				updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			else
			{
				updateTimer.Change(updateTime * 1000, 0);
			}
		}

		private void PauseUpdate()
		{
			updateTimer.Change(Timeout.Infinite, 0);
		}

		private void CancelLastRequest()
		{
			DataRequest.CancelRequestAd(this);
			//RequestCanceled = true;
		}

		void IDataRequestListener.OnAdResponse(string newAdContent)
		{
			if (newAdContent == INVALID_PARAMETERS_RESPONSE || String.IsNullOrEmpty(newAdContent))
			{
				OnAdDownloadError(newAdContent);
			}
			else
			{
				OnAdDownloadEnd();
			}

			if (newAdContent == adContent)
			{
				ResumeUpdate();
				return;
			}

			adContent = newAdContent;

			GenericThirdParty.GenericThirdPartyResponse thirdParty = GenericThirdParty.SearchThirdParty(newAdContent);
			if (thirdParty != null)
			{
				OnAdExternalCampaign(thirdParty);
			}

			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				string metaTags = "<meta name=\"viewport\" content=\"width=" + this.Width.ToString("F0") + ", height=" + this.Height.ToString("F0") + ", user-scalable=yes\"/>";
				string fullAdContent = "<html><head>" +
					metaTags +
					"<script type=\"text/javascript\">" + mOceanWindowsPhone.Resources.ormma + "</script>" +
					"</head><body style=\"margin:0; padding:0; width: 100%; height: 100%\">" +
					adContent +
					"</body></html>";

				bool saved = TrySaveFile(adFileName, fullAdContent);
				updatingAdContent = true;
				if (saved)
				{
					try
					{
						webBrowser.Navigate(new Uri(adFileName, UriKind.Relative));
					}
					catch (Exception)
					{ }
				}
				else
				{
					webBrowser.NavigateToString(fullAdContent);
				}
			});
		}

		void IDataRequestListener.OnAdVideoResponse(string newAdContent, string src, string href)
		{
			if (newAdContent == INVALID_PARAMETERS_RESPONSE || String.IsNullOrEmpty(newAdContent))
			{
				OnAdDownloadError(INVALID_PARAMETERS_MESSAGE);
			}
			else
			{
				OnAdDownloadEnd();
			}

			if (newAdContent == adContent)
			{
// 				ResumeUpdate();
// 				return;
			}

			adContent = newAdContent;
			mediaSource = src;
			mediaHref = href;
			mediaAutoplay = adContent.Contains("autoplay");
			Deployment.Current.Dispatcher.BeginInvoke(() => PlayMedia());
		}

		private bool TrySaveFile(string fileName, string ad)
		{
			bool saved = false;

			try
			{
				using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					if (appStorage.FileExists(fileName))
					{
						appStorage.DeleteFile(fileName);
					}

					using (IsolatedStorageFileStream file = appStorage.OpenFile(fileName, FileMode.Create))
					{
						using (StreamWriter writer = new StreamWriter(file))
						{
							writer.Write(ad);
							saved = true;
						}
					}
				}
			}
			catch (System.Exception)
			{ }

			return saved;
		}
		#endregion

		#region Showing Ad
		bool isClosed = false;
		private void CloseAdView()
		{
			ClosePopup();
			this.Visibility = Visibility.Collapsed;
			PauseUpdate();
			isClosed = true;
		}

		private Button closeButton = null;
		public Button CloseButton
		{
			get { return closeButton; }
			set
			{
				if (value != null)
				{
					imageClose.Visibility = Visibility.Collapsed;

					closeButton = value;
					closeButton.Click += closeButton_Click;
					LayoutRoot.Children.Add(closeButton);
				}
			}
		}

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			CloseAdView();
		}

		private Image defaultImage = null;

		private void InitWebBrowser()
		{
			webBrowser.Base = String.Empty;
			webBrowser.IsScriptEnabled = true;
			webBrowser.Loaded += webBrowser_Loaded;
			webBrowser.LoadCompleted += webBrowser_LoadCompleted;
			webBrowser.Navigating += webBrowser_Navigating;
			webBrowser.Navigated += webBrowser_Navigated;
			webBrowser.ScriptNotify += webBrowser_ScriptNotify;
			webBrowser.Visibility = Visibility.Collapsed;
		}

		private Point absolutePosition = new Point(0, 0);
		private bool appBarVisible = false;
		private bool systemTrayVisible = false;
		private Popup popup = new Popup();
		private Grid expandContainer = new Grid();
		private WebBrowser webBrowserExpanded = new WebBrowser();
		private string webBrowserExpandedUrl = null;
		private MediaElement mediaElementExpanded = new MediaElement();
		private Map mapExpanded = new Map();
		private Size screenSize = new Size(AdInterstitialView.initWidth, AdInterstitialView.initHeight);

		private Image imageCloseExpanded = new Image();

		private void InitExpandView()
		{
			popup.Width = AdInterstitialView.initWidth;
			popup.Height = AdInterstitialView.initHeight;
			popup.HorizontalAlignment = HorizontalAlignment.Left;
			popup.VerticalAlignment = VerticalAlignment.Top;
			popup.HorizontalOffset = 0;
			popup.VerticalOffset = 0;
			popup.Opened += new EventHandler(popup_Opened);

			popup.Child = expandContainer;
			LayoutRoot.Children.Add(popup);

			expandContainer.Width = AdInterstitialView.initWidth;
			expandContainer.Height = AdInterstitialView.initHeight;
			expandContainer.HorizontalAlignment = HorizontalAlignment.Left;
			expandContainer.VerticalAlignment = VerticalAlignment.Top;
			expandContainer.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

			webBrowserExpanded.HorizontalAlignment = HorizontalAlignment.Stretch;
			webBrowserExpanded.VerticalAlignment = VerticalAlignment.Stretch;
			webBrowserExpanded.Base = String.Empty;
			webBrowserExpanded.IsScriptEnabled = true;
			// 			webBrowserExpanded.Loaded += webBrowser_Loaded;
			webBrowserExpanded.LoadCompleted += webBrowser_LoadCompleted;
			// 			webBrowserExpanded.Navigating += webBrowser_Navigating;
			webBrowserExpanded.Navigated += webBrowser_Navigated;
			webBrowserExpanded.ScriptNotify += webBrowser_ScriptNotify;
			webBrowserExpanded.Visibility = Visibility.Collapsed;

			expandContainer.Children.Add(webBrowserExpanded);

			mediaElementExpanded.HorizontalAlignment = HorizontalAlignment.Stretch;
			mediaElementExpanded.VerticalAlignment = VerticalAlignment.Stretch;
			mediaElementExpanded.Visibility = Visibility.Collapsed;
			mediaElementExpanded.MediaOpened += mediaElement_MediaOpened;
			mediaElementExpanded.MediaEnded += mediaElement_MediaEnded;
			mediaElementExpanded.MediaFailed += mediaElement_MediaFailed;
			mediaElementExpanded.Stretch = Stretch.Uniform;

			expandContainer.Children.Add(mediaElementExpanded);

			InitMap(mapExpanded);
			expandContainer.Children.Add(mapExpanded);


			imageCloseExpanded.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("customclose.png", UriKind.Relative));
			imageCloseExpanded.Width = CUSTOM_CLOSE_BUTTON_SIZE;
			imageCloseExpanded.Height = CUSTOM_CLOSE_BUTTON_SIZE;
			imageCloseExpanded.HorizontalAlignment = HorizontalAlignment.Right;
			imageCloseExpanded.VerticalAlignment = VerticalAlignment.Top;
			imageCloseExpanded.Stretch = Stretch.Fill;
			imageCloseExpanded.Tap += imageClose_Tap;
			imageCloseExpanded.Visibility = Visibility.Collapsed;

			expandContainer.Children.Add(imageCloseExpanded);
		}

		private void popup_Opened(object sender, EventArgs e)
		{
			if (ownerPage != null)
			{
				ResizeExpandedView(ownerPage.Orientation);
			}
		}

		private void ResizeExpandedView(PageOrientation pageOrientation)
		{
			absolutePosition = this.TransformToVisual(Application.Current.RootVisual).Transform(new Point(0, 0));

			popup.HorizontalOffset = -absolutePosition.X;
			popup.VerticalOffset = -absolutePosition.Y;

			switch (pageOrientation)
			{
				case PageOrientation.Landscape:
				case PageOrientation.LandscapeLeft:
				case PageOrientation.LandscapeRight:
					expandContainer.Width = screenSize.Height;
					expandContainer.Height = screenSize.Width;
					break;
				default:
					expandContainer.Width = screenSize.Width;
					expandContainer.Height = screenSize.Height;
					break;
			}
		}

		private void OpenPopup()
		{
			try
			{
				if (ownerPage != null)
				{
					if (ownerPage.ApplicationBar != null)
					{
						appBarVisible = ownerPage.ApplicationBar.IsVisible;
						ownerPage.ApplicationBar.IsVisible = false;
					}

					systemTrayVisible = Microsoft.Phone.Shell.SystemTray.IsVisible;
					Microsoft.Phone.Shell.SystemTray.IsVisible = false;
				}

				popup.IsOpen = true;
			}
			catch (Exception)
			{ }
		}

		private void ClosePopup()
		{
			try
			{
				if (webBrowserExpanded.Visibility == Visibility.Visible && webBrowserExpandedUrl != null)
				{
					OnAdWebViewClosing(webBrowserExpandedUrl);
				}

				popup.IsOpen = false;
				webBrowserExpanded.Visibility = Visibility.Collapsed;
				mapExpanded.Visibility = Visibility.Collapsed;
				mediaElementExpanded.Visibility = Visibility.Collapsed;

				if (ownerPage != null)
				{
					if (ownerPage.ApplicationBar != null)
					{
						if (!ownerPage.ApplicationBar.IsVisible)
						{
							ownerPage.ApplicationBar.IsVisible = appBarVisible;
						}
					}

					if (!Microsoft.Phone.Shell.SystemTray.IsVisible)
					{
						Microsoft.Phone.Shell.SystemTray.IsVisible = systemTrayVisible;
					}
				}
			}
			catch (Exception)
			{ }
		}

		private void webBrowser_Loaded(object sender, RoutedEventArgs e)
		{
			PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
			if (frame != null)
			{
				screenSize = frame.RenderSize;

				ownerPage = frame.Content as PhoneApplicationPage;
				if (ownerPage != null)
				{
					ownerPage.BackKeyPress += new EventHandler<System.ComponentModel.CancelEventArgs>(ownerPage_BackKeyPress);
					ownerPage.OrientationChanged += new EventHandler<OrientationChangedEventArgs>(ownerPage_OrientationChanged);
					ownerPage.NavigationService.Navigating += new NavigatingCancelEventHandler(ownerPage_Navigating);
				}
			}

			if (userAgent == null)
			{
				webBrowser.NavigateToString("<html><head><script type=\"text/javascript\"></script></head><body></body></html>");
			}

			RegisterFirstLaunch();
		}

		private void ownerPage_Navigating(object sender, NavigatingCancelEventArgs e)
		{
		}

		private void webBrowser_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (sender.Equals(webBrowserExpanded))
			{
				OrmmaExpandReady();
				return;
			}

			if (userAgent == null)
			{
				try
				{
					userAgent = (string)webBrowser.InvokeScript("eval", "navigator.userAgent.toString()");
				}
				catch (Exception)
				{
					userAgent = String.Empty;
				}

				if (UrlToLoad != null)
				{
					updatingAdContent = true;
					webBrowser.Navigate(new Uri(UrlToLoad, UriKind.Absolute));
				}
				else
				{
					adserverRequest.SetUserAgent(userAgent);
					Update();
				}
			}
			else
			{
				if (webBrowser.Visibility == Visibility.Collapsed)
				{
					webBrowser.Visibility = Visibility.Visible;
					LayoutRoot.Background = null;
				}
				ResumeUpdate();
				OrmmaReady();
			}
		}

		private void webBrowser_Navigated(object sender, NavigationEventArgs e)
		{
			if (sender.Equals(webBrowserExpanded))
			{
				try
				{
					if (e.Uri.IsAbsoluteUri)
					{
						webBrowserExpandedUrl = e.Uri.AbsoluteUri;
					}
					else
					{
						webBrowserExpandedUrl = null;
					}
				}
				catch (Exception)
				{
					webBrowserExpandedUrl = null;
				}
			}
			else
			{
				updatingAdContent = false;
				string content = webBrowser.SaveToString();
			}
		}

		private void webBrowser_Navigating(object sender, NavigatingEventArgs e)
		{
			if (updatingAdContent)
			{
				OrmmaSetState(OrmmaState.Loading);
				return;
			}

			if (InternalBrowser)
			{
				OnAdNavigateBanner(e);
				if (!e.Cancel)
				{
					e.Cancel = true;
					PauseUpdate();
					CancelLastRequest();

					WriteLog(Logger.LogLevel.All, Logger.OPEN_INTERNAL_BROWSER);
					OpenUrlInternal(e.Uri.ToString());
				}
			}
			else
			{
				e.Cancel = true;

				PauseUpdate();
				CancelLastRequest();
				
				try
				{
					WebBrowserTask task = new WebBrowserTask();
					task.Uri = e.Uri;
					task.Show();
				}
				catch (Exception)
				{}
			}
		}

		private void webBrowser_ScriptNotify(object sender, NotifyEventArgs e)
		{
			OrmmaNotify(e.Value);
		}

		internal string UrlToLoad { get; set; }

		private string mediaSource = null;
		private string mediaHref = null;
		private bool mediaAutoplay = false;
		private void PlayMedia()
		{
			mediaElement.Visibility = Visibility.Visible;

			try
			{
				using (var appStorage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					using (var file = new IsolatedStorageFileStream(mediaSource, FileMode.Open, FileAccess.Read, appStorage))
					{
						mediaElement.SetSource(file);
						mediaElement.Play();
					}
				}
			}
			catch (Exception)
			{ }
		}

		private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
		{
			ResumeUpdate();
		}

		private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
		{
			if (mediaAutoplay)
			{
				mediaElement.Play();
			}
			else
			{
				ResumeUpdate();
			}
		}

		private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
		{
			mediaElement.Visibility = Visibility.Visible;
			LayoutRoot.Background = null;
		}

		private void OpenUrlInternal(string url)
		{
			OpenPopup();
			mediaElementExpanded.Visibility = Visibility.Collapsed;
			mapExpanded.Visibility = Visibility.Collapsed;
			webBrowserExpanded.Visibility = Visibility.Visible;
			webBrowserExpanded.Navigate(new Uri(url, UriKind.RelativeOrAbsolute));
		}
		#endregion

		#region Events
		public event EventHandler AdDownloadBegin = null;
		protected virtual void OnAdDownloadBegin()
		{
			EventHandler handler = AdDownloadBegin;
			if (handler != null)
			{
				try
				{
					handler(this, EventArgs.Empty);
				}
				catch (Exception)
				{ }
			}
		}

		public event EventHandler AdDownloadEnd = null;
		protected virtual void OnAdDownloadEnd()
		{
			EventHandler handler = AdDownloadEnd;
			if (handler != null)
			{
				try
				{
					handler(this, EventArgs.Empty);
				}
				catch (Exception)
				{ }
			}
		}

		public event EventHandler<NavigatingEventArgs> AdNavigateBanner = null;
		protected virtual void OnAdNavigateBanner(NavigatingEventArgs e)
		{
			EventHandler<NavigatingEventArgs> handler = AdNavigateBanner;
			if (handler != null)
			{
				try
				{
					handler(this, e);
				}
				catch (Exception)
				{ }
			}
		}

		public class DownloadErrorEventArgs : EventArgs
		{
			public string Error { get; private set; }
			public DownloadErrorEventArgs(string error) { Error = error; }
		}
		public event EventHandler<DownloadErrorEventArgs> AdDownloadError = null;
		protected virtual void OnAdDownloadError(string error)
		{
			WriteLog(Logger.LogLevel.ErrorsOnly, Logger.FAIL_AD_DOWNLOAD, error);

			EventHandler<DownloadErrorEventArgs> handler = AdDownloadError;
			if (handler != null)
			{
				try
				{
					handler(this, new DownloadErrorEventArgs(error));
				}
				catch (Exception)
				{ }
			}
		}

		public class WebViewClosingEventArgs : EventArgs
		{
			public string Url { get; private set; }
			public WebViewClosingEventArgs(string url) { Url = url; }
		}
		public event EventHandler<WebViewClosingEventArgs> AdWebViewClosing = null;
		protected virtual void OnAdWebViewClosing(string url)
		{
			EventHandler<WebViewClosingEventArgs> handler = AdWebViewClosing;
			if (handler != null)
			{
				try
				{
					handler(this, new WebViewClosingEventArgs(url));
				}
				catch (Exception)
				{ }
			}
		}

		public class ExternalCampaignEventArgs : EventArgs
		{
			public string CampaignId { get; internal set; }
			public string Type { get; internal set; }
			public string Variables { get; internal set; }
			public string TrackUrl { get; internal set; }
			public ExternalCampaignEventArgs() { }
		}
		public event EventHandler<ExternalCampaignEventArgs> AdExternalCampaign = null;
		private void OnAdExternalCampaign(GenericThirdParty.GenericThirdPartyResponse thirdParty)
		{
			EventHandler<ExternalCampaignEventArgs> handler = AdExternalCampaign;
			if (handler != null)
			{
				ExternalCampaignEventArgs e = new ExternalCampaignEventArgs();
				e.CampaignId = thirdParty.campaignId;
				e.Type = thirdParty.type;
				e.Variables = thirdParty.variables;
				e.TrackUrl = thirdParty.trackUrl;

				try
				{
					handler(this, e);
				}
				catch (Exception)
				{ }
			}
		}
		#endregion

		#region ORMMA
		private Size defaultSize = Size.Empty;
		private Thickness defaultMargin = new Thickness(0);
		private Panel parent = null;
		internal string PlacementType { get; set; }
		private AccelerometerController accelerometer = null;
		enum OrmmaState
		{
			Loading,
			Default,
			Resized,
			Expanded,
			Hidden
		}

		private volatile OrmmaState ormmaState = OrmmaState.Loading;

		private void OrmmaReady()
		{
			OrmmaSetState(OrmmaState.Default);

			parent = this.Parent as Panel;
			defaultMargin = this.Margin;
			defaultSize = this.RenderSize;

			if (accelerometer == null)
			{
				accelerometer = new AccelerometerController();
				accelerometer.TiltChange += new AccelerometerController.TiltChangeEventHandler(accelerometer_TiltChange);
				accelerometer.Shake += new EventHandler(accelerometer_Shake);
				accelerometer.StartListen();
			}

			StringBuilder script = new StringBuilder();
			if (parent != null)
			{
				script.AppendLine("maxSize = { width:" + parent.RenderSize.Width.ToString("F0") + ", height:" + parent.RenderSize.Height.ToString("F0") + "};");
			}

			if (!defaultSize.IsEmpty)
			{
				script.AppendLine("size = { width:" + defaultSize.Width.ToString("F0") + ", height:" + defaultSize.Height.ToString("F0") + "};");
				script.AppendLine("defaultPosition = {x: " + defaultMargin.Left.ToString("F0") + ", y: " + defaultMargin.Top.ToString("F0") + ", width: " + defaultSize.Width.ToString("F0") + ", height: " + defaultSize.Height.ToString("F0") + "};");
				script.AppendLine("placementType = \"" + PlacementType + "\";");
			}

			ExecScript(script.ToString());

			OrmmaSetOrientation(ownerPage.Orientation);
			OrmmaNetworkChanged(AutoDetectParameters.Instance.CurrentNetworkType);

			ExecScript("ORMMAReady(); ormma.raiseEvent(\"ready\");");
		}

		private void OrmmaExpandReady()
		{
			if (accelerometer == null)
			{
				accelerometer = new AccelerometerController();
				accelerometer.TiltChange += new AccelerometerController.TiltChangeEventHandler(accelerometer_TiltChange);
				accelerometer.Shake += new EventHandler(accelerometer_Shake);
				accelerometer.StartListen();
			}

			StringBuilder script = new StringBuilder();
			if (parent != null)
			{
				script.AppendLine("maxSize = { width:" + parent.RenderSize.Width.ToString("F0") + ", height:" + parent.RenderSize.Height.ToString("F0") + "};");
			}

			if (!defaultSize.IsEmpty)
			{
				script.AppendLine("size = { width:" + defaultSize.Width.ToString("F0") + ", height:" + defaultSize.Height.ToString("F0") + "};");
				script.AppendLine("defaultPosition = {x: " + defaultMargin.Left.ToString("F0") + ", y: " + defaultMargin.Top.ToString("F0") + ", width: " + defaultSize.Width.ToString("F0") + ", height: " + defaultSize.Height.ToString("F0") + "};");
				script.AppendLine("placementType = \"" + PlacementType + "\";");
				script.AppendLine("initState = \"expanded\";");
			}

			ExecScript(script.ToString());

			OrmmaSetOrientation(ownerPage.Orientation);
			OrmmaNetworkChanged(AutoDetectParameters.Instance.CurrentNetworkType);
		}

		private void accelerometer_TiltChange(double x, double y, double z)
		{
			try
			{
				string tilt = "{x:" + x.ToString("F2") + ", y:" + y.ToString("F2") + ", z:" + z.ToString("F2") + "}";

				StringBuilder script = new StringBuilder();
				script.AppendFormat("tilt = {0};", tilt);
				script.AppendFormat("Ormma.raiseEvent(\"tiltChange\", {0});", tilt);
				ExecScript(script.ToString());
			}
			catch (Exception)
			{}
		}

		private void accelerometer_Shake(object sender, EventArgs e)
		{
			ExecScript("Ormma.raiseEvent(\"shake\");");
		}

		private void OrmmaSetState(OrmmaState newOrmmaState)
		{
			if (ormmaState != OrmmaState.Expanded)
			{
				ormmaState = newOrmmaState;
			}
		}

		private void ExecScript(string script)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				try
				{
					if (ormmaState == OrmmaState.Expanded)
					{
						webBrowserExpanded.InvokeScript("execScript", script);
					}
					else
					{
						webBrowser.InvokeScript("execScript", script);
					}
				}
				catch (Exception)
				{ }
			});
		}

		private void OrmmaNotify(string notify)
		{
			OnAlert(notify);
			string[] notifyParts = notify.Split('|');
			if (notifyParts.Length > 0)
			{
				string method = notifyParts[0];

				if (method == "javascript_alert") OnAlert(notifyParts);
				else if (method == "useCustomClose" && notifyParts.Length == 2) OrmmaUseCustomClose(notifyParts[1]);
				else if (method == "hide") OrmmaHide();
				else if (method == "show") OrmmaShow();
				else if (method == "resize") OrmmaResize(notifyParts);
				else if (method == "close") OrmmaClose();
				else if (method == "expand") OrmmaExpand(notifyParts);
				else if (method == "open" && notifyParts.Length == 2) OpenUrlInternal(notifyParts[1]);
				else if (method == "storePicture" && notifyParts.Length == 2) OrmmaStorePicture(notifyParts[1]);
				else if (method == "openMap" && notifyParts.Length == 3) OrmmaOpenMap(notifyParts);
					
				else if (method == "request" && notifyParts.Length == 3) DataRequest.OrmmaRequest(this, notifyParts[1], notifyParts[2]);
				else if (method == "makeCall" && notifyParts.Length == 2)
				{
					NativeAppManager.MakeCall(notifyParts[1]);
				}
				else if (method == "sendMail" && notifyParts.Length == 4)
				{
					NativeAppManager.SendMail(notifyParts[1], notifyParts[2], notifyParts[3]);
				}
				else if (method == "sendSMS" && notifyParts.Length == 3)
				{
					NativeAppManager.SendSms(notifyParts[1], notifyParts[2]);
				}
				else if ((method == "playAudio" || method == "playVideo") && notifyParts.Length == 3)
				{
					NativeAppManager.PlayMedia(notifyParts[1], notifyParts[2]);
				}
			}
		}

		private void OrmmaHide()
		{
			this.Visibility = Visibility.Collapsed;
			OrmmaSetState(OrmmaState.Hidden);
		}

		private void OrmmaShow()
		{
			this.Visibility = Visibility.Visible;
			OrmmaSetState(OrmmaState.Default);
		}

		private void OrmmaResize(params string[] resizeParams)
		{
			if (resizeParams.Length >= 3)
			{
				try
				{
					double width = Double.Parse(resizeParams[1]);
					double height = Double.Parse(resizeParams[2]);

					this.Width = width;
					this.Height = height;

					OrmmaSetState(OrmmaState.Resized);
					//ExecScript("window.location.reload();");
				}
				catch (Exception)
				{ }
			}
		}

		private void OrmmaClose()
		{
			switch (ormmaState)
			{
				case OrmmaState.Default:
					OrmmaHide();
					break;
				case OrmmaState.Resized:
					try
					{
						this.Width = defaultSize.Width;
						this.Height = defaultSize.Height;
						OrmmaSetState(OrmmaState.Default);
					}
					catch (Exception)
					{ }
					break;
				case OrmmaState.Expanded:
					ClosePopup();
					try
					{
						this.Width = defaultSize.Width;
						this.Height = defaultSize.Height;
						ormmaState = OrmmaState.Default;
					}
					catch (Exception)
					{ }

					ExecScript("ormma.onExpandClosed();");
					break;
				default:
					break;
			}
		}

		[DataContract]
		internal class ExpandProperties
		{
			[DataMember]
			public int width = 0;
			[DataMember]
			public int height = 0;
			[DataMember]
			public bool useCustomClose = true;
			[DataMember]
			public bool isModal = true;
			[DataMember]
			public bool lockOrientation = false;
			[DataMember]
			public bool useBackground = false;
			[DataMember]
			public string backgroundColor = null;
			[DataMember]
			public double backgroundOpacity = 1;

			public ExpandProperties()
			{ }
		}
		private void OrmmaExpand(params string[] expandParams)
		{
			if (expandParams.Length == 3)
			{
				ExpandProperties properties = null;
				try
				{
					using (var memoryStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(expandParams[1])))
					{
						var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ExpandProperties));
						properties = serializer.ReadObject(memoryStream) as ExpandProperties;
					}
				}
				catch (Exception)
				{
					properties = null;
				}

				if (properties != null)
				{
					SetUseCustomClose(properties.useCustomClose);

					OrmmaSetState(OrmmaState.Expanded);

					string url = expandParams[2];
					if (String.IsNullOrEmpty(url) || url.Equals("null", StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							string currentAdContent = webBrowser.SaveToString();
							currentAdContent = currentAdContent.Replace("initState = \"loading\"", "initState = \"expanded\"");
							if (TrySaveFile(adExpandFileName, currentAdContent))
							{
								OpenUrlInternal(adExpandFileName);
							}
						}
						catch (Exception)
						{ }
					}
					else
					{
						OpenUrlInternal(expandParams[2]);
					}
				}
			}
		}

		private void OrmmaStorePicture(string url)
		{
			DataRequest.StoreMediaFile(url);
		}

		void IDataRequestListener.OnOrmmaResponse(string url, string response)
		{
			try
			{
				response = response.Replace("\"", "\\\"").Replace("\'", "\\\'").Replace("\n", "\\n");
				string script = String.Format("ormma.raiseEvent(\"response\", \"{0}\", \"{1}\");", url, response);
				ExecScript(script);
			}
			catch (Exception)
			{}
		}

		private void OrmmaUseCustomClose(string use)
		{
			bool useCustomClose = false;
			Boolean.TryParse(use, out useCustomClose);
			SetUseCustomClose(useCustomClose);
		}

		private void SetUseCustomClose(bool use)
		{
			if (use)
			{
				imageCloseExpanded.Visibility = Visibility.Visible;
			}
			else
			{
				imageCloseExpanded.Visibility = Visibility.Collapsed;
			}
		}

		private void imageClose_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			if (sender.Equals(imageClose))
			{
				CloseAdView();
			}

			if (ormmaState == OrmmaState.Expanded)
			{
				OrmmaClose();
			}
		}

		#region Map
		private const string KEY = "AogiRkuyqKxbvxlcu99SUeRp5M9V3j4hije0x4onsbMqkoOEE6p4QNPkA6mjqvxD";
		private const double DEFAULT_ZOOM_LEVEL = 0;
		private const double DEFAULT_LATITUDE = 56.632778;
		private const double DEFAULT_LONGITUDE = 47.895833;
		private Color PUSHPIN_COLOR = Colors.LightGray;
		private Color PUSHPIN_TEXT_COLOR = Colors.Black;
		private const int PUSHPIN_TEXT_SIZE = 30;

		private const string MAP_TYPE_PARAM_NAME = "t";
		private const string MAP_TYPE_MAP = "m";
		private const string MAP_TYPE_SATELLITE = "k";
		private const string MAP_TYPE_HYBRID = "h";
		private const string MAP_TYPE_TERRAIN = "p";
		private const string MAP_TYPE_GOOGLE_EARTH = "e";

		private const string QUERY_PARAM_NAME = "q";
		private const string CENTER_PARAM_NAME = "ll";
		private const string ZOOM_LEVEL_PARAM_NAME = "z";

		private const string PROOF = "http://maps.google.ru/maps/ms?gl=ru&ie=UTF8&msa=0&msid=213579429772951119527.0004a3c5ac3165f7db1ea&ll=56.674338,47.891464&spn=0.19013,0.623474&t=h&z=11&iwloc=0004a3c5ac333eb8e4989";

		private void InitMap(Map map)
		{
			map.Visibility = Visibility.Collapsed;
			map.LoadingError += new EventHandler<Microsoft.Phone.Controls.Maps.LoadingErrorEventArgs>(map_LoadingError);
			map.CredentialsProvider = new ApplicationIdCredentialsProvider(KEY);
			map.CopyrightVisibility = Visibility.Collapsed;
			map.LogoVisibility = Visibility.Collapsed;
			map.ZoomBarVisibility = Visibility.Visible;
			map.Center = new GeoCoordinate(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
			map.ZoomLevel = DEFAULT_ZOOM_LEVEL;
		}

		private void OrmmaOpenMap(params string[] mapParams)
		{
			if (mapParams.Length == 3)
			{
				string poi = mapParams[1];
				bool fullScreen = false;
				Boolean.TryParse(mapParams[2], out fullScreen);

				poi = PROOF;

				if (fullScreen)
				{
					OpenPopup();
					mediaElementExpanded.Visibility = Visibility.Collapsed;
					webBrowserExpanded.Visibility = Visibility.Collapsed;
					mapExpanded.Visibility = Visibility.Visible;
					OpenPoi(mapExpanded, poi);
				}
				else
				{
					webBrowser.Visibility = Visibility.Collapsed;
					mediaElement.Visibility = Visibility.Collapsed;
					map.Visibility = Visibility.Visible;
					OpenPoi(map, poi);
				}
			}
		}

		private void OpenPoi(Map map, string poiUrl)
		{
			Dictionary<string, string> poiParams = SplitPoiParams(poiUrl);

			if (poiParams != null)
			{
				map.Children.Clear();

				Pushpin pushpin = new Pushpin();
				pushpin.Background = new SolidColorBrush(PUSHPIN_COLOR);

				foreach (string paramName in poiParams.Keys)
				{
					string paramValue = poiParams[paramName];

					if (paramName == MAP_TYPE_PARAM_NAME)
					{
						switch (paramValue)
						{
							case MAP_TYPE_SATELLITE:
							case MAP_TYPE_TERRAIN:
							case MAP_TYPE_GOOGLE_EARTH:
								map.Mode = new Microsoft.Phone.Controls.Maps.AerialMode();
								break;
							case MAP_TYPE_MAP:
							case MAP_TYPE_HYBRID:
								map.Mode = new Microsoft.Phone.Controls.Maps.RoadMode();
								break;
							default:
								break;
						}
					}
					else if (paramName == CENTER_PARAM_NAME)
					{
						Point center = ParseLocation(paramValue);
						map.Center = new GeoCoordinate(center.X, center.Y);
						pushpin.Location = map.Center;
					}
					else if (paramName == ZOOM_LEVEL_PARAM_NAME)
					{
						try
						{
							map.ZoomLevel = Convert.ToDouble(paramValue);
						}
						catch (System.Exception)
						{ }
					}
					else if (paramName == QUERY_PARAM_NAME)
					{
						TextBlock textBlock = new TextBlock();
						textBlock.Text = paramValue;
						pushpin.Content = textBlock;
						pushpin.FontSize = PUSHPIN_TEXT_SIZE;
						pushpin.Foreground = new SolidColorBrush(PUSHPIN_TEXT_COLOR);
					}
				}

				map.Children.Add(pushpin);
			}
		}

		private Dictionary<string, string> SplitPoiParams(string poiUrl)
		{
			Dictionary<string, string> result = null;

			try
			{
				poiUrl = System.Net.HttpUtility.UrlDecode(poiUrl);

				poiUrl = poiUrl.Replace("http://maps.google.com/maps?", String.Empty).Trim();
				string[] parameters = poiUrl.Split('&');

				if (parameters != null)
				{
					result = new Dictionary<string, string>();
					for (int i = 0; i < parameters.Length; i++)
					{
						string[] nameValue = parameters[i].Split('=');
						if (nameValue.Length == 2)
						{
							result.Add(nameValue[0], nameValue[1]);
						}
					}
				}
			}
			catch (System.Exception)
			{}

			return result;
		}

		private Point ParseLocation(string locationString)
		{
			Point location = new Point(0, 0);
			try
			{
				string[] coords = locationString.Split(',');
				location.X = Convert.ToDouble(coords[0]);
				location.Y = Convert.ToDouble(coords[1]);
			}
			catch (System.Exception)
			{
				location.X = location.Y = 0;
			}

			return location;
		}

		private void map_LoadingError(object sender, LoadingErrorEventArgs e)
		{
			ClosePopup();
		}

		#endregion

		private void OrmmaSetHeading(int heading)
		{
			try
			{
				StringBuilder script = new StringBuilder();
				script.AppendFormat("heading = {0};", heading);
				script.AppendFormat("Ormma.raiseEvent(\"headingChange\", {0});", heading);
				ExecScript(script.ToString());
			}
			catch (Exception)
			{ }
		}

		private void OrmmaSetLocation(double latitude, double longitude, double accuracy)
		{
			try
			{
				string location = "{lat:" + latitude.ToString("F1") + ", lon:" + longitude.ToString("F1") + ", acc:" + accuracy.ToString("F1") + "}";

				StringBuilder script = new StringBuilder();
				script.AppendFormat("geoLocation = {0};", location);
				script.AppendFormat("Ormma.raiseEvent(\"locationChange\", {0});", location);
				ExecScript(script.ToString());
			}
			catch (Exception)
			{ }
		}

		private void OrmmaNetworkChanged(NetworkInterfaceType networkType)
		{
			try
			{
				string netWork = "offline";

				switch (networkType)
				{
					case NetworkInterfaceType.Wireless80211:
						netWork = "wifi";
						break;
					case NetworkInterfaceType.MobileBroadbandGsm:
					case NetworkInterfaceType.MobileBroadbandCdma:
						netWork = "cell";
						break;
					case NetworkInterfaceType.Unknown:
					default:
						netWork = "unknown";
						break;
				}

				StringBuilder script = new StringBuilder();
				script.AppendFormat("network = \"{0}\";", netWork);
				script.AppendFormat("Ormma.raiseEvent(\"networkChange\", {0});", "{online: " + (netWork == "wifi" || netWork == "cell").ToString().ToLower() + ", connection: \"" + netWork + "\"}");
				ExecScript(script.ToString());
			}
			catch (Exception)
			{ }
		}

		private void OrmmaSetOrientation(PageOrientation orientation)
		{
			int degrees = -1;
			switch (orientation)
			{
				case PageOrientation.Portrait:
				case PageOrientation.PortraitUp:
					degrees = 0;
					break;
				case PageOrientation.PortraitDown:
					degrees = 180;
					break;
				case PageOrientation.Landscape:
				case PageOrientation.LandscapeLeft:
					degrees = 270;
					break;
				case PageOrientation.LandscapeRight:
					degrees = 90;
					break;
				default:
					break;
			}

			double width = Double.NaN;
			double height = Double.NaN;

			switch (orientation)
			{
				case PageOrientation.Portrait:
				case PageOrientation.PortraitUp:
				case PageOrientation.PortraitDown:
					width = Application.Current.RootVisual.RenderSize.Width;
					height = Application.Current.RootVisual.RenderSize.Height;
					break;
				case PageOrientation.Landscape:
				case PageOrientation.LandscapeLeft:
				case PageOrientation.LandscapeRight:
					width = Application.Current.RootVisual.RenderSize.Height;
					height = Application.Current.RootVisual.RenderSize.Width;
					break;
				default:
					break;
			}

			try
			{
				StringBuilder script = new StringBuilder();


				string screenSizeSetter = String.Format("width:{0},height:{1};", width.ToString("F0"), height.ToString("F0"));
				script.AppendFormat("screenSize = {0};", "{width: " + width.ToString("F0") + ", height:" + height.ToString("F0") + "}");

				script.AppendFormat("orientation = {0};", degrees);
				script.AppendFormat("Ormma.raiseEvent(\"orientationChange\", {0});", degrees);
				if (!Double.IsNaN(width) && !Double.IsNaN(height))
				{
					script.AppendFormat("Ormma.raiseEvent(\"screenChange\", {0});", "{width: " + width.ToString("F0") + ", height:" + height.ToString("F0") + "}");
				}
				ExecScript(script.ToString());
			}
			catch (Exception)
			{ }
		}
		#endregion

		private void GpsLocationChanged(double latitude, double longitude, double accuracy)
		{
			WriteLog(Logger.LogLevel.All, "AutoDetectParameters", Logger.GPS_COORDINATES_DETECTED, latitude.ToString("F1") + "," + longitude.ToString("F1"));
			OrmmaSetLocation(latitude, longitude, accuracy);
		}

		private void GpsCourseChanged(int course)
		{
			OrmmaSetHeading(course);
		}

		private void NetworkChanged(NetworkInterfaceType networkType)
		{
			OrmmaNetworkChanged(networkType);
		}

		private void ownerPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
		{
			if (e != null)
			{
				ResizeExpandedView(e.Orientation);

				OrmmaSetOrientation(e.Orientation);
			}
		}

		internal void ownerPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				if (ormmaState == OrmmaState.Expanded)
				{
					e.Cancel = true;
					OrmmaClose();
				}
				else
				{
					if (popup.IsOpen)
					{
						e.Cancel = true;
						ClosePopup();
					}
				}
			}
		}

		private void WriteLog(Logger.LogLevel logLevel, string message, string parameter = null)
		{
			WriteLog(logLevel, "AdView", message, parameter);
		}

		private void WriteLog(Logger.LogLevel logLevel, string className, string message, string parameter = null)
		{
#if DEBUG
			if (logger != null)
			{
				if (this.logLevel >= logLevel)
				{
					logger.WriteLine(logLevel, className, viewId, message, parameter);
				}
			}
#endif
		}

		internal static Color ColorFromRgb(string rgb)
		{
			if (rgb.Length >= 7)
			{
				try
				{
					byte r = byte.Parse(rgb.Substring(1, 2));
					byte g = byte.Parse(rgb.Substring(3, 2));
					byte b = byte.Parse(rgb.Substring(5, 2));
					return Color.FromArgb(255, r, g, b);
				}
				catch (Exception)
				{ }
			}
			return Color.FromArgb(0, 0, 0, 0);
		}

		internal static string ColorToRgb(Color color)
		{
			return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
		}

		private void OnAlert(params string[] alertParams)
		{
// 			if (alertParams.Length <= 1)
// 			{
// 				if (!alertParams[0].Contains("tilt"))
// 				{
// 					MessageBox.Show(alertParams[0]);
// 				}
// 			}
// 			else
// 			{
// 				MessageBox.Show(alertParams[1], alertParams[0], MessageBoxButton.OK);
// 			}
		}
	}
}
