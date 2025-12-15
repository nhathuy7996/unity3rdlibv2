# 3rdLib - Unity Ad Management System

## 📋 Tổng quan

**3rdLib** là hệ thống quản lý quảng cáo modular cho Unity với kiến trúc DLL extensible, tích hợp Firebase Analytics tự động.

### Tính năng chính

✅ **Modular Ad Network System** - Hỗ trợ nhiều ad networks (AdMob, AppLovin, IronSource, Unity Ads)  
✅ **Dynamic Ad Format Registration** - String-based IDs, không dùng enum để dễ extend  
✅ **Waterfall Mediation** - Priority-based ad serving  
✅ **Auto-initialization** - Singleton pattern tự động khởi tạo khi game start  
✅ **Unity Editor Integration** - Visual config window với 2 tabs (SDK Config, Ad Units)  
✅ **Event-Driven Architecture** - 9 loại ad events (load, show, impression, revenue, etc.)  
✅ **Firebase Analytics Integration** - Tự động log ad events lên Firebase  
✅ **DLL-Compatible** - Core có thể build thành DLL, extensions ở ngoài  

---

## 🏗️ Kiến trúc hệ thống

### 1. Core Architecture

```
┌────────────────────────────────────────────────────────────┐
│                    Unity Editor Window                     │
│  ┌────────────────────────────────────────────────────┐    │
│  │  ThirdLibWindow (Menu: 3rdLib > Open Window)       │    │
│  │  • Tab 1: SDK Config (App Info + SDK Keys)         │    │
│  │  • Tab 2: Ad Units (CRUD management)               │    │
│  └────────────────────────────────────────────────────┘    │
│                           │                                │
│                           ▼                                │
│  ┌────────────────────────────────────────────────────┐    │
│  │  ThirdLibConfig (ScriptableObject)                 │    │
│  │  • Resources/ThirdLibConfig.asset                  │    │
│  │  • App info + SDK keys + Ad units list             │    │
│  └────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────┐
│                      Runtime System                        │
│  ┌────────────────────────────────────────────────────┐    │
│  │  AdRegistry (Dynamic Registration)                 │    │
│  │  • Built-in: banner, interstitial, rewarded, etc.  │    │
│  │  • Custom: RegisterFormat(), RegisterNetwork()     │    │
│  └────────────────────────────────────────────────────┘    │
│                           │                                │
│                           ▼                                │
│  ┌────────────────────────────────────────────────────┐    │
│  │  AdBridge (Singleton)                              │    │
│  │  • Dictionary<networkId, IAdNetworkModule>         │    │
│  │  • ShowAd(formatId), IsAdReady(), etc.             │    │
│  │  • Waterfall mediation logic                       │    │
│  └────────────────────────────────────────────────────┘    │
│                           │                                │
│                           ▼                                │
│  ┌────────────────────────────────────────────────────┐    │
│  │  Ad Network Modules (IAdNetworkModule)             │    │
│  │  • AdMobModule                                     │    │
│  │  • AppLovinModule                                  │    │
│  │  • IronSourceModule                                │    │
│  │  • UnityAdsModule                                  │    │
│  └────────────────────────────────────────────────────┘    │
│                           │                                │
│                           ▼                                │
│  ┌────────────────────────────────────────────────────┐    │
│  │  AdEvents (Event System)                           │    │
│  │  • 9 event types with AdEventArgs                  │    │
│  │  • Static events: OnAdImpression, OnAdPaid, etc.   │    │
│  └────────────────────────────────────────────────────┘    │
│                           │                                │
│                           ▼                                │
│  ┌────────────────────────────────────────────────────┐    │
│  │  FirebaseBridge (Singleton)                        │    │
│  │  • Auto-subscribe to AdEvents                      │    │
│  │  • Log to Firebase Analytics                       │    │
│  │  • Focus: ad_impression, ad_revenue                │    │
│  └────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────┘
```

### 2. Module Flow

