using UnityEngine;
using GameDevToi.ThirdLib.Core;

namespace GameDevToi.ThirdLib.Examples
{
    /// <summary>
    /// Example: Sử dụng Custom Ad Definitions để tránh magic strings
    /// </summary>
    public class CustomAdDefinitionsExample : MonoBehaviour
    {
        void Start()
        {
            // ❌ CÁC CÁCH KHÔNG NÊN (Magic Strings - dễ lỗi typo)
            BadExample();

            // ✅ CÁC CÁCH NÊN DÙNG (Type-safe constants)
            GoodExample();
        }

        void BadExample()
        {
            // ❌ Sử dụng magic string - dễ typo, không có IntelliSense
            var adUnit1 = new AdUnit("custom_banner", "my_network");
            adUnit1.formatId = "custm_banner"; // Typo! Sẽ không phát hiện cho đến runtime

            // ❌ Không biết format nào available
            AdBridge.Instance.ShowAd("banner"); // Có banner không? Phải mở config để check
        }

        void GoodExample()
        {
            // ✅ Sử dụng built-in constants
            var adUnit1 = new AdUnit(AdFormats.Banner.id, AdNetworks.AdMob.id);
            adUnit1.formatId = AdFormats.Interstitial.id; // IntelliSense support!

            // ✅ Sử dụng custom constants (sau khi generate)
            // Uncomment sau khi generate CustomAdConstants.cs
            /*
            var adUnit2 = new AdUnit(
                CustomAdFormats.MyCustomBanner,  // Type-safe!
                CustomAdNetworks.MyCustomNetwork  // IDE sẽ gợi ý!
            );

            // Load specific ad
            AdBridge.Instance.LoadSpecificAdUnit(
                CustomAdFormats.MyCustomBanner,
                CustomAdNetworks.MyCustomNetwork
            );

            // Show ad
            AdBridge.Instance.ShowAd(CustomAdFormats.MyCustomBanner);
            */

            // ✅ Sử dụng AdFormats/AdNetworks cho built-in
            AdBridge.Instance.ShowAd(AdFormats.Rewarded.id);

            // ✅ Kiểm tra ad ready
            if (AdBridge.Instance.IsAdReady(AdFormats.Interstitial.id))
            {
                AdBridge.Instance.ShowAd(AdFormats.Interstitial.id);
            }
        }

        // ✅ Method với type-safe parameters
        void ShowInterstitialAd()
        {
            // Rõ ràng, không cần comment
            AdBridge.Instance.ShowAd(AdFormats.Interstitial.id);
        }

        // ✅ Example: Dynamic format selection
        void ShowAdByType(AdFormatDefinition format)
        {
            if (AdBridge.Instance.IsAdReady(format.id))
            {
                Debug.Log($"Showing {format.displayName} ad");
                AdBridge.Instance.ShowAd(format.id);
            }
            else
            {
                Debug.LogWarning($"{format.displayName} ad not ready");
            }
        }
    }

    /*
     * HƯỚNG DẪN TẠO CUSTOM DEFINITIONS:
     * 
     * 1. Mở Window: 3rdLib > Open Window
     * 2. Chọn tab "Custom Definitions"
     * 3. Click "Add Custom Format" hoặc "Add Custom Network"
     * 4. Điền thông tin:
     *    - ID: lowercase, no spaces (e.g., "rewarded_survey")
     *    - Display Name: Tên hiển thị (e.g., "Rewarded Survey")
     *    - Description: Mô tả (optional)
     * 5. Click "Validate All" để kiểm tra lỗi
     * 6. Click "Generate Constants Class" để tạo file CustomAdConstants.cs
     * 7. Click "Register All to AdRegistry" để đăng ký vào runtime
     * 
     * SAU KHI GENERATE, SỬ DỤNG:
     * 
     * // Trong code:
     * adUnit.formatId = CustomAdFormats.RewardedSurvey;  // Type-safe!
     * adUnit.networkId = CustomAdNetworks.MyNetwork;
     * 
     * // Load ad:
     * AdBridge.Instance.LoadSpecificAdUnit(
     *     CustomAdFormats.RewardedSurvey,
     *     CustomAdNetworks.MyNetwork
     * );
     * 
     * // Show ad:
     * AdBridge.Instance.ShowAd(CustomAdFormats.RewardedSurvey);
     */
}
