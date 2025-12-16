using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameDevToi.ThirdLib.Core;
using GameDevToi.ThirdLib.AdModule;

namespace GameDevToi.ThirdLib
{
    /// <summary>
    /// AdBridge - Singleton quản lý tất cả các mạng quảng cáo
    /// Tự động khởi tạo khi game start
    /// Sử dụng string-based IDs để có thể mở rộng động
    /// </summary>
    public class AdBridge : MonoBehaviour
    {
        private static AdBridge instance;
        private static readonly object lockObject = new object();
        private static bool isQuitting = false;

        private Dictionary<string, IAdNetworkModule> adModules;
        private ThirdLibConfig config;
        private bool isInitialized = false;

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static AdBridge Instance
        {
            get
            {
                if (isQuitting)
                {
                    Debug.LogWarning("[AdBridge] Instance already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<AdBridge>();

                        if (instance == null)
                        {
                            GameObject go = new GameObject("[AdBridge]");
                            instance = go.AddComponent<AdBridge>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Khởi tạo AdBridge và các module
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[AdBridge] Already initialized");
                return;
            }

            Debug.Log("[AdBridge] Initializing...");

            // Load config
            config = ThirdLibConfig.Instance;
            if (config == null)
            {
                Debug.LogError("[AdBridge] ThirdLibConfig not found! Cannot initialize.");
                return;
            }

            // Khởi tạo dictionary cho modules
            adModules = new Dictionary<string, IAdNetworkModule>();

            // Đăng ký các built-in modules
            RegisterBuiltInModules();

            // Phân tích ad units và init các module cần thiết (async)
            _ = InitializeRequiredModulesAsync();
        }

        /// <summary>
        /// Đăng ký các built-in modules tự động qua reflection
        /// </summary>
        private void RegisterBuiltInModules()
        {
            // Tìm tất cả các class implement IAdNetworkModule trong assembly hiện tại
            var moduleTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && !type.IsInterface &&
                              typeof(IAdNetworkModule).IsAssignableFrom(type) &&
                              type != typeof(BaseAdNetworkModule))
                .ToList();

            Debug.Log($"[AdBridge] Found {moduleTypes.Count} ad network module types");

            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    // Tạo instance của module
                    var module = System.Activator.CreateInstance(moduleType) as IAdNetworkModule;
                    if (module != null)
                    {
                        RegisterAdModule(module);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AdBridge] Failed to create instance of {moduleType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Đăng ký một ad network module
        /// PUBLIC để developers có thể đăng ký custom modules
        /// </summary>
        public void RegisterAdModule(IAdNetworkModule module)
        {
            if (module == null)
            {
                Debug.LogError("[AdBridge] Cannot register null module");
                return;
            }

            string networkId = module.NetworkId;
            if (string.IsNullOrEmpty(networkId))
            {
                Debug.LogError("[AdBridge] Cannot register module with empty network ID");
                return;
            }

            if (adModules.ContainsKey(networkId))
            {
                Debug.LogWarning($"[AdBridge] Module for '{networkId}' already registered. Overwriting...");
            }

            adModules[networkId] = module;

            var networkDef = AdRegistry.GetNetwork(networkId);
            string displayName = networkDef?.displayName ?? networkId;
            Debug.Log($"[AdBridge] Registered module: {displayName} ({networkId})");
        }

        /// <summary>
        /// Phân tích ad units và khởi tạo các module cần thiết (async)
        /// </summary>
        private async Task InitializeRequiredModulesAsync()
        {
            if (config.adUnits == null || config.adUnits.Count == 0)
            {
                Debug.LogWarning("[AdBridge] No ad units configured");
                isInitialized = true;
                return;
            }

            // Lấy danh sách các network được sử dụng từ ad units
            var usedNetworks = config.adUnits
                .Where(ad => ad.isActive && ad.IsValid())
                .Select(ad => ad.networkId)
                .Distinct()
                .ToList();

            Debug.Log($"[AdBridge] Found {usedNetworks.Count} active ad networks: {string.Join(", ", usedNetworks)}");

            // Khởi tạo từng module
            foreach (var networkId in usedNetworks)
            {
                if (adModules.ContainsKey(networkId))
                {
                    var networkDef = AdRegistry.GetNetwork(networkId);
                    string displayName = networkDef?.displayName ?? networkId;
                    Debug.Log($"[AdBridge] Initializing {displayName} module...");

                    adModules[networkId].Initialize(config);

                    // Đợi cho đến khi module initialized (timeout 10 giây)
                    bool initialized = await WaitForModuleInitialized(networkId, 10f);

                    if (initialized)
                    {
                        Debug.Log($"[AdBridge] {displayName} module initialized successfully");

                        // Load các ad units của network này
                        LoadAdUnitsForNetwork(networkId);
                    }
                    else
                    {
                        Debug.LogWarning($"[AdBridge] {displayName} module initialization timeout");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AdBridge] Module for '{networkId}' not registered");
                }
            }

            isInitialized = true;
            Debug.Log("[AdBridge] Initialization completed");
        }

        /// <summary>
        /// Đợi cho module được khởi tạo với timeout
        /// </summary>
        private async Task<bool> WaitForModuleInitialized(string networkId, float timeoutSeconds)
        {
            float elapsed = 0f;
            float checkInterval = 0.1f;

            while (!adModules[networkId].IsInitialized && elapsed < timeoutSeconds)
            {
                await Task.Delay((int)(checkInterval * 1000));
                elapsed += checkInterval;
            }

            return adModules[networkId].IsInitialized;
        }

        /// <summary>
        /// Load tất cả ad units cho một network cụ thể
        /// </summary>
        private void LoadAdUnitsForNetwork(string networkId)
        {
            var adUnits = config.adUnits
                .Where(ad => ad.networkId == networkId && ad.isActive && ad.IsValid())
                .ToList();

            var networkDef = AdRegistry.GetNetwork(networkId);
            string displayName = networkDef?.displayName ?? networkId;
            Debug.Log($"[AdBridge] Loading {adUnits.Count} ad units for {displayName}");

            foreach (var adUnit in adUnits)
            {
                if (adModules.ContainsKey(networkId))
                {
                    adModules[networkId].LoadAdUnit(adUnit);
                }
            }
        }

        /// <summary>
        /// Load một ad unit cụ thể theo format và network
        /// </summary>
        /// <param name="formatId">Format ID (e.g., "banner", "interstitial", "rewarded")</param>
        /// <param name="networkId">Network ID (e.g., "admob", "applovin")</param>
        /// <returns>True nếu load thành công, false nếu không tìm thấy hoặc lỗi</returns>
        public bool LoadSpecificAdUnit(string formatId, string networkId)
        {
            if (!isInitialized)
            {
                Debug.LogError("[AdBridge] Not initialized yet");
                return false;
            }

            // Tìm ad unit phù hợp
            var adUnit = config.adUnits
                .FirstOrDefault(ad => ad.formatId == formatId &&
                                     ad.networkId == networkId &&
                                     ad.isActive &&
                                     ad.IsValid());

            if (adUnit == null)
            {
                var formatDef = AdRegistry.GetFormat(formatId);
                var networkDef = AdRegistry.GetNetwork(networkId);
                string formatName = formatDef?.displayName ?? formatId;
                string networkName = networkDef?.displayName ?? networkId;
                Debug.LogWarning($"[AdBridge] Ad unit not found: {formatName} on {networkName}");
                return false;
            }

            // Kiểm tra module có tồn tại không
            if (!adModules.ContainsKey(networkId))
            {
                var networkDef = AdRegistry.GetNetwork(networkId);
                string displayName = networkDef?.displayName ?? networkId;
                Debug.LogWarning($"[AdBridge] Module for '{displayName}' not found");
                return false;
            }

            var module = adModules[networkId];
            if (!module.IsInitialized)
            {
                var networkDef = AdRegistry.GetNetwork(networkId);
                string displayName = networkDef?.displayName ?? networkId;
                Debug.LogWarning($"[AdBridge] {displayName} module not initialized yet");
                return false;
            }

            // Load ad unit
            Debug.Log($"[AdBridge] Loading ad unit: {adUnit.name}");
            module.LoadAdUnit(adUnit);
            return true;
        }

        /// <summary>
        /// Load một ad unit cụ thể theo tên
        /// </summary>
        /// <param name="adUnitName">Tên của ad unit trong config</param>
        /// <returns>True nếu load thành công, false nếu không tìm thấy hoặc lỗi</returns>
        public bool LoadAdUnitByName(string adUnitName)
        {
            if (!isInitialized)
            {
                Debug.LogError("[AdBridge] Not initialized yet");
                return false;
            }

            // Tìm ad unit theo tên
            var adUnit = config.adUnits
                .FirstOrDefault(ad => ad.name == adUnitName && ad.isActive && ad.IsValid());

            if (adUnit == null)
            {
                Debug.LogWarning($"[AdBridge] Ad unit not found: {adUnitName}");
                return false;
            }

            return LoadSpecificAdUnit(adUnit.formatId, adUnit.networkId);
        }

        /// <summary>
        /// Load một ad unit cụ thể theo format và network (type-safe overload)
        /// </summary>
        /// <param name="format">Ad format definition</param>
        /// <param name="network">Ad network definition</param>
        public bool LoadSpecificAdUnit(AdFormatDefinition format, AdNetworkDefinition network)
        {
            if (format == null || network == null)
            {
                Debug.LogError("[AdBridge] Format or network is null");
                return false;
            }
            return LoadSpecificAdUnit(format.id, network.id);
        }

        /// <summary>
        /// Lấy module của một network cụ thể
        /// </summary>
        public IAdNetworkModule GetModule(string networkId)
        {
            if (adModules != null && adModules.ContainsKey(networkId))
            {
                return adModules[networkId];
            }

            Debug.LogWarning($"[AdBridge] Module for '{networkId}' not found");
            return null;
        }

        /// <summary>
        /// Hiển thị quảng cáo theo format (tự động chọn network theo priority)
        /// </summary>
        public void ShowAd(string formatId)
        {
            if (!isInitialized)
            {
                Debug.LogError("[AdBridge] Not initialized");
                return;
            }

            // Lấy danh sách ad units theo format, sắp xếp theo priority
            var adUnits = config.GetActiveAdUnits(formatId);

            if (adUnits.Count == 0)
            {
                var formatDef = AdRegistry.GetFormat(formatId);
                string formatName = formatDef?.displayName ?? formatId;
                Debug.LogWarning($"[AdBridge] No active ad units found for {formatName}");
                return;
            }

            // Thử show ad từ network có priority cao nhất
            foreach (var adUnit in adUnits)
            {
                var module = GetModule(adUnit.networkId);
                if (module != null && module.IsInitialized)
                {
                    if (module.IsAdReady(formatId))
                    {
                        module.ShowAd(formatId);
                        return;
                    }
                }
            }

            var format = AdRegistry.GetFormat(formatId);
            string name = format?.displayName ?? formatId;
            Debug.LogWarning($"[AdBridge] No ready ads found for {name}");
        }

        /// <summary>
        /// Hiển thị quảng cáo theo format (type-safe overload)
        /// </summary>
        /// <param name="format">Ad format definition (e.g., AdFormats.Rewarded)</param>
        public void ShowAd(AdFormatDefinition format)
        {
            if (format == null)
            {
                Debug.LogError("[AdBridge] Format is null");
                return;
            }
            ShowAd(format.id);
        }

        /// <summary>
        /// Kiểm tra xem quảng cáo có sẵn sàng không
        /// </summary>
        public bool IsAdReady(string formatId, string specificNetworkId = null)
        {
            if (!isInitialized) return false;

            if (!string.IsNullOrEmpty(specificNetworkId))
            {
                var module = GetModule(specificNetworkId);
                return module != null && module.IsAdReady(formatId);
            }

            // Kiểm tra tất cả networks
            var adUnits = config.GetActiveAdUnits(formatId);
            foreach (var adUnit in adUnits)
            {
                var module = GetModule(adUnit.networkId);
                if (module != null && module.IsAdReady(formatId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem quảng cáo có sẵn sàng không (type-safe overload)
        /// </summary>
        /// <param name="format">Ad format definition</param>
        /// <param name="network">Optional: specific network definition</param>
        public bool IsAdReady(AdFormatDefinition format, AdNetworkDefinition network = null)
        {
            if (format == null) return false;
            return IsAdReady(format.id, network?.id);
        }

        /// <summary>
        /// Reload ad units (dùng khi config thay đổi)
        /// </summary>
        public void ReloadAdUnits()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdBridge] Not initialized yet");
                return;
            }

            Debug.Log("[AdBridge] Reloading ad units...");
            config = ThirdLibConfig.Instance;
            _ = InitializeRequiredModulesAsync();
        }

        /// <summary>
        /// Ẩn banner (chỉ áp dụng cho banner ads)
        /// </summary>
        public void HideBanner()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdBridge] Not initialized yet");
                return;
            }

            // Tìm network đang hiển thị banner
            var bannerUnits = config.GetActiveAdUnits("banner");
            if (bannerUnits.Count > 0)
            {
                var networkId = bannerUnits[0].networkId;
                if (adModules.ContainsKey(networkId))
                {
                    adModules[networkId].HideBanner();
                }
            }
            else
            {
                Debug.LogWarning("[AdBridge] No active banner ad units found");
            }
        }

        /// <summary>
        /// Hiện banner (chỉ áp dụng cho banner ads)
        /// </summary>
        public void ShowBanner()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdBridge] Not initialized yet");
                return;
            }