```
User Code
   │
   └─> AdBridge.Instance.ShowAd("interstitial")
          │
          └─> Get active ad units for "interstitial" (priority sorted)
                │
                └─> Loop through networks (waterfall)
                       │
                       ├─> Check IsAdReady("interstitial", "admob")
                       │      │
                       │      └─> AdMobModule.IsAdReady()
                       │             │
                       │             └─> Return true/false
                       │
                       ├─> If ready: AdMobModule.ShowAd("interstitial")
                       │      │
                       │      └─> Trigger AdEvents:
                       │             • AdEvents.Trigger(AdShown)
                       │             • AdEvents.Trigger(AdImpression) ⭐
                       │             • AdEvents.TriggerPaid(revenue, currency)
                       │             • AdEvents.Trigger(AdClosed)
                       │
                       └─> FirebaseBridge auto-handles events
                              │
                              └─> Log to Firebase Analytics:
                                    • ad_show
                                    • ad_impression ⭐
                                    • ad_revenue
                                    • ad_close
```

### 3. Data Flow

```
Editor Window
    │
    └─> Save to ThirdLibConfig.asset (Resources/)
           │
           └─> Runtime: ThirdLibConfig.Instance
                  │
                  ├─> AdBridge.Initialize()
                  │      │
                  │      └─> Register modules
                  │             │
                  │             ├─> AdMobModule
                  │             ├─> AppLovinModule
                  │             ├─> IronSourceModule
                  │             └─> UnityAdsModule
                  │
                  └─> GetActiveAdUnits(formatId)
                         │
                         └─> Return List<AdUnit> (priority sorted)
```

---

## 📦 Cấu trúc dự án

### Folder Structure

```
Assets/
├── HuynnSDK/                      # SDK Runtime Components
│   ├── Core/                      # ⚡ Core System (trong DLL)
│   │   ├── AdUnit.cs              # Data model cho ad units
│   │   ├── AdEvents.cs            # Event system (9 event types)
│   │   ├── AdDefinitions.cs       # Format & Network definitions
│   │   └── AdRegistry.cs          # Dynamic registration system
│   │
│   ├── AdModule/                  # 🔌 Ad Network Modules
│   │   ├── IAdNetworkModule.cs    # ⚡ Interface (trong DLL)
│   │   ├── BaseAdNetworkModule.cs # ⚡ Base class (trong DLL)
│   └── AdEvents.cs                # Event system (9 events)
│
├── AdModule/
│   ├── IAdNetworkModule.cs        # Interface
│   │   ├── AdMobModule.cs         # 📱 Google AdMob (#if ADMOB)
│   │   ├── AppLovinModule.cs      # 📱 AppLovin MAX (#if APPLOVIN)
│   │   ├── IronSourceModule.cs    # 📱 IronSource (#if IRONSOURCE)
│   │   └── UnityAdsModule.cs      # 📱 Unity Ads (#if UNITY_ADS)
│   │
│   ├── ThirdLibConfig.cs          # ⚡ ScriptableObject config (trong DLL)
│   ├── AdBridge.cs                # ⚡ Singleton manager (trong DLL)
│   ├── FirebaseBridge.cs          # 📱 Firebase Analytics (#if FIREBASE)
│   │
│   ├── Extensions/                # 🔧 Optional Extensions
│   │   ├── CustomAdExtensions.cs  # Custom network example
│   │   └── FirebaseAnalyticsLogger.cs
│   │
│   └── *Example.cs                # 📘 Usage Examples
│       ├── AdBridgeExample.cs
│       ├── ThirdLibConfigExample.cs
│       ├── AdEventsExample.cs
│       ├── FirebaseBridgeExample.cs
│       └── TestSceneController.cs
│
├── Editor/                        # ⚡ Editor Tools (trong DLL)
│   ├── ThirdLibWindow.cs          # Editor window UI
│   └── AdEditorHelper.cs          # Helper utilities
│
├── Plugins/                       # 🔒 Compiled DLLs
│   ├── HuynnSDK.Runtime.dll       # Core runtime components
│   └── Editor/
│       └── HuynnSDK.Editor.dll    # Editor components
│
└── Resources/
    └── ThirdLibConfig.asset       # Runtime config asset

BuildDLL/                          # 🛠️ DLL Build System
├── HuynnSDK.Runtime.csproj        # Runtime DLL project
├── HuynnSDK.Editor.csproj         # Editor DLL project
└── build.sh                       # Build automation script
```

### Ký hiệu
- ⚡ **Trong DLL**: Components đã được compile
- 📱 **Source Code**: Có conditional compilation, Unity tự compile
- 🔧 **Extensions**: Optional, có thể thêm hoặc bỏ
- 📘 **Examples**: Demo code, reference implementation

---

## 🚀 Hướng dẫn implement

### 1. Setup trong Unity Editor

#### Bước 1: Mở ThirdLib Window

