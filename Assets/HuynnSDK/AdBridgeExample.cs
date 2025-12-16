using UnityEngine;
using GameDevToi.ThirdLib;
using GameDevToi.ThirdLib.Core;
using GameDevToi.ThirdLib.AdModule;

namespace GameDevToi.ThirdLib.Example
{
    /// <summary>
    /// Ví dụ sử dụng AdBridge - Type-Safe Approach (Recommended)
    /// </summary>
    public class AdBridgeExample : MonoBehaviour
    {
        private void Start()
        {
            // AdBridge tự động khởi tạo khi game start
            // Bạn không cần làm gì cả!

            // Có thể lấy instance và kiểm tra status
            if (AdBridge.Instance != null)
            {
                Debug.Log("AdBridge is ready!");

                // In debug info
                Debug.Log(AdBridge.Instance.GetDebugInfo());
            }

            AdBridge.Instance.ShowAd(CustomAdFormats.HuynnAdmobFormat);
        }

        // ===== ✅ RECOMMENDED: Type-Safe Approach =====

        // Ví dụ hiển thị Banner (sử dụng AdFormats constants)
        public void ShowBanner()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ShowAd(AdFormats.Banner); // Type-safe!
            }
        }

        // Ví dụ hiển thị MREC (Medium Rectangle 300x250)
        public void ShowMrec()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ShowAd(AdFormats.Mrec); // Type-safe!
            }
        }

        // Ví dụ ẩn MREC
        public void HideMrec()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.HideMrec();
            }
        }

        // Ví dụ hiển thị Interstitial
        public void ShowInterstitial()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ShowAd(AdFormats.Interstitial); // Type-safe!
            }
        }

        // Ví dụ hiển thị Rewarded
        public void ShowRewarded()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ShowAd(AdFormats.Rewarded); // Type-safe!
            }
        }

        // Ví dụ hiển thị App Open Ad
        public void ShowAppOpen()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ShowAd(AdFormats.AppOpen); // Type-safe!
            }
        }

        // Ví dụ kiểm tra ad có sẵn sàng không (Type-safe)
        public void CheckAdReady()
        {
            if (AdBridge.Instance != null)
            {
                bool interstitialReady = AdBridge.Instance.IsAdReady(AdFormats.Interstitial);
                Debug.Log($"Interstitial ready: {interstitialReady}");

                bool rewardedReady = AdBridge.Instance.IsAdReady(AdFormats.Rewarded);
                Debug.Log($"Rewarded ready: {rewardedReady}");

                bool bannerReady = AdBridge.Instance.IsAdReady(AdFormats.Banner);
                Debug.Log($"Banner ready: {bannerReady}");

                bool mrecReady = AdBridge.Instance.IsAdReady(AdFormats.Mrec);
                Debug.Log($"MREC ready: {mrecReady}");
            }
        }

        // Ví dụ kiểm tra ad của network cụ thể (Type-safe)
        public void CheckAdMobInterstitial()
        {
            if (AdBridge.Instance != null)
            {
                bool ready = AdBridge.Instance.IsAdReady(AdFormats.Interstitial, AdNetworks.AdMob);
                Debug.Log($"AdMob Interstitial ready: {ready}");
            }
        }

        // Ví dụ lấy module cụ thể để sử dụng
        public void UseSpecificModule()
        {
            if (AdBridge.Instance != null)
            {
                // Lấy AdMob module (Type-safe)
                var adMobModule = AdBridge.Instance.GetModule(AdNetworks.AdMob.id);
                if (adMobModule != null && adMobModule.IsInitialized)
                {
                    Debug.Log("AdMob module is initialized and ready");
                    // Có thể gọi methods cụ thể của module nếu cần
                    adMobModule.ShowAd("interstitial");
                }

                // Lấy AppLovin module
                var appLovinModule = AdBridge.Instance.GetModule("applovin");
                if (appLovinModule != null && appLovinModule.IsInitialized)
                {
                    Debug.Log("AppLovin module is initialized and ready");
                }

                // Lấy IronSource module
                var ironSourceModule = AdBridge.Instance.GetModule("ironsource");
                if (ironSourceModule != null && ironSourceModule.IsInitialized)
                {
                    Debug.Log("IronSource module is initialized and ready");
                }
            }
        }

        // Ví dụ reload ad units khi config thay đổi
        public void ReloadConfig()
        {
            if (AdBridge.Instance != null)
            {
                AdBridge.Instance.ReloadAdUnits();
                Debug.Log("Ad units reloaded");
            }
        }

        // Ví dụ sử dụng AdRegistry để list formats và networks
        public void ListAvailableFormatsAndNetworks()
        {
            Debug.Log("=== Available Ad Formats ===");
            var formats = AdRegistry.GetAllFormats();
            foreach (var format in formats)
            {
                Debug.Log($"- {format.displayName} (ID: {format.id})");
            }

            Debug.Log("\n=== Available Ad Networks ===");
            var networks = AdRegistry.GetAllNetworks();
            foreach (var network in networks)
            {
                Debug.Log($"- {network.displayName} (ID: {network.id})");
            }
        }

        // Test button trong Inspector
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));

            GUILayout.Label("AdBridge Test Panel", GUI.skin.box);

            if (GUILayout.Button("Show Banner", GUILayout.Height(40)))
            {
                ShowBanner();
            }

            if (GUILayout.Button("Show MREC", GUILayout.Height(40)))
            {
                ShowMrec();
            }

            if (GUILayout.Button("Hide MREC", GUILayout.Height(40)))
            {
                HideMrec();
            }

            if (GUILayout.Button("Show Interstitial", GUILayout.Height(40)))
            {
                ShowInterstitial();
            }

            if (GUILayout.Button("Show Rewarded", GUILayout.Height(40)))
            {
                ShowRewarded();
            }

            if (GUILayout.Button("Check Ad Ready", GUILayout.Height(40)))
            {
                CheckAdReady();
            }

            if (GUILayout.Button("Print Debug Info", GUILayout.Height(40)))
            {
                if (AdBridge.Instance != null)
                {
                    Debug.Log(AdBridge.Instance.GetDebugInfo());
                }
            }

            GUILayout.EndArea();
        }
    }
}
