using UnityEngine;
using GameDevToi.ThirdLib.Core;

namespace GameDevToi.ThirdLib.AdModule
{
    /// <summary>
    /// Interface cho các module mạng quảng cáo
    /// </summary>
    public interface IAdNetworkModule
    {
        /// <summary>
        /// ID của mạng quảng cáo (e.g., "admob", "applovin")
        /// </summary>
        string NetworkId { get; }

        /// <summary>
        /// Khởi tạo SDK của mạng quảng cáo
        /// </summary>
        void Initialize(ThirdLibConfig config);

        /// <summary>
        /// Kiểm tra xem SDK đã được khởi tạo chưa
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Load một ad unit
        /// </summary>
        void LoadAdUnit(AdUnit adUnit);

        /// <summary>
        /// Hiển thị quảng cáo
        /// </summary>
        void ShowAd(string formatId, string placementId = null);

        /// <summary>
        /// Kiểm tra xem quảng cáo đã sẵn sàng chưa
        /// </summary>
        bool IsAdReady(string formatId, string placementId = null);

        /// <summary>
        /// Hủy quảng cáo
        /// </summary>
        void DestroyAd(string formatId);

        /// <summary>
        /// Ẩn banner (chỉ áp dụng cho banner ads)
        /// </summary>
        void HideBanner();

        /// <summary>
        /// Hiện banner (chỉ áp dụng cho banner ads)
        /// </summary>
        void ShowBanner();

        /// <summary>
        /// Ẩn MREC (Medium Rectangle 300x250)
        /// </summary>
        void HideMrec();

        /// <summary>
        /// Hiện MREC (Medium Rectangle 300x250)
        /// </summary>
        void ShowMrec();
    }
}