```
Menu: 3rdLib > Open Window
```

#### Bước 2: Cấu hình SDK (Tab: SDK Config)

```
App Information:
├── Android Package Name: com.yourcompany.yourgame
├── App Version: 1.0.0
└── Version Code: 1

SDK Keys:
├── Google AdMob
│   ├── Android App ID: ca-app-pub-xxxxxxxx~xxxxxxxxxx
│   └── iOS App ID: ca-app-pub-xxxxxxxx~xxxxxxxxxx
│
├── Facebook
│   ├── App ID: xxxxxxxxxxxx
│   └── Client Token: xxxxxxxxxxxxxxxxxxxxxxxx
│
├── AppLovin SDK Key: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
└── IronSource App Key: xxxxxxxxx
```

#### Bước 3: Tạo Ad Units (Tab: Ad Units)

```
Ad Units Management:
├── Add Ad Unit (+)
│   ├── Select Format: Interstitial
│   └── Select Network: AdMob
│
└── Configure:
    ├── Active: ✓
    ├── Priority: 1 (1 = highest)
    ├── Android Ad Unit ID: ca-app-pub-xxx/xxxxxxxxxx
    └── iOS Ad Unit ID: ca-app-pub-xxx/xxxxxxxxxx
```

### 2. Runtime Usage (Code)

#### ⚡ Type-Safe vs String-Based

**3rdLib hỗ trợ 2 cách gọi API:**

```csharp
// ✅ RECOMMENDED: Type-safe với AdFormats constants
AdBridge.Instance.ShowAd(AdFormats.Rewarded);
// ✓ IntelliSense support
// ✓ Compile-time check
// ✓ No typo errors

// ⚠️ LEGACY: String-based (vẫn hoạt động nhưng không khuyến khích)
AdBridge.Instance.ShowAd("rewarded");
// ✗ No IntelliSense
// ✗ Runtime error if typo
// ✗ "rewarddd" compiles but fails at runtime
```

**Built-in Constants:**
```csharp
using GameDevToi.ThirdLib.Core;

// Ad Formats
AdFormats.Banner          // "banner"
AdFormats.Interstitial    // "interstitial"
AdFormats.Rewarded        // "rewarded"
AdFormats.AppOpen         // "appopen"
AdFormats.RewardedInterstitial  // "rewarded_interstitial"
AdFormats.Native          // "native"

// Ad Networks
AdNetworks.AdMob          // "admob"
AdNetworks.AppLovin       // "applovin"
AdNetworks.IronSource     // "ironsource"
AdNetworks.UnityAds       // "unityads"

// Custom (after generating via Custom Definitions tab)
CustomAdFormats.YourCustomFormat
CustomAdNetworks.YourCustomNetwork
```

#### Basic - Show Ads

```csharp
using UnityEngine;
using GameDevToi.ThirdLib;
using GameDevToi.ThirdLib.Core; // ⭐ Import for AdFormats

public class GameManager : MonoBehaviour
{
    // ✅ RECOMMENDED: Type-safe approach
    public void ShowBanner()
    {
        AdBridge.Instance.ShowAd(AdFormats.Banner);
    }

    public void HideBanner()
    {
        AdBridge.Instance.HideBanner();
    }

    public void ShowInterstitial()
    {
        if (AdBridge.Instance.IsAdReady(AdFormats.Interstitial))
        {
            AdBridge.Instance.ShowAd(AdFormats.Interstitial);
        }
    }

    public void ShowRewarded()
    {
        AdBridge.Instance.ShowAd(AdFormats.Rewarded);
    }

    public void ShowAppOpen()
    {
        AdBridge.Instance.ShowAd(AdFormats.AppOpen);
    }

    // ⚠️ LEGACY: String-based (not recommended, error-prone)
    public void ShowRewardedLegacy()
    {
        AdBridge.Instance.ShowAd("rewarded"); // ❌ Typo risk!
    }
}
```

#### Advanced - Custom Logic

