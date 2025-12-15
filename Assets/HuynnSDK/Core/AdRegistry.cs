using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevToi.ThirdLib.Core
{
    /// <summary>
    /// Registry để đăng ký Ad Formats và Networks động
    /// Developers có thể thêm custom formats/networks
    /// </summary>
    public static class AdRegistry
    {
        private static Dictionary<string, AdFormatDefinition> registeredFormats = new Dictionary<string, AdFormatDefinition>();
        private static Dictionary<string, AdNetworkDefinition> registeredNetworks = new Dictionary<string, AdNetworkDefinition>();
        private static bool isInitialized = false;

        /// <summary>
        /// Khởi tạo registry với built-in definitions
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized) return;

            // Register built-in formats
            RegisterFormat(AdFormats.Banner);
            RegisterFormat(AdFormats.Interstitial);
            RegisterFormat(AdFormats.Rewarded);
            RegisterFormat(AdFormats.AppOpen);
            RegisterFormat(AdFormats.RewardedInterstitial);
            RegisterFormat(AdFormats.Native);

            // Register built-in networks
            RegisterNetwork(AdNetworks.AdMob);
            RegisterNetwork(AdNetworks.AppLovin);
            RegisterNetwork(AdNetworks.IronSource);
            RegisterNetwork(AdNetworks.UnityAds);

            // Load and register custom definitions
            LoadCustomDefinitions();

            isInitialized = true;
            Debug.Log($"[AdRegistry] Initialized with {registeredFormats.Count} formats and {registeredNetworks.Count} networks");
        }

        /// <summary>
        /// Load custom definitions từ Resources
        /// </summary>
        private static void LoadCustomDefinitions()
        {
            var customDefinitions = Resources.Load<CustomAdDefinitions>("CustomAdDefinitions");
            if (customDefinitions != null)
            {
                customDefinitions.RegisterAll();
                Debug.Log($"[AdRegistry] Loaded custom definitions: {customDefinitions.customFormats.Count} formats, {customDefinitions.customNetworks.Count} networks");
            }
        }

        /// <summary>
        /// Đăng ký Ad Format mới
        /// </summary>
        public static void RegisterFormat(AdFormatDefinition format)
        {
            if (format == null || string.IsNullOrEmpty(format.id))
            {
                Debug.LogError("[AdRegistry] Cannot register null or empty format");
                return;
            }

            if (registeredFormats.ContainsKey(format.id))
            {
                Debug.LogWarning($"[AdRegistry] Format '{format.id}' already registered. Overwriting...");
            }

            registeredFormats[format.id] = format;
            Debug.Log($"[AdRegistry] Registered format: {format.displayName} ({format.id})");
        }

        /// <summary>
        /// Đăng ký Ad Network mới
        /// </summary>
        public static void RegisterNetwork(AdNetworkDefinition network)
        {
            if (network == null || string.IsNullOrEmpty(network.id))
            {
                Debug.LogError("[AdRegistry] Cannot register null or empty network");
                return;
            }

            if (registeredNetworks.ContainsKey(network.id))
            {
                Debug.LogWarning($"[AdRegistry] Network '{network.id}' already registered. Overwriting...");
            }

            registeredNetworks[network.id] = network;
            Debug.Log($"[AdRegistry] Registered network: {network.displayName} ({network.id})");
        }

        /// <summary>
        /// Hủy đăng ký Ad Format
        /// </summary>
        public static bool UnregisterFormat(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[AdRegistry] Cannot unregister format with empty ID");
                return false;
            }

            if (!registeredFormats.ContainsKey(id))
            {
                Debug.LogWarning($"[AdRegistry] Format '{id}' not found in registry");
                return false;
            }

            var format = registeredFormats[id];
            registeredFormats.Remove(id);
            Debug.Log($"[AdRegistry] Unregistered format: {format.displayName} ({id})");
            return true;
        }

        /// <summary>
        /// Hủy đăng ký Ad Network
        /// </summary>
        public static bool UnregisterNetwork(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[AdRegistry] Cannot unregister network with empty ID");
                return false;
            }

            if (!registeredNetworks.ContainsKey(id))
            {
                Debug.LogWarning($"[AdRegistry] Network '{id}' not found in registry");
                return false;
            }

            var network = registeredNetworks[id];
            registeredNetworks.Remove(id);
            Debug.Log($"[AdRegistry] Unregistered network: {network.displayName} ({id})");
            return true;
        }

        /// <summary>
        /// Hủy đăng ký tất cả custom definitions (giữ lại built-in)
        /// </summary>
        public static void UnregisterAllCustom()
        {
            var customDefinitions = Resources.Load<CustomAdDefinitions>("CustomAdDefinitions");
            if (customDefinitions != null)
            {
                int formatCount = 0;
                int networkCount = 0;

                foreach (var format in customDefinitions.customFormats)
                {
                    if (format.IsValid() && UnregisterFormat(format.id))
                    {
                        formatCount++;
                    }
                }

                foreach (var network in customDefinitions.customNetworks)
                {
                    if (network.IsValid() && UnregisterNetwork(network.id))
                    {
                        networkCount++;
                    }
                }

                Debug.Log($"[AdRegistry] Unregistered {formatCount} custom formats and {networkCount} custom networks");
            }
        }

        /// <summary>
        /// Lấy tất cả formats đã đăng ký
        /// </summary>
        public static List<AdFormatDefinition> GetAllFormats()
        {
            if (!isInitialized) Initialize();
            return new List<AdFormatDefinition>(registeredFormats.Values);
        }

        /// <summary>
        /// Lấy tất cả networks đã đăng ký
        /// </summary>
        public static List<AdNetworkDefinition> GetAllNetworks()
        {
            if (!isInitialized) Initialize();
            return new List<AdNetworkDefinition>(registeredNetworks.Values);
        }

        /// <summary>
        /// Lấy format theo ID
        /// </summary>
        public static AdFormatDefinition GetFormat(string id)
        {
            if (!isInitialized) Initialize();
            return registeredFormats.TryGetValue(id, out var format) ? format : null;
        }

        /// <summary>
        /// Lấy network theo ID
        /// </summary>
        public static AdNetworkDefinition GetNetwork(string id)
        {
            if (!isInitialized) Initialize();
            return registeredNetworks.TryGetValue(id, out var network) ? network : null;
        }

        /// <summary>
        /// Kiểm tra format có tồn tại không
        /// </summary>
        public static bool HasFormat(string id)
        {
            if (!isInitialized) Initialize();
            return registeredFormats.ContainsKey(id);
        }

        /// <summary>
        /// Kiểm tra network có tồn tại không
        /// </summary>
        public static bool HasNetwork(string id)
        {
            if (!isInitialized) Initialize();
            return registeredNetworks.ContainsKey(id);
        }

        /// <summary>
        /// Reset registry (dùng cho testing)
        /// </summary>
        public static void Reset()
        {
            registeredFormats.Clear();
            registeredNetworks.Clear();
            isInitialized = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Initialize cho Editor (vì RuntimeInitialize không chạy trong Editor)
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeInEditor()
        {
            Initialize();
        }
#endif
    }
}
