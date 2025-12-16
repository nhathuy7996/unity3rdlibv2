using System.Collections.Generic;
using UnityEngine;
using GameDevToi.ThirdLib.Core;

namespace GameDevToi.ThirdLib.AdModule
{
#if APPLOVIN_MAX
    /// <summary>
    /// Module cho AppLovin MAX với full implementation
    /// </summary>
    public class AppLovinModule : BaseAdNetworkModule
    {
        public override string NetworkId => "applovin";

        // Dictionary to store ad unit IDs by format
        private Dictionary<string, string> adUnitIds = new Dictionary<string, string>();
        private HashSet<string> loadedAds = new HashSet<string>();

        public override void Initialize(ThirdLibConfig config)
        {
            base.Initialize(config);

            string sdkKey = config.appLovinSdkKey;

            if (string.IsNullOrEmpty(sdkKey))
            {
                LogWarning("AppLovin SDK Key not configured");
                return;
            }

            LogInfo($"AppLovin initializing with SDK Key: {sdkKey}");

            // SDK Key should be set in AppLovin Integration Manager
            MaxSdk.InitializeSdk();

            // Setup callbacks
            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                isInitialized = true;
                LogInfo("AppLovin MAX initialized successfully");
            };

            // Attach ad callbacks
            AttachAdCallbacks();
        }

        private void AttachAdCallbacks()
        {
            // Banner callbacks
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerClicked;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerRevenuePaid;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerExpanded;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerCollapsed;

            // Interstitial callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaid;

            // Rewarded callbacks
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClicked;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedRevenuePaid;

            // MREC callbacks
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMrecLoaded;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMrecLoadFailed;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMrecClicked;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMrecRevenuePaid;
        }

        public override void LoadAdUnit(AdUnit adUnit)
        {
            if (!isInitialized)
            {
                LogError("AppLovin not initialized. Cannot load ad unit.");
                return;
            }

            string adUnitId = adUnit.GetAdUnitId();
            var formatDef = adUnit.GetFormat();
            string formatName = formatDef?.displayName ?? adUnit.formatId;
            LogInfo($"Loading {formatName} ad with ID: {adUnitId}");

            // Store ad unit ID
            adUnitIds[adUnit.formatId] = adUnitId;

            string formatId = adUnit.formatId;

            // Trigger load started event
            AdEvents.Trigger(AdEventType.AdLoadStarted, formatId, NetworkId, adUnitId);

            if (formatId == "banner") LoadBanner(adUnitId);
            else if (formatId == "mrec") LoadMrec(adUnitId);
            else if (formatId == "interstitial") LoadInterstitial(adUnitId);
            else if (formatId == "rewarded") LoadRewarded(adUnitId);
            else LogWarning($"Unknown ad format: {formatId}");
        }