```csharp
using UnityEngine;
using GameDevToi.ThirdLib;
using GameDevToi.ThirdLib.Core;
using GameDevToi.ThirdLib.AdModule;

public class AdManager : MonoBehaviour
{
    void Start()
    {
        // Check if AdBridge is ready
        if (AdBridge.Instance != null)
        {
            Debug.Log("AdBridge initialized!");
            Debug.Log(AdBridge.Instance.GetDebugInfo());
        }
    }

    // ✅ Type-safe approach
    public bool IsInterstitialReady()
    {
        return AdBridge.Instance.IsAdReady(AdFormats.Interstitial);
    }

    // Check specific network (type-safe)
    public bool IsAdMobInterstitialReady()
    {
        return AdBridge.Instance.IsAdReady(AdFormats.Interstitial, AdNetworks.AdMob);
    }

    // Get specific module
    public void ConfigureAdMob()
    {
        var adMobModule = AdBridge.Instance.GetModule(AdNetworks.AdMob.id);
        if (adMobModule != null)
        {
            Debug.Log($"AdMob initialized: {adMobModule.IsInitialized}");
        }
    }

    // Load specific ad unit (type-safe)
    public void LoadSpecificAd()
    {
        AdBridge.Instance.LoadSpecificAdUnit(AdFormats.Interstitial, AdNetworks.AdMob);
    }

    // Reload ad units (if config changed)
    public void ReloadAds()
    {
        AdBridge.Instance.ReloadAdUnits();
    }
}
```

### 3. Event Handling

#### Option 1: Automatic Firebase Logging (Zero Code)

```csharp
// FirebaseBridge tự động log tất cả ad events!
// Không cần code gì, chỉ cần show ads:

AdBridge.Instance.ShowAd("interstitial");
// --> FirebaseBridge tự động log:
//     • ad_show
//     • ad_impression ⭐
//     • ad_revenue (nếu có)
//     • ad_close
```

#### Option 2: Custom Event Handling

```csharp
using GameDevToi.ThirdLib.Core;

public class MyAdEventHandler : MonoBehaviour
{
    void OnEnable()
    {
        // Subscribe to events
        AdEvents.OnAdImpression += OnAdImpression;
        AdEvents.OnAdPaid += OnAdPaid;
        AdEvents.OnAdRewarded += OnAdRewarded;
        AdEvents.OnAdClosed += OnAdClosed;
    }

    void OnDisable()
    {
        // Unsubscribe
        AdEvents.OnAdImpression -= OnAdImpression;
        AdEvents.OnAdPaid -= OnAdPaid;
        AdEvents.OnAdRewarded -= OnAdRewarded;
        AdEvents.OnAdClosed -= OnAdClosed;
    }

    void OnAdImpression(AdEventArgs e)
    {
        Debug.Log($"👁 Impression: {e.FormatId} from {e.NetworkId}");
        // Track with your analytics
    }

    void OnAdPaid(AdEventArgs e)
    {
        Debug.Log($"💰 Revenue: {e.Revenue} {e.CurrencyCode}");
        // Send to Adjust, AppsFlyer, etc.
    }

    void OnAdRewarded(AdEventArgs e)
    {
        // Grant reward to player
        int coins = (int)e.RewardAmount;
        PlayerData.AddCoins(coins);
        UIManager.ShowNotification($"+{coins} coins!");
    }

    void OnAdClosed(AdEventArgs e)
    {
        // Resume game
        Time.timeScale = 1;
        AudioListener.volume = 1;
    }
}
```

### 4. Firebase Analytics Integration

```csharp
using GameDevToi.ThirdLib;

public class FirebaseSetup : MonoBehaviour
{
    void Start()
    {
        // FirebaseBridge tự động khởi tạo và log ad events
        // Optional: Set user properties
        
        FirebaseBridge.Instance.SetUserId(GetUserId());
        FirebaseBridge.Instance.SetUserProperty("user_level", "5");
        FirebaseBridge.Instance.SetUserProperty("vip_status", "premium");
    }

    // Log custom game events
    public void OnLevelComplete(int level, int score)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "level", level },
            { "score", score },
            { "time", Time.timeSinceLevelLoad }
        };
        
        FirebaseBridge.Instance.LogCustomEvent("level_complete", parameters);
    }
}
```

---

## 🎯 Tính năng chi tiết

### 1. Ad Formats (Built-in)

| Format ID | Display Name | Description |
|-----------|-------------|-------------|
| `banner` | Banner | Small banner at top/bottom |
| `interstitial` | Interstitial | Full-screen ad |
| `rewarded` | Rewarded | Reward-based video ad |
| `appopen` | App Open | App launch ad |
| `rewarded_interstitial` | Rewarded Interstitial | Full-screen rewarded |
| `native` | Native | Custom native ad |

### 2. Ad Networks (Built-in)