            // Tìm network đang hiển thị banner
            var bannerUnits = config.GetActiveAdUnits("banner");
            if (bannerUnits.Count > 0)
            {
                var networkId = bannerUnits[0].networkId;
                if (adModules.ContainsKey(networkId))
                {
                    adModules[networkId].ShowBanner();
                }
            }
            else
            {
                Debug.LogWarning("[AdBridge] No active banner ad units found");
            }
        }

        /// <summary>
        /// Ẩn MREC (Medium Rectangle 300x250)
        /// </summary>
        public void HideMrec()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdBridge] Not initialized yet");
                return;
            }

            // Tìm network đang hiển thị MREC
            var mrecUnits = config.GetActiveAdUnits("mrec");
            if (mrecUnits.Count > 0)
            {
                var networkId = mrecUnits[0].networkId;
                if (adModules.ContainsKey(networkId))
                {
                    adModules[networkId].HideMrec();
                }
            }
            else
            {
                Debug.LogWarning("[AdBridge] No active MREC ad units found");
            }
        }

        /// <summary>
        /// Hiện MREC (Medium Rectangle 300x250)
        /// </summary>
        public void ShowMrec()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[AdBridge] Not initialized yet");
                return;
            }

            // Tìm network đang hiển thị MREC
            var mrecUnits = config.GetActiveAdUnits("mrec");
            if (mrecUnits.Count > 0)
            {
                var networkId = mrecUnits[0].networkId;
                if (adModules.ContainsKey(networkId))
                {
                    adModules[networkId].ShowMrec();
                }
            }
            else
            {
                Debug.LogWarning("[AdBridge] No active MREC ad units found");
            }
        }

        /// <summary>
        /// Lấy thông tin debug
        /// </summary>
        public string GetDebugInfo()
        {
            if (!isInitialized) return "AdBridge not initialized";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== AdBridge Debug Info ===");
            sb.AppendLine($"Initialized: {isInitialized}");
            sb.AppendLine($"Total Modules: {adModules.Count}");
            sb.AppendLine($"Total Ad Units: {config.adUnits.Count}");
            sb.AppendLine("\nRegistered Modules:");

            foreach (var module in adModules)
            {
                var networkDef = AdRegistry.GetNetwork(module.Key);
                string displayName = networkDef?.displayName ?? module.Key;
                string status = module.Value.IsInitialized ? "✓ Initialized" : "✗ Not Initialized";
                sb.AppendLine($"  - {displayName} ({module.Key}): {status}");
            }

            sb.AppendLine("\nActive Ad Units by Format:");
            var formats = AdRegistry.GetAllFormats();
            foreach (var format in formats)
            {
                var count = config.GetActiveAdUnits(format.id).Count;
                if (count > 0)
                {
                    sb.AppendLine($"  - {format.displayName} ({format.id}): {count} units");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Auto-initialize khi game start
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Tự động tạo instance khi game start
            var instance = Instance;
            Debug.Log("[AdBridge] Auto-initialized on game start");
        }
    }
}
