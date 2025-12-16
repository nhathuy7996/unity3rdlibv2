using UnityEngine;
using GameDevToi.ThirdLib.Core;

namespace GameDevToi.ThirdLib.AdModule
{
    /// <summary>
    /// Base class cho các module mạng quảng cáo
    /// Implement các logic chung, các module cụ thể kế thừa và override
    /// </summary>
    public abstract class BaseAdNetworkModule : IAdNetworkModule
    {
        protected bool isInitialized = false;
        protected ThirdLibConfig config;

        public abstract string NetworkId { get; }

        public bool IsInitialized => isInitialized;

        public virtual void Initialize(ThirdLibConfig config)
        {
            this.config = config;
            var networkDef = AdRegistry.GetNetwork(NetworkId);
            string displayName = networkDef?.displayName ?? NetworkId;
            UnityEngine.Debug.Log($"[{displayName}] Initializing...");
        }

        public abstract void LoadAdUnit(AdUnit adUnit);

        public abstract void ShowAd(string formatId, string placementId = null);

        public abstract bool IsAdReady(string formatId, string placementId = null);

        public virtual void DestroyAd(string formatId)
        {
            var formatDef = AdRegistry.GetFormat(formatId);
            string formatName = formatDef?.displayName ?? formatId;
            LogInfo($"Destroying {formatName} ad");
        }

        public virtual void HideBanner()
        {
            LogWarning("HideBanner not implemented for this network");
        }

        public virtual void ShowBanner()
        {
            LogWarning("ShowBanner not implemented for this network");
        }

        public virtual void HideMrec()
        {
            LogWarning("HideMrec not implemented for this network");
        }

        public virtual void ShowMrec()
        {
            LogWarning("ShowMrec not implemented for this network");
        }

        protected void LogInfo(string message)
        {
            var networkDef = AdRegistry.GetNetwork(NetworkId);
            string displayName = networkDef?.displayName ?? NetworkId;

            // Get the calling method info using StackTrace
            var stackTrace = new System.Diagnostics.StackTrace(1, true);
            var frame = stackTrace.GetFrame(0);

            if (frame != null)
            {
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                if (!string.IsNullOrEmpty(fileName))
                {
                    // Log with file context for proper navigation
                    UnityEngine.Debug.LogFormat(LogType.Log, LogOption.None, null,
                        $"[{displayName}] {message}\n  (at {fileName}:{lineNumber})");
                    return;
                }
            }

            // Fallback to normal logging
            UnityEngine.Debug.Log($"[{displayName}] {message}");
        }

        protected void LogWarning(string message)
        {
            var networkDef = AdRegistry.GetNetwork(NetworkId);
            string displayName = networkDef?.displayName ?? NetworkId;

            var stackTrace = new System.Diagnostics.StackTrace(1, true);
            var frame = stackTrace.GetFrame(0);

            if (frame != null)
            {
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                if (!string.IsNullOrEmpty(fileName))
                {
                    UnityEngine.Debug.LogFormat(LogType.Warning, LogOption.None, null,
                        $"[{displayName}] {message}\n  (at {fileName}:{lineNumber})");
                    return;
                }
            }

            UnityEngine.Debug.LogWarning($"[{displayName}] {message}");
        }

        protected void LogError(string message)
        {
            var networkDef = AdRegistry.GetNetwork(NetworkId);
            string displayName = networkDef?.displayName ?? NetworkId;

            var stackTrace = new System.Diagnostics.StackTrace(1, true);
            var frame = stackTrace.GetFrame(0);

            if (frame != null)
            {
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                if (!string.IsNullOrEmpty(fileName))
                {
                    UnityEngine.Debug.LogFormat(LogType.Error, LogOption.None, null,
                        $"[{displayName}] {message}\n  (at {fileName}:{lineNumber})");
                    return;
                }
            }

            UnityEngine.Debug.LogError($"[{displayName}] {message}");
        }
    }
}