| Network ID | Display Name | Module |
|-----------|-------------|---------|
| `admob` | Google AdMob | AdMobModule (Full) |
| `applovin` | AppLovin MAX | AppLovinModule |
| `ironsource` | IronSource | IronSourceModule |
| `unityads` | Unity Ads | UnityAdsModule |

### 3. Ad Events System

| Event | Firebase Event | Trigger Time | Use Case |
|-------|---------------|--------------|----------|
| `AdLoadStarted` | `ad_load_started` | Bắt đầu load | Track load time |
| `AdLoadSuccess` | `ad_load_success` | Load thành công | Success rate |
| `AdLoadFailed` | `ad_load_failed` | Load thất bại | Error tracking |
| `AdShown` | `ad_show` | Ad hiển thị | Show rate |
| `AdClicked` | `ad_click` | User click | CTR calculation |
| `AdClosed` | `ad_close` | Ad đóng | Completion rate |
| **`AdImpression`** ⭐ | **`ad_impression`** | **Impression ghi nhận** | **Main metric** |
| `AdPaid` | `ad_revenue` | Revenue tracked | Monetization |
| `AdRewarded` | `ad_rewarded` | Reward granted | Reward tracking |

### 4. Waterfall Mediation

```csharp
// Ad units được load theo priority (1 = cao nhất)

Ad Units cho "interstitial":
1. Priority 1: AdMob (ca-app-pub-xxx/111111)
2. Priority 2: AppLovin (ad-unit-222)
3. Priority 3: IronSource (ad-unit-333)

// AdBridge tự động thử từ priority cao xuống thấp
AdBridge.Instance.ShowAd("interstitial");
// --> Try AdMob first
// --> If not ready, try AppLovin
// --> If not ready, try IronSource
```

### 5. Firebase Events Generated

```json
{
  "event_name": "ad_impression",
  "parameters": {
    "ad_format": "interstitial",
    "ad_network": "admob",
    "ad_unit_id": "ca-app-pub-xxx/xxxxxxxxxx",
    "ad_platform": "android"
  }
}

{
  "event_name": "ad_revenue",
  "parameters": {
    "ad_format": "interstitial",
    "ad_network": "admob",
    "ad_unit_id": "ca-app-pub-xxx/xxxxxxxxxx",
    "ad_platform": "android",
    "value": 0.05,
    "currency": "USD",
    "revenue": 0.05
  }
}
```

---

## 🔧 Advanced: Custom Extensions

### Thêm custom ad network (Vungle example)

```csharp
using GameDevToi.ThirdLib.Core;
using GameDevToi.ThirdLib.AdModule;
using UnityEngine;

namespace GameDevToi.ThirdLib.Extensions
{
    // Step 1: Define network
    public static class CustomNetworks
    {
        public static AdNetworkDefinition Vungle = new AdNetworkDefinition
        {
            id = "vungle",
            displayName = "Vungle",
            color = new Color(0.2f, 0.6f, 1f)
        };
    }

    // Step 2: Create module
    public class VungleModule : BaseAdNetworkModule
    {
        public override string NetworkId => "vungle";

        public override void Initialize(ThirdLibConfig config)
        {
            base.Initialize(config);
            // Initialize Vungle SDK
            LogInfo("Vungle initialized");
        }

        public override void LoadAdUnit(AdUnit adUnit)
        {
            LogInfo($"Loading Vungle ad: {adUnit.GetAdUnitId()}");
            // Load Vungle ad
        }

        public override void ShowAd(string formatId, string placementId = null)
        {
            LogInfo($"Showing Vungle ad: {formatId}");
            // Show Vungle ad
        }

        public override bool IsAdReady(string formatId, string placementId = null)
        {
            return false; // Check Vungle ad ready
        }
    }

    // Step 3: Register extension
    public static class VungleExtension
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            // Register network
            AdRegistry.RegisterNetwork(CustomNetworks.Vungle);

            // Register module
            AdBridge.Instance.RegisterAdModule(new VungleModule());

            Debug.Log("[Vungle] Extension registered!");
        }
    }
}
```

### Thêm custom ad format

```csharp
// Define custom format
public static class CustomFormats
{
    public static AdFormatDefinition Playable = new AdFormatDefinition
    {
        id = "playable",
        displayName = "Playable",
        description = "Interactive playable ad"
    };
}

// Register
[RuntimeInitializeOnLoadMethod]
static void RegisterCustomFormat()
{
    AdRegistry.RegisterFormat(CustomFormats.Playable);
}

// Use
AdBridge.Instance.ShowAd("playable");
```

