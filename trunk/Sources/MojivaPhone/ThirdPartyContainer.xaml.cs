﻿/****
 * © 2010-2011 mOcean Mobile. A subsidiary of Mojiva, Inc. All Rights Reserved.
 * */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using mmiWP7SDK;

namespace MojivaPhone
{
	public partial class ThirdPartyContainer : UserControl
	{
		public ThirdPartyContainer()
		{
			InitializeComponent();
		}

		private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
		{
			Size renderSize = Application.Current.RootVisual.RenderSize;
 			this.Width = renderSize.Width;
 			this.Height = renderSize.Height;
			LayoutRoot.Width = renderSize.Width;
			LayoutRoot.Height = renderSize.Height;
		}

		public void Run(string pageContent)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => this.Visibility = Visibility.Visible);

			CSLThirdPartyManager.Instance.SetMMAdView(mMAdView);
			CSLThirdPartyManager.Instance.Run(pageContent);
		}

		public void Hide()
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => this.Visibility = Visibility.Collapsed);
		}

		private class CSLThirdPartyManager : CThirdPartyManager
		{
			private CSLThirdPartyManager()
			{}

			public static new CSLThirdPartyManager Instance
			{
				get
				{
					lock (padlock)
					{
						if (instance == null)
						{
							instance = new CSLThirdPartyManager();
						}
						return (CSLThirdPartyManager)instance;
					}
				}
			}

			public void SetMMAdView(MMAdView mmAdView)
			{
				Instance.millennialAdView = new CSLMillennialAdView(mmAdView);
				Instance.millennialAdView.AdViewSuccess += new EventHandler(MillennialAdViewSuccess);
				Instance.millennialAdView.AdViewFailure += new EventHandler(MillennialAdViewFailure);
			}
		}

		private class CSLMillennialAdView : CMillennialAdView
		{
			private MMAdView adView = null;

			public CSLMillennialAdView(MMAdView mmAdView)
			{
				adView = mmAdView;

				if (adView != null)
				{
					adView.MMAdSuccess += new EventHandler<EventArgs>(AdView_MMAdSuccess);
					adView.MMAdFailure += new EventHandler<EventArgs>(AdView_MMAdFailure);
				}
			}

			private void AdView_MMAdSuccess(object sender, EventArgs e)
			{
				OnAdViewSuccess();
			}

			private void AdView_MMAdFailure(object sender, EventArgs e)
			{
				OnAdViewFailure();
			}

			protected override bool IsInitParams(Dictionary<string, string> parameters)
			{
				if (parameters == null || parameters.Count == 0)
				{
					return false;
				}

				foreach (string param in parameters.Keys)
				{
					string paramName = String.Empty;
					string paramValue = parameters[param];

					switch (param)
					{
						case "id":
							paramName = "Apid";
							break;
						case "long":
							paramName = "Longitude";
							break;
						case "lat":
							paramName = "Latitude";
							break;
						default:
							paramName = param;
							break;
					}

					PropertyInfo property = typeof(MMAdView).GetProperty(paramName, BindingFlags.Public |
																					BindingFlags.Instance |
																					BindingFlags.IgnoreCase);

					if (property != null)
					{
						System.Diagnostics.Debug.WriteLine("finded property " + property.Name);

						try
						{
							if (property.PropertyType.BaseType == typeof(Enum))
							{
								property.SetValue(adView, (Enum.Parse(property.PropertyType, paramValue, true)), null);
							}
							else if (property.PropertyType == typeof(int))
							{
								property.SetValue(adView, Int32.Parse(paramValue), null);
							}
							else
							{
								property.SetValue(adView, paramValue, null);
							}
						}
						catch (System.Exception /*ex*/)
						{ }
					}
				}

				return true;
			}

			protected override void RunAd()
			{
				if (adView.AdType == MMAdView.MMAdType.MMFullScreenAdLaunch ||
					adView.AdType == MMAdView.MMAdType.MMFullScreenAdTransition)
				{
					adView.Width = 0;
					adView.Height = 0;

					adView.AdWidth = "0";
					adView.AdHeight = "0";
				}
				adView.CallForAd();
			}
		}
	}
}
