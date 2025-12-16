using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevToi.ThirdLib.Core;

#if ADMOB
using GoogleMobileAds.Api;
#endif

namespace GameDevToi.ThirdLib.AdModule
{
#if ADMOB
    /// <summary>
    /// Module cho Google AdMob với full implementation
    /// </summary>
    public class AdMobModule : BaseAdNetworkModule
    {
        public override string NetworkId => "admob";

        // Dictionary to store loaded ads by format ID
        private Dictionary<string, BannerView> bannerAds = new Dictionary<string, BannerView>();
        private Dictionary<string, BannerView> mrecAds = new Dictionary<string, BannerView>();
        private Dictionary<string, InterstitialAd> interstitialAds = new Dictionary<string, InterstitialAd>();
        private Dictionary<string, RewardedAd> rewardedAds = new Dictionary<string, RewardedAd>();
        private Dictionary<string, AppOpenAd> appOpenAds = new Dictionary<string, AppOpenAd>();
        private Dictionary<string, RewardedInterstitialAd> rewardedInterstitialAds = new Dictionary<string, RewardedInterstitialAd>();

        // Track which ads are currently loaded
        private HashSet<string> loadedAds = new HashSet<string>();

        public override void Initialize(ThirdLibConfig config)
        {
            base.Initialize(config);

            string appId = config.GetGoogleAdMobAppId();

            if (string.IsNullOrEmpty(appId) || appId.Contains("xxxxxxxx"))
            {
                LogWarning("AdMob App ID not configured properly");
                return;
            }

            LogInfo($"AdMob initializing with App ID: {appId}");

            MobileAds.Initialize(initStatus =>
            {
                isInitialized = true;
                LogInfo("AdMob initialized successfully");

                // Log adapter status
                var adapterStatus = initStatus.getAdapterStatusMap();
                foreach (var adapter in adapterStatus)
                {
                    Debug.Log($"AdMob Adapter: {adapter.Key} - {adapter.Value.InitializationState}");
                }
            });
        }

        public override void LoadAdUnit(AdUnit adUnit)
        {
            if (!isInitialized)
            {
                LogError("AdMob not initialized. Cannot load ad unit.");
                return;
            }

            string adUnitId = adUnit.GetAdUnitId();
            var formatDef = adUnit.GetFormat();
            string formatName = formatDef?.displayName ?? adUnit.formatId;
            LogInfo($"Loading {formatName} ad with ID: {adUnitId}");


            string formatId = adUnit.formatId;
            if (formatId == "banner") LoadBanner(adUnitId);
            else if (formatId == "mrec") LoadMrec(adUnitId);
            else if (formatId == "interstitial") LoadInterstitial(adUnitId);
            else if (formatId == "rewarded") LoadRewarded(adUnitId);
            else if (formatId == "appopen") LoadAppOpen(adUnitId);
            else if (formatId == "rewarded_interstitial") LoadRewardedInterstitial(adUnitId);
            else if (formatId == "native") LoadNative(adUnitId);
            else LogWarning($"Unknown ad format: {formatId}");
        }