---

## 📊 Firebase Analytics Dashboard

### Events to Monitor

```
Key Metrics:
├── ad_impression (Total impressions) ⭐⭐⭐⭐⭐
├── ad_revenue (Total revenue) ⭐⭐⭐⭐⭐
├── ad_show (Show rate)
├── ad_click (CTR calculation)
└── ad_load_failed (Error rate)

Segmentation:
├── By ad_format: banner | interstitial | rewarded
├── By ad_network: admob | applovin | ironsource
└── By ad_platform: android | ios | webgl
```

### Custom Reports Examples

```sql
-- Total impressions by format
SELECT ad_format, COUNT(*) as impressions
FROM ad_impression
GROUP BY ad_format
ORDER BY impressions DESC

-- Revenue by network
SELECT ad_network, SUM(value) as total_revenue
FROM ad_revenue
GROUP BY ad_network
ORDER BY total_revenue DESC

-- CTR (Click-Through Rate)
SELECT 
  (COUNT(ad_click) * 100.0 / COUNT(ad_impression)) as ctr
FROM events

-- Fill rate by network
SELECT 
  ad_network,
  (COUNT(ad_load_success) * 100.0 / COUNT(ad_load_started)) as fill_rate
FROM events
GROUP BY ad_network
```

---

## 📚 API Reference

### AdBridge

```csharp
// Properties
static AdBridge Instance { get; }

// Methods
void ShowAd(string formatId, string placementId = null)
bool IsAdReady(string formatId, string placementId = null)
IAdNetworkModule GetModule(string networkId)
void RegisterAdModule(IAdNetworkModule module)
void ReloadAdUnits()
string GetDebugInfo()
void HideBanner()
void ShowBanner()
```

### AdEvents

```csharp
// Events
static event AdEventHandler OnAdEvent;
static event AdEventHandler OnAdLoadStarted;
static event AdEventHandler OnAdLoadSuccess;
static event AdEventHandler OnAdLoadFailed;
static event AdEventHandler OnAdShown;
static event AdEventHandler OnAdClicked;
static event AdEventHandler OnAdClosed;
static event AdEventHandler OnAdImpression;
static event AdEventHandler OnAdPaid;
static event AdEventHandler OnAdRewarded;

// Methods
static void TriggerEvent(AdEventArgs eventArgs)
static void Trigger(AdEventType type, string formatId, string networkId, string adUnitId)
static void TriggerFailed(string formatId, string networkId, string adUnitId, string error, int code)
static void TriggerPaid(string formatId, string networkId, string adUnitId, double revenue, string currency)
static void TriggerRewarded(string formatId, string networkId, string adUnitId, string type, double amount)
```

### FirebaseBridge

```csharp
// Properties
static FirebaseBridge Instance { get; }
bool IsInitialized { get; }

// Methods
void LogCustomEvent(string eventName, Dictionary<string, object> parameters)
void LogCustomEvent(string eventName, string paramName, string paramValue)
void SetUserProperty(string propertyName, string propertyValue)
void SetUserId(string userId)
```

### AdRegistry

```csharp
// Methods
static void RegisterFormat(AdFormatDefinition format)
static void RegisterNetwork(AdNetworkDefinition network)
static List<AdFormatDefinition> GetAllFormats()
static List<AdNetworkDefinition> GetAllNetworks()
static AdFormatDefinition GetFormat(string id)
static AdNetworkDefinition GetNetwork(string id)
static bool HasFormat(string id)
static bool HasNetwork(string id)
```

### ThirdLibConfig

```csharp
// Properties
static ThirdLibConfig Instance { get; }
string androidPackageName { get; set; }
string appVersion { get; set; }
int versionCode { get; set; }
List<AdUnit> adUnits { get; set; }

// Methods
string GetGoogleAdMobAppId()
AdUnit GetAdUnit(string formatId, string networkId)
List<AdUnit> GetActiveAdUnits(string formatId)
```

---

## 📖 Implementation Examples

### Complete Game Integration

