using UnityEngine;
using GameDevToi.ThirdLib.Core;
using System.Collections.Generic;
using Firebase.Extensions;
// Uncomment when Firebase SDK is installed
// using Firebase;
// using Firebase.Analytics;
// using Firebase.Extensions;

namespace GameDevToi.ThirdLib
{
    /// <summary>
    /// FirebaseBridge - Singleton tự động khởi tạo và log ad events lên Firebase Analytics
    /// </summary>
    public class FirebaseBridge : MonoBehaviour
    {
        private static FirebaseBridge instance;
        private static readonly object lockObject = new object();
        private static bool isQuitting = false;

        private bool isFirebaseInitialized = false;
        private Queue<System.Action> pendingEvents = new Queue<System.Action>();

        [Header("Firebase Configuration")]
        [SerializeField] private bool autoLogAdEvents = true;
        [SerializeField] private bool logToConsole = true;

        [Header("Event Name Configuration")]
        [SerializeField] private string eventPrefix = "";
        [Tooltip("Use standard Firebase event names (recommended)")]
        [SerializeField] private bool useStandardEventNames = true;

        [Header("Logging Filters")]
        [SerializeField] private bool logLoadStarted = false;
        [SerializeField] private bool logLoadSuccess = true;
        [SerializeField] private bool logLoadFailed = true;
        [SerializeField] private bool logAdShown = true;
        [SerializeField] private bool logAdClicked = true;
        [SerializeField] private bool logAdClosed = true;
        [SerializeField] private bool logAdImpression = true; // QUAN TRỌNG
        [SerializeField] private bool logAdPaid = true;
        [SerializeField] private bool logAdRewarded = true;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static FirebaseBridge Instance
        {
            get
            {
                if (isQuitting)
                {
                    Debug.LogWarning("[FirebaseBridge] Instance already destroyed on application quit.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<FirebaseBridge>();

                        if (instance == null)
                        {
                            GameObject go = new GameObject("[FirebaseBridge]");
                            instance = go.AddComponent<FirebaseBridge>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        public bool IsInitialized => isFirebaseInitialized;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeFirebase();
        }

        private void OnEnable()
        {
            if (autoLogAdEvents)
            {
                SubscribeToAdEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromAdEvents();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Khởi tạo Firebase
        /// </summary>
        private void InitializeFirebase()
        {
            Debug.Log("[FirebaseBridge] Initializing Firebase...");

            // Uncomment when Firebase SDK is installed
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[FirebaseBridge] Firebase initialization failed: {task.Exception?.Message}");
                    isFirebaseInitialized = false;
                    return;
                }

                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    var app = Firebase.FirebaseApp.DefaultInstance;
                    isFirebaseInitialized = true;
                    Debug.Log("[FirebaseBridge] Firebase initialized successfully");

                    // Process pending events
                    ProcessPendingEvents();
                }
                else
                {
                    Debug.LogError($"[FirebaseBridge] Could not resolve all Firebase dependencies: {dependencyStatus}");
                    isFirebaseInitialized = false;
                }
            });
        }

        /// <summary>
        /// Subscribe vào tất cả ad events
        /// </summary>
        private void SubscribeToAdEvents()
        {
            AdEvents.OnAdLoadStarted += OnAdLoadStarted;
            AdEvents.OnAdLoadSuccess += OnAdLoadSuccess;
            AdEvents.OnAdLoadFailed += OnAdLoadFailed;
            AdEvents.OnAdShown += OnAdShown;
            AdEvents.OnAdClicked += OnAdClicked;
            AdEvents.OnAdClosed += OnAdClosed;
            AdEvents.OnAdImpression += OnAdImpression; // QUAN TRỌNG
            AdEvents.OnAdPaid += OnAdPaid;
            AdEvents.OnAdRewarded += OnAdRewarded;

            Debug.Log("[FirebaseBridge] Subscribed to ad events");
        }

        /// <summary>
        /// Unsubscribe khỏi ad events
        /// </summary>
        private void UnsubscribeFromAdEvents()
        {
            AdEvents.OnAdLoadStarted -= OnAdLoadStarted;
            AdEvents.OnAdLoadSuccess -= OnAdLoadSuccess;
            AdEvents.OnAdLoadFailed -= OnAdLoadFailed;
            AdEvents.OnAdShown -= OnAdShown;
            AdEvents.OnAdClicked -= OnAdClicked;
            AdEvents.OnAdClosed -= OnAdClosed;
            AdEvents.OnAdImpression -= OnAdImpression;
            AdEvents.OnAdPaid -= OnAdPaid;
            AdEvents.OnAdRewarded -= OnAdRewarded;
        }

        #region Ad Event Handlers

        private void OnAdLoadStarted(AdEventArgs e)
        {
            if (!logLoadStarted) return;
            LogEvent("ad_load_started", CreateAdParameters(e));
        }

        private void OnAdLoadSuccess(AdEventArgs e)
        {
            if (!logLoadSuccess) return;
            LogEvent("ad_load_success", CreateAdParameters(e));
        }

        private void OnAdLoadFailed(AdEventArgs e)
        {
            if (!logLoadFailed) return;
            var parameters = CreateAdParameters(e);
            parameters["error_message"] = e.ErrorMessage;
            parameters["error_code"] = e.ErrorCode;
            LogEvent("ad_load_failed", parameters);
        }

        private void OnAdShown(AdEventArgs e)
        {
            if (!logAdShown) return;
            LogEvent("ad_show", CreateAdParameters(e));
        }

        private void OnAdClicked(AdEventArgs e)
        {
            if (!logAdClicked) return;
            LogEvent("ad_click", CreateAdParameters(e));
        }

        private void OnAdClosed(AdEventArgs e)
        {
            if (!logAdClosed) return;
            LogEvent("ad_close", CreateAdParameters(e));
        }

        /// <summary>
        /// QUAN TRỌNG: Log ad impression - standard Firebase event
        /// </summary>
        private void OnAdImpression(AdEventArgs e)
        {
            if (!logAdImpression) return;

            // Use Firebase standard event name for ad impression
            string eventName = useStandardEventNames ? "ad_impression" : $"{eventPrefix}ad_impression";

            var parameters = CreateAdParameters(e);
            LogEvent(eventName, parameters);

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] 👁 AD_IMPRESSION: {e.FormatId} from {e.NetworkId}");
            }
        }