        private void LoadBanner(string adUnitId)
        {
            var config = new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter);
            MaxSdk.CreateBanner(adUnitId, config);
            MaxSdk.SetBannerBackgroundColor(adUnitId, Color.black);
            MaxSdk.ShowBanner(adUnitId);
        }

        private void LoadMrec(string adUnitId)
        {
            var config = new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomRight);
            MaxSdk.CreateMRec(adUnitId, config);
            MaxSdk.ShowMRec(adUnitId);
        }

        private void LoadInterstitial(string adUnitId)
        {
            MaxSdk.LoadInterstitial(adUnitId);
        }

        private void LoadRewarded(string adUnitId)
        {
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public override void ShowAd(string formatId, string placementId = null)
        {
            if (!isInitialized)
            {
                LogError("AppLovin not initialized. Cannot show ad.");
                return;
            }

            if (!adUnitIds.ContainsKey(formatId))
            {
                LogWarning($"No ad unit loaded for format: {formatId}");
                return;
            }

            string adUnitId = adUnitIds[formatId];
            var formatDef = AdRegistry.GetFormat(formatId);
            string formatName = formatDef?.displayName ?? formatId;

            if (!IsAdReady(formatId, placementId))
            {
                LogWarning($"{formatName} ad is not ready to show");
                return;
            }

            LogInfo($"Showing {formatName} ad");

            if (formatId == "banner")
            {
                MaxSdk.ShowBanner(adUnitId);
            }
            else if (formatId == "mrec")
            {
                MaxSdk.ShowMRec(adUnitId);
            }
            else if (formatId == "interstitial")
            {
                MaxSdk.ShowInterstitial(adUnitId);
            }
            else if (formatId == "rewarded")
            {
                MaxSdk.ShowRewardedAd(adUnitId);
            }
        }

        public override bool IsAdReady(string formatId, string placementId = null)
        {
            if (!isInitialized) return false;
            if (!adUnitIds.ContainsKey(formatId)) return false;

            string adUnitId = adUnitIds[formatId];

            if (formatId == "banner")
            {
                return loadedAds.Contains("banner");
            }
            else if (formatId == "mrec")
            {
                return loadedAds.Contains("mrec");
            }
            else if (formatId == "interstitial")
            {
                return MaxSdk.IsInterstitialReady(adUnitId);
            }
            else if (formatId == "rewarded")
            {
                return MaxSdk.IsRewardedAdReady(adUnitId);
            }

            return false;
        }

        public override void HideBanner()
        {
            if (adUnitIds.ContainsKey("banner"))
            {
                MaxSdk.HideBanner(adUnitIds["banner"]);
                LogInfo("Banner hidden");
            }
        }

        public override void ShowBanner()
        {
            if (adUnitIds.ContainsKey("banner"))
            {
                MaxSdk.ShowBanner(adUnitIds["banner"]);
                LogInfo("Banner shown");
            }
        }

        public override void HideMrec()
        {
            if (adUnitIds.ContainsKey("mrec"))
            {
                MaxSdk.HideMRec(adUnitIds["mrec"]);
                LogInfo("MREC hidden");
            }
        }

        public override void ShowMrec()
        {
            if (adUnitIds.ContainsKey("mrec"))
            {
                MaxSdk.ShowMRec(adUnitIds["mrec"]);
                LogInfo("MREC shown");
            }
        }

        public override void DestroyAd(string formatId)
        {
            if (formatId == "banner" && adUnitIds.ContainsKey("banner"))
            {
                MaxSdk.DestroyBanner(adUnitIds["banner"]);
                loadedAds.Remove("banner");
                LogInfo("Banner destroyed");
            }
            else if (formatId == "mrec" && adUnitIds.ContainsKey("mrec"))
            {
                MaxSdk.DestroyMRec(adUnitIds["mrec"]);
                loadedAds.Remove("mrec");
                LogInfo("MREC destroyed");
            }
        }

        #region Banner Callbacks

        private void OnBannerLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            loadedAds.Add("banner");
            LogInfo($"Banner loaded: {adUnitId}");
            AdEvents.Trigger(AdEventType.AdLoadSuccess, "banner", NetworkId, adUnitId);
            AdEvents.Trigger(AdEventType.AdImpression, "banner", NetworkId, adUnitId);
        }

        private void OnBannerLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogError($"Banner load failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("banner", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);
        }

        private void OnBannerClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Banner clicked");
            AdEvents.Trigger(AdEventType.AdClicked, "banner", NetworkId, adUnitId);
        }

        private void OnBannerRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double revenue = adInfo.Revenue;
            LogInfo($"Banner revenue: {revenue:F4} USD");
            AdEvents.TriggerPaid("banner", NetworkId, adUnitId, revenue, "USD");
        }

        private void OnBannerExpanded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo("Banner expanded");
        }

        private void OnBannerCollapsed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo("Banner collapsed");
        }

        #endregion

        #region Interstitial Callbacks

        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Interstitial loaded: {adUnitId}");
            AdEvents.Trigger(AdEventType.AdLoadSuccess, "interstitial", NetworkId, adUnitId);
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogError($"Interstitial load failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("interstitial", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Interstitial displayed");
            AdEvents.Trigger(AdEventType.AdShown, "interstitial", NetworkId, adUnitId);
            AdEvents.Trigger(AdEventType.AdImpression, "interstitial", NetworkId, adUnitId);
        }

        private void OnInterstitialClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Interstitial clicked");
            AdEvents.Trigger(AdEventType.AdClicked, "interstitial", NetworkId, adUnitId);
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Interstitial hidden");
            AdEvents.Trigger(AdEventType.AdClosed, "interstitial", NetworkId, adUnitId);

            // Reload interstitial
            LoadInterstitial(adUnitId);
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogError($"Interstitial display failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("interstitial", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);

            // Reload interstitial
            LoadInterstitial(adUnitId);
        }
        private void OnInterstitialRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double revenue = adInfo.Revenue;
            LogInfo($"Interstitial revenue: {revenue:F4} USD");
            AdEvents.TriggerPaid("interstitial", NetworkId, adUnitId, revenue, "USD");
        }

        #endregion

        #region Rewarded Callbacks

        private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Rewarded loaded: {adUnitId}");
            AdEvents.Trigger(AdEventType.AdLoadSuccess, "rewarded", NetworkId, adUnitId);
        }

        private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogError($"Rewarded load failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("rewarded", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);
        }

        private void OnRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Rewarded displayed");
            AdEvents.Trigger(AdEventType.AdShown, "rewarded", NetworkId, adUnitId);
            AdEvents.Trigger(AdEventType.AdImpression, "rewarded", NetworkId, adUnitId);
        }

        private void OnRewardedClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Rewarded clicked");
            AdEvents.Trigger(AdEventType.AdClicked, "rewarded", NetworkId, adUnitId);
        }

        private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Rewarded hidden");
            AdEvents.Trigger(AdEventType.AdClosed, "rewarded", NetworkId, adUnitId);

            // Reload rewarded
            LoadRewarded(adUnitId);
        }

        private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogError($"Rewarded display failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("rewarded", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);

            // Reload rewarded
            LoadRewarded(adUnitId);
        }
        private void OnRewardedReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"Rewarded reward received: {reward.Amount} {reward.Label}");
            AdEvents.TriggerRewarded("rewarded", NetworkId, adUnitId, reward.Label, reward.Amount);
        }

        private void OnRewardedRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double revenue = adInfo.Revenue;
            LogInfo($"Rewarded revenue: {revenue:F4} USD");
            AdEvents.TriggerPaid("rewarded", NetworkId, adUnitId, revenue, "USD");
        }

        #endregion

        #region MREC Callbacks

        private void OnMrecLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            loadedAds.Add("mrec");
            LogInfo($"MREC loaded: {adUnitId}");
            AdEvents.Trigger(AdEventType.AdLoadSuccess, "mrec", NetworkId, adUnitId);
            AdEvents.Trigger(AdEventType.AdImpression, "mrec", NetworkId, adUnitId);
        }

        private void OnMrecLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogError($"MREC load failed: {errorInfo.Message}");
            AdEvents.TriggerFailed("mrec", NetworkId, adUnitId, errorInfo.Message, (int)errorInfo.Code);
        }

        private void OnMrecClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogInfo($"MREC clicked");
            AdEvents.Trigger(AdEventType.AdClicked, "mrec", NetworkId, adUnitId);
        }

        private void OnMrecRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            double revenue = adInfo.Revenue;
            LogInfo($"MREC revenue: {revenue:F4} USD");
            AdEvents.TriggerPaid("mrec", NetworkId, adUnitId, revenue, "USD");
        }

        #endregion
    }
#else
    /// <summary>
    /// Stub implementation when AppLovin MAX SDK is not imported
    /// </summary>
    public class AppLovinModule : BaseAdNetworkModule
    {
        public override string NetworkId => "applovin";

        public override void Initialize(ThirdLibConfig config)
        {
            base.Initialize(config);
            LogError("AppLovin MAX SDK is not installed. Please import the SDK to use AppLovin.");
        }

        public override void LoadAdUnit(AdUnit adUnit)
        {
            LogError("AppLovin MAX SDK is not installed.");
        }

        public override void ShowAd(string formatId, string placementId = null)
        {
            LogError("AppLovin MAX SDK is not installed.");
        }

        public override bool IsAdReady(string formatId, string placementId = null)
        {
            return false;
        }
    }
#endif
}