```csharp
using UnityEngine;
using GameDevToi.ThirdLib;
using GameDevToi.ThirdLib.Core;

public class GameController : MonoBehaviour
{
    void Start()
    {
        // Setup event handlers
        AdEvents.OnAdRewarded += OnRewardGranted;
        AdEvents.OnAdClosed += OnAdClosed;
    }

    void OnDestroy()
    {
        AdEvents.OnAdRewarded -= OnRewardGranted;
        AdEvents.OnAdClosed -= OnAdClosed;
    }

    public void OnGameStart()
    {
        // ✅ Type-safe
        AdBridge.Instance.ShowAd(AdFormats.Banner);
    }

    public void OnLevelComplete()
    {
        // ✅ Type-safe with check
        if (AdBridge.Instance.IsAdReady(AdFormats.Interstitial))
        {
            AdBridge.Instance.ShowAd(AdFormats.Interstitial);
        }
    }

    public void OnWatchAdForCoins()
    {
        // ✅ Type-safe
        if (AdBridge.Instance.IsAdReady(AdFormats.Rewarded))
        {
            AdBridge.Instance.ShowAd(AdFormats.Rewarded);
        }
    }

    void OnRewardGranted(AdEventArgs e)
    {
        int coins = (int)e.RewardAmount;
        PlayerData.AddCoins(coins);
    }

    void OnAdClosed(AdEventArgs e)
    {
        Time.timeScale = 1;
        AudioListener.volume = 1;
    }
}
```

---

## 🏗️ Kiến trúc tổng quan

### Hybrid DLL Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    HuynnSDK.Runtime.dll                 │
│  ┌────────────────────────────────────────────────┐     │
│  │  Core Components (SDK-Independent)             │     │
│  │  • AdUnit, AdEvents, AdDefinitions             │     │
│  │  • AdRegistry, ThirdLibConfig                  │     │
│  │  • AdBridge (Dynamic Module Loader)            │     │
│  │  • IAdNetworkModule, BaseAdNetworkModule       │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│              Ad Network Modules (Source Code)           │
│  ┌────────────────────────────────────────────────┐     │
│  │  #if ADMOB                                     │     │
│  │      AdMobModule : BaseAdNetworkModule         │     │
│  │  #endif                                        │     │
│  │                                                │     │
│  │  #if APPLOVIN                                  │     │
│  │      AppLovinModule : BaseAdNetworkModule      │     │
│  │  #endif                                        │     │
│  │                                                │     │
│  │  → Unity tự động compile khi SDK có sẵn       │     │
│  │  → AdBridge dùng reflection để load           │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    HuynnSDK.Editor.dll                  │
│  ┌────────────────────────────────────────────────┐     │
│  │  Editor Components                             │     │
│  │  • ThirdLibWindow (3rdLib Menu)                │     │
│  │  • AdEditorHelper                              │     │
│  └────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

### Data Flow

```
ThirdLibWindow (Editor)
    │
    └─> ThirdLibConfig.asset (Resources/)
           │
           └─> Runtime: AdBridge.Instance
                  │
                  ├─> Auto-detect modules via Reflection
                  │      │
                  │      ├─> AdMobModule (nếu có #if ADMOB)
                  │      ├─> AppLovinModule (nếu có #if APPLOVIN)
                  │      └─> ...
                  │
                  └─> ShowAd(formatId)
                         │
                         └─> Waterfall mediation → Show ad
                                │
                                └─> Trigger AdEvents
                                       │
                                       └─> FirebaseBridge auto-log
```

---

## 💡 Design Principles

1. **Core trong DLL** - Components ổn định, không phụ thuộc external SDKs
2. **Modules ở Source** - Flexible với conditional compilation
3. **Dynamic Loading** - Reflection-based module registration tự động
4. **Event-Driven** - Decoupled communication giữa components
5. **Zero Config** - Auto-initialization, tự động setup khi game start
- **Event-Driven**: Decouple ad logic khỏi game logic
- **Auto-Initialization**: Singleton pattern với RuntimeInitializeOnLoadMethod
- **Firebase Ready**: Tự động log ad events

### Key Features

✅ Unity Editor integration  
✅ Multi-network waterfall mediation  
✅ Event system với 9 event types  
✅ Firebase Analytics auto-logging  
✅ DLL-compatible architecture  
✅ Zero-code Firebase integration  
✅ Extensible design  

### Getting Started

1. Open `3rdLib > Open Window`
2. Configure SDK keys
3. Add ad units
4. Call `AdBridge.Instance.ShowAd("interstitial")`
5. Done! Firebase tự động log events

---

**Made with ❤️ for Unity Developers**