        private void OnAdPaid(AdEventArgs e)
        {
            if (!logAdPaid) return;

            var parameters = CreateAdParameters(e);
            parameters["value"] = e.Revenue; // Firebase standard parameter
            parameters["currency"] = e.CurrencyCode;
            parameters["revenue"] = e.Revenue;

            LogEvent("ad_revenue", parameters);

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] 💰 AD_REVENUE: {e.Revenue} {e.CurrencyCode} from {e.FormatId}/{e.NetworkId}");
            }
        }

        private void OnAdRewarded(AdEventArgs e)
        {
            if (!logAdRewarded) return;

            var parameters = CreateAdParameters(e);
            parameters["reward_type"] = e.RewardType;
            parameters["reward_amount"] = e.RewardAmount;

            LogEvent("ad_rewarded", parameters);

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] 🎁 AD_REWARDED: {e.RewardType} x{e.RewardAmount}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo parameters cơ bản cho ad events
        /// </summary>
        private Dictionary<string, object> CreateAdParameters(AdEventArgs e)
        {
            return new Dictionary<string, object>
            {
                { "ad_format", e.FormatId },
                { "ad_network", e.NetworkId },
                { "ad_unit_id", e.AdUnitId },
                { "ad_platform", GetPlatformName() }
            };
        }

        /// <summary>
        /// Log event lên Firebase Analytics
        /// </summary>
        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!isFirebaseInitialized)
            {
                // Queue event để log sau khi Firebase initialized
                pendingEvents.Enqueue(() => LogEvent(eventName, parameters));
                return;
            }

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] Event: {eventName}, Params: {GetParameterString(parameters)}");
            }

            // Uncomment when Firebase SDK is installed

            var firebaseParams = new Firebase.Analytics.Parameter[parameters.Count];
            int index = 0;
            foreach (var kvp in parameters)
            {
                if (kvp.Value is string strValue)
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, strValue);
                else if (kvp.Value is int intValue)
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, intValue);
                else if (kvp.Value is long longValue)
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, longValue);
                else if (kvp.Value is double doubleValue)
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, doubleValue);
                else if (kvp.Value is float floatValue)
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, floatValue);
                else
                    firebaseParams[index] = new Firebase.Analytics.Parameter(kvp.Key, kvp.Value.ToString());

                index++;
            }

            Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParams);

        }

        /// <summary>
        /// Process các events đã queue khi Firebase chưa init
        /// </summary>
        private void ProcessPendingEvents()
        {
            while (pendingEvents.Count > 0)
            {
                var action = pendingEvents.Dequeue();
                action?.Invoke();
            }
        }

        /// <summary>
        /// Get platform name
        /// </summary>
        private string GetPlatformName()
        {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#elif UNITY_WEBGL
            return "webgl";
#else
            return "unknown";
#endif
        }

        /// <summary>
        /// Convert parameters to string for logging
        /// </summary>
        private string GetParameterString(Dictionary<string, object> parameters)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kvp in parameters)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join(", ", parts);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Log custom event (cho dev sử dụng)
        /// </summary>
        public void LogCustomEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            LogEvent(eventName, parameters);
        }

        /// <summary>
        /// Log custom event với single parameter
        /// </summary>
        public void LogCustomEvent(string eventName, string paramName, string paramValue)
        {
            var parameters = new Dictionary<string, object>
            {
                { paramName, paramValue }
            };
            LogEvent(eventName, parameters);
        }

        /// <summary>
        /// Set user property
        /// </summary>
        public void SetUserProperty(string propertyName, string propertyValue)
        {
            if (!isFirebaseInitialized)
            {
                Debug.LogWarning("[FirebaseBridge] Firebase not initialized yet");
                return;
            }

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] Set User Property: {propertyName} = {propertyValue}");
            }

            // Uncomment when Firebase SDK is installed

            Firebase.Analytics.FirebaseAnalytics.SetUserProperty(propertyName, propertyValue);

        }

        /// <summary>
        /// Set user ID
        /// </summary>
        public void SetUserId(string userId)
        {
            if (!isFirebaseInitialized)
            {
                Debug.LogWarning("[FirebaseBridge] Firebase not initialized yet");
                return;
            }

            if (logToConsole)
            {
                Debug.Log($"[FirebaseBridge] Set User ID: {userId}");
            }

            // Uncomment when Firebase SDK is installed

            Firebase.Analytics.FirebaseAnalytics.SetUserId(userId);

        }

        #endregion

        /// <summary>
        /// Auto-initialize khi game start
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Tự động tạo instance khi game start
            var instance = Instance;
            Debug.Log("[FirebaseBridge] Auto-initialized on game start");
        }
    }
}