        public override void ShowAd(string formatId, string placementId = null)
        {
            if (!isInitialized)
            {
                LogError("AdMob not initialized. Cannot show ad.");
                return;
            }

            var formatDef = AdRegistry.GetFormat(formatId);
            string formatName = formatDef?.displayName ?? formatId;

            if (!IsAdReady(formatId, placementId))
            {
                LogWarning($"{formatName} ad is not ready to show");
                return;
            }

            LogInfo($"Showing {formatName} ad");

            try
            {
                if (formatId == "banner")
                {
                    ShowBanner();
                }
                else if (formatId == "mrec")
                {
                    ShowMrec();
                }
                else if (formatId == "interstitial")
                {
                    if (interstitialAds.ContainsKey(formatId) && interstitialAds[formatId].CanShowAd())
                    {
                        interstitialAds[formatId].Show();
                    }
                }
                else if (formatId == "rewarded")
                {
                    if (rewardedAds.ContainsKey(formatId) && rewardedAds[formatId].CanShowAd())
                    {
                        var adUnit = config.GetActiveAdUnits("rewarded").FirstOrDefault();
                        string adUnitId = adUnit?.GetAdUnitId() ?? "unknown";

                        rewardedAds[formatId].Show((Reward reward) =>
                        {
                            LogInfo($"Rewarded ad granted reward: {reward.Type} - {reward.Amount}");
                            AdEvents.TriggerRewarded("rewarded", NetworkId, adUnitId, reward.Type, reward.Amount);
                        });
                    }
                }
                else if (formatId == "appopen")
                {
                    if (appOpenAds.ContainsKey(formatId) && appOpenAds[formatId].CanShowAd())
                    {
                        appOpenAds[formatId].Show();
                    }
                }
                else if (formatId == "rewarded_interstitial")
                {
                    if (rewardedInterstitialAds.ContainsKey(formatId) && rewardedInterstitialAds[formatId].CanShowAd())
                    {
                        var adUnit = config.GetActiveAdUnits("rewarded_interstitial").FirstOrDefault();
                        string adUnitId = adUnit?.GetAdUnitId() ?? "unknown";

                        rewardedInterstitialAds[formatId].Show((Reward reward) =>
                        {
                            LogInfo($"Rewarded interstitial granted reward: {reward.Type} - {reward.Amount}");
                            AdEvents.TriggerRewarded("rewarded_interstitial", NetworkId, adUnitId, reward.Type, reward.Amount);
                        });
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error showing {formatName} ad: {e.Message}");
            }
        }

        public override bool IsAdReady(string formatId, string placementId = null)
        {
            if (!isInitialized) return false;

            try
            {
                if (formatId == "banner")
                {
                    return bannerAds.ContainsKey(formatId);
                }
                else if (formatId == "mrec")
                {
                    return mrecAds.ContainsKey(formatId);
                }
                else if (formatId == "interstitial")
                {
                    return interstitialAds.ContainsKey(formatId) && interstitialAds[formatId].CanShowAd();
                }
                else if (formatId == "rewarded")
                {
                    return rewardedAds.ContainsKey(formatId) && rewardedAds[formatId].CanShowAd();
                }
                else if (formatId == "appopen")
                {
                    return appOpenAds.ContainsKey(formatId) && appOpenAds[formatId].CanShowAd();
                }
                else if (formatId == "rewarded_interstitial")
                {
                    return rewardedInterstitialAds.ContainsKey(formatId) && rewardedInterstitialAds[formatId].CanShowAd();
                }
            }
            catch (Exception e)
            {
                LogError($"Error checking ad ready status: {e.Message}");
            }

            return false;
        }

        public override void DestroyAd(string formatId)
        {
            try
            {
                if (formatId == "banner" && bannerAds.ContainsKey(formatId))
                {
                    bannerAds[formatId].Destroy();
                    bannerAds.Remove(formatId);
                    LogInfo("Banner ad destroyed");
                }
                else if (formatId == "mrec" && mrecAds.ContainsKey(formatId))
                {
                    mrecAds[formatId].Destroy();
                    mrecAds.Remove(formatId);
                    LogInfo("MREC ad destroyed");
                }
                else if (formatId == "interstitial" && interstitialAds.ContainsKey(formatId))
                {
                    interstitialAds[formatId].Destroy();
                    interstitialAds.Remove(formatId);
                    LogInfo("Interstitial ad destroyed");
                }
                else if (formatId == "rewarded" && rewardedAds.ContainsKey(formatId))
                {
                    rewardedAds[formatId].Destroy();
                    rewardedAds.Remove(formatId);
                    LogInfo("Rewarded ad destroyed");
                }
                else if (formatId == "appopen" && appOpenAds.ContainsKey(formatId))
                {
                    appOpenAds[formatId].Destroy();
                    appOpenAds.Remove(formatId);
                    LogInfo("App Open ad destroyed");
                }
                else if (formatId == "rewarded_interstitial" && rewardedInterstitialAds.ContainsKey(formatId))
                {
                    rewardedInterstitialAds[formatId].Destroy();
                    rewardedInterstitialAds.Remove(formatId);
                    LogInfo("Rewarded Interstitial ad destroyed");
                }

                loadedAds.Remove(formatId);
            }
            catch (Exception e)
            {
                LogError($"Error destroying ad: {e.Message}");
            }
        }

        #region Banner Ad Implementation
        private void LoadBanner(string adUnitId)
        {
            LogInfo($"Loading Banner: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "banner", NetworkId, adUnitId);

            // Destroy existing banner if any
            if (bannerAds.ContainsKey("banner"))
            {
                bannerAds["banner"].Destroy();
            }

            // Create banner ad
            BannerView bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

            // Register event handlers
            bannerView.OnBannerAdLoaded += () =>
            {
                LogInfo("Banner ad loaded successfully");
                loadedAds.Add("banner");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "banner", NetworkId, adUnitId);
            };

            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                LogError($"Banner ad failed to load: {error.GetMessage()}");
                loadedAds.Remove("banner");
                AdEvents.TriggerFailed("banner", NetworkId, adUnitId, error.GetMessage(), error.GetCode());
            };

            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                LogInfo($"Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
                AdEvents.TriggerPaid("banner", NetworkId, adUnitId,
                    (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
            };

            bannerView.OnAdClicked += () =>
            {
                LogInfo("Banner ad clicked");
                AdEvents.Trigger(AdEventType.AdClicked, "banner", NetworkId, adUnitId);
            };

            bannerView.OnAdImpressionRecorded += () =>
            {
                LogInfo("Banner ad impression recorded");
                AdEvents.Trigger(AdEventType.AdImpression, "banner", NetworkId, adUnitId);
            };

            // Store reference
            bannerAds["banner"] = bannerView;

            // Load the banner ad
            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);
        }
        #endregion

        #region MREC Ad Implementation
        private void LoadMrec(string adUnitId)
        {
            LogInfo($"Loading MREC: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "mrec", NetworkId, adUnitId);

            // Destroy existing mrec if any
            if (mrecAds.ContainsKey("mrec"))
            {
                mrecAds["mrec"].Destroy();
            }

            // Create MREC ad (300x250) - positioned at bottom center by default
            BannerView mrecView = new BannerView(adUnitId, AdSize.MediumRectangle, AdPosition.BottomRight);

            // Register event handlers
            mrecView.OnBannerAdLoaded += () =>
            {
                LogInfo("MREC ad loaded successfully");
                loadedAds.Add("mrec");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "mrec", NetworkId, adUnitId);
            };

            mrecView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                LogError($"MREC ad failed to load: {error.GetMessage()}");
                loadedAds.Remove("mrec");
                AdEvents.TriggerFailed("mrec", NetworkId, adUnitId, error.GetMessage(), error.GetCode());
            };

            mrecView.OnAdPaid += (AdValue adValue) =>
            {
                LogInfo($"MREC ad paid: {adValue.Value} {adValue.CurrencyCode}");
                AdEvents.TriggerPaid("mrec", NetworkId, adUnitId,
                    (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
            };

            mrecView.OnAdClicked += () =>
            {
                LogInfo("MREC ad clicked");
                AdEvents.Trigger(AdEventType.AdClicked, "mrec", NetworkId, adUnitId);
            };

            mrecView.OnAdImpressionRecorded += () =>
            {
                LogInfo("MREC ad impression recorded");
                AdEvents.Trigger(AdEventType.AdImpression, "mrec", NetworkId, adUnitId);
            };

            // Store reference
            mrecAds["mrec"] = mrecView;

            // Load the MREC ad
            AdRequest request = new AdRequest();
            mrecView.LoadAd(request);
        }
        #endregion

        #region Interstitial Ad Implementation
        private void LoadInterstitial(string adUnitId)
        {
            LogInfo($"Loading Interstitial: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "interstitial", NetworkId, adUnitId);

            // Clean up old ad if exists
            if (interstitialAds.ContainsKey("interstitial"))
            {
                interstitialAds["interstitial"].Destroy();
                interstitialAds.Remove("interstitial");
            }

            AdRequest request = new AdRequest();

            InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    LogError($"Interstitial ad failed to load: {error?.GetMessage()}");
                    loadedAds.Remove("interstitial");
                    AdEvents.TriggerFailed("interstitial", NetworkId, adUnitId, error?.GetMessage(), error?.GetCode() ?? 0);
                    return;
                }

                LogInfo("Interstitial ad loaded successfully");
                interstitialAds["interstitial"] = ad;
                loadedAds.Add("interstitial");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "interstitial", NetworkId, adUnitId);

                // Register event handlers
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    LogInfo($"Interstitial ad paid: {adValue.Value} {adValue.CurrencyCode}");
                    AdEvents.TriggerPaid("interstitial", NetworkId, adUnitId,
                        (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    LogInfo("Interstitial ad impression recorded");
                    AdEvents.Trigger(AdEventType.AdImpression, "interstitial", NetworkId, adUnitId);
                };

                ad.OnAdClicked += () =>
                {
                    LogInfo("Interstitial ad clicked");
                    AdEvents.Trigger(AdEventType.AdClicked, "interstitial", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    LogInfo("Interstitial ad opened");
                    AdEvents.Trigger(AdEventType.AdShown, "interstitial", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    LogInfo("Interstitial ad closed");
                    AdEvents.Trigger(AdEventType.AdClosed, "interstitial", NetworkId, adUnitId);
                    // Auto reload
                    LoadInterstitial(adUnitId);
                };

                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    LogError($"Interstitial ad failed to show: {error.GetMessage()}");
                    loadedAds.Remove("interstitial");
                    // Auto reload
                    LoadInterstitial(adUnitId);
                };
            });
        }
        #endregion

        #region Rewarded Ad Implementation
        private void LoadRewarded(string adUnitId)
        {
            LogInfo($"Loading Rewarded: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "rewarded", NetworkId, adUnitId);

            // Clean up old ad if exists
            if (rewardedAds.ContainsKey("rewarded"))
            {
                rewardedAds["rewarded"].Destroy();
                rewardedAds.Remove("rewarded");
            }

            AdRequest request = new AdRequest();

            RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    LogError($"Rewarded ad failed to load: {error?.GetMessage()}");
                    loadedAds.Remove("rewarded");
                    AdEvents.TriggerFailed("rewarded", NetworkId, adUnitId, error?.GetMessage(), error?.GetCode() ?? 0);
                    return;
                }

                LogInfo("Rewarded ad loaded successfully");
                rewardedAds["rewarded"] = ad;
                loadedAds.Add("rewarded");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "rewarded", NetworkId, adUnitId);

                // Register event handlers
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    LogInfo($"Rewarded ad paid: {adValue.Value} {adValue.CurrencyCode}");
                    AdEvents.TriggerPaid("rewarded", NetworkId, adUnitId,
                        (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    LogInfo("Rewarded ad impression recorded");
                    AdEvents.Trigger(AdEventType.AdImpression, "rewarded", NetworkId, adUnitId);
                };

                ad.OnAdClicked += () =>
                {
                    LogInfo("Rewarded ad clicked");
                    AdEvents.Trigger(AdEventType.AdClicked, "rewarded", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    LogInfo("Rewarded ad opened");
                    AdEvents.Trigger(AdEventType.AdShown, "rewarded", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    LogInfo("Rewarded ad closed");
                    AdEvents.Trigger(AdEventType.AdClosed, "rewarded", NetworkId, adUnitId);
                    // Auto reload
                    LoadRewarded(adUnitId);
                };

                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    LogError($"Rewarded ad failed to show: {error.GetMessage()}");
                    loadedAds.Remove("rewarded");
                    // Auto reload
                    LoadRewarded(adUnitId);
                };
            });
        }
        #endregion

        #region App Open Ad Implementation
        private void LoadAppOpen(string adUnitId)
        {
            LogInfo($"Loading App Open: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "appopen", NetworkId, adUnitId);

            // Clean up old ad if exists
            if (appOpenAds.ContainsKey("appopen"))
            {
                appOpenAds["appopen"].Destroy();
                appOpenAds.Remove("appopen");
            }

            AdRequest request = new AdRequest();

            AppOpenAd.Load(adUnitId, request, (AppOpenAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    LogError($"App Open ad failed to load: {error?.GetMessage()}");
                    loadedAds.Remove("appopen");
                    AdEvents.TriggerFailed("appopen", NetworkId, adUnitId, error?.GetMessage(), error?.GetCode() ?? 0);
                    return;
                }

                LogInfo("App Open ad loaded successfully");
                appOpenAds["appopen"] = ad;
                loadedAds.Add("appopen");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "appopen", NetworkId, adUnitId);

                // Register event handlers
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    LogInfo($"App Open ad paid: {adValue.Value} {adValue.CurrencyCode}");
                    AdEvents.TriggerPaid("appopen", NetworkId, adUnitId,
                        (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    LogInfo("App Open ad impression recorded");
                    AdEvents.Trigger(AdEventType.AdImpression, "appopen", NetworkId, adUnitId);
                };

                ad.OnAdClicked += () =>
                {
                    LogInfo("App Open ad clicked");
                    AdEvents.Trigger(AdEventType.AdClicked, "appopen", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    LogInfo("App Open ad opened");
                    AdEvents.Trigger(AdEventType.AdShown, "appopen", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    LogInfo("App Open ad closed");
                    AdEvents.Trigger(AdEventType.AdClosed, "appopen", NetworkId, adUnitId);
                    // Auto reload
                    LoadAppOpen(adUnitId);
                };

                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    LogError($"App Open ad failed to show: {error.GetMessage()}");
                    loadedAds.Remove("appopen");
                    // Auto reload
                    LoadAppOpen(adUnitId);
                };
            });
        }
        #endregion

        #region Rewarded Interstitial Ad Implementation
        private void LoadRewardedInterstitial(string adUnitId)
        {
            LogInfo($"Loading Rewarded Interstitial: {adUnitId}");

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, "rewarded_interstitial", NetworkId, adUnitId);

            // Clean up old ad if exists
            if (rewardedInterstitialAds.ContainsKey("rewarded_interstitial"))
            {
                rewardedInterstitialAds["rewarded_interstitial"].Destroy();
                rewardedInterstitialAds.Remove("rewarded_interstitial");
            }

            AdRequest request = new AdRequest();

            RewardedInterstitialAd.Load(adUnitId, request, (RewardedInterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    LogError($"Rewarded Interstitial ad failed to load: {error?.GetMessage()}");
                    loadedAds.Remove("rewarded_interstitial");
                    AdEvents.TriggerFailed("rewarded_interstitial", NetworkId, adUnitId, error?.GetMessage(), error?.GetCode() ?? 0);
                    return;
                }

                LogInfo("Rewarded Interstitial ad loaded successfully");
                rewardedInterstitialAds["rewarded_interstitial"] = ad;
                loadedAds.Add("rewarded_interstitial");
                AdEvents.Trigger(AdEventType.AdLoadSuccess, "rewarded_interstitial", NetworkId, adUnitId);

                // Register event handlers
                ad.OnAdPaid += (AdValue adValue) =>
                {
                    LogInfo($"Rewarded Interstitial ad paid: {adValue.Value} {adValue.CurrencyCode}");
                    AdEvents.TriggerPaid("rewarded_interstitial", NetworkId, adUnitId,
                        (double)adValue.Value / 1000000.0, adValue.CurrencyCode);
                };

                ad.OnAdImpressionRecorded += () =>
                {
                    LogInfo("Rewarded Interstitial ad impression recorded");
                    AdEvents.Trigger(AdEventType.AdImpression, "rewarded_interstitial", NetworkId, adUnitId);
                };

                ad.OnAdClicked += () =>
                {
                    LogInfo("Rewarded Interstitial ad clicked");
                    AdEvents.Trigger(AdEventType.AdClicked, "rewarded_interstitial", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentOpened += () =>
                {
                    LogInfo("Rewarded Interstitial ad opened");
                    AdEvents.Trigger(AdEventType.AdShown, "rewarded_interstitial", NetworkId, adUnitId);
                };

                ad.OnAdFullScreenContentClosed += () =>
                {
                    LogInfo("Rewarded Interstitial ad closed");
                    AdEvents.Trigger(AdEventType.AdClosed, "rewarded_interstitial", NetworkId, adUnitId);
                    // Auto reload
                    LoadRewardedInterstitial(adUnitId);
                };

                ad.OnAdFullScreenContentFailed += (AdError error) =>
                {
                    LogError($"Rewarded Interstitial ad failed to show: {error.GetMessage()}");
                    loadedAds.Remove("rewarded_interstitial");
                    // Auto reload
                    LoadRewardedInterstitial(adUnitId);
                };
            });
        }
        #endregion

        #region Native Ad Implementation
        private void LoadNative(string adUnitId)
        {
            LogInfo($"Loading Native: {adUnitId}");
            LogWarning("Native ads require custom UI implementation. Please refer to AdMob documentation.");
            // Native ads require custom UI implementation
            // Reference: https://developers.google.com/admob/unity/native
        }
        #endregion

        #region Banner Visibility Control
        public override void HideBanner()
        {
            if (bannerAds.ContainsKey("banner"))
            {
                bannerAds["banner"].Hide();
                LogInfo("Banner ad hidden");
            }
            else
            {
                LogWarning("No banner ad loaded to hide");
            }
        }

        public override void ShowBanner()
        {
            if (bannerAds.ContainsKey("banner"))
            {
                bannerAds["banner"].Show();
                LogInfo("Banner ad shown");
            }
            else
            {
                LogWarning("No banner ad loaded to show");
            }
        }
        #endregion

        #region MREC Visibility Control
        public override void HideMrec()
        {
            if (mrecAds.ContainsKey("mrec"))
            {
                mrecAds["mrec"].Hide();
                LogInfo("MREC ad hidden");
            }
            else
            {
                LogWarning("No MREC ad loaded to hide");
            }
        }

        public override void ShowMrec()
        {
            if (mrecAds.ContainsKey("mrec"))
            {
                mrecAds["mrec"].Show();
                LogInfo("MREC ad shown");
            }
            else
            {
                LogWarning("No MREC ad loaded to show");
            }
        }
        #endregion
    }
#else
    /// <summary>
    /// Stub implementation when Google Mobile Ads SDK is not imported
    /// </summary>
    public class AdMobModule : BaseAdNetworkModule
    {
        public override string NetworkId => "admob";

        public override void Initialize(ThirdLibConfig config)
        {
            base.Initialize(config);
            LogError("Google Mobile Ads SDK is not installed. Please import the SDK to use AdMob.");
        }

        public override void LoadAdUnit(AdUnit adUnit)
        {
            LogError("Google Mobile Ads SDK is not installed.");
        }

        public override void ShowAd(string formatId, string placementId = null)
        {
            LogError("Google Mobile Ads SDK is not installed.");
        }

        public override bool IsAdReady(string formatId, string placementId = null)
        {
            return false;
        }
    }
#endif
}
