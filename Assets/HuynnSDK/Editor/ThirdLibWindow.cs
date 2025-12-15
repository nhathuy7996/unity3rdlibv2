using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GameDevToi.ThirdLib.Core;
using GameDevToi.ThirdLib.Editor;

namespace GameDevToi.ThirdLib
{
    public class ThirdLibWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private ThirdLibConfig config;
        private SerializedObject serializedConfig;
        private const string CONFIG_PATH = "Assets/Resources/ThirdLibConfig.asset";
        private const string RESOURCES_FOLDER = "Assets/Resources";

        // Tab management
        private int selectedTab = 0;
        private readonly string[] tabs = { "SDK Config", "Ad Units", "Custom Definitions", "Download Modules" };

        // Custom Definitions management
        private CustomAdDefinitions customDefinitions;
        private SerializedObject serializedCustomDefinitions;
        private const string CUSTOM_DEFINITIONS_PATH = "Assets/Resources/CustomAdDefinitions.asset";

        // Ad Units management
        private Dictionary<string, bool> adFormatFoldouts = new Dictionary<string, bool>();
        private Vector2 adUnitsScrollPosition;

        // Download modules management
        private Dictionary<string, bool> downloadingModules = new Dictionary<string, bool>();
        private Dictionary<string, string> downloadErrors = new Dictionary<string, string>();

        [MenuItem("3rdLib/Open Window")]
        public static void ShowWindow()
        {
            ThirdLibWindow window = GetWindow<ThirdLibWindow>("3rd Lib Manager");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
            LoadOrCreateCustomDefinitions();
            InitializeAdFormatFoldouts();
        }

        private void InitializeAdFormatFoldouts()
        {
            var formats = AdRegistry.GetAllFormats();
            foreach (var format in formats)
            {
                if (!adFormatFoldouts.ContainsKey(format.id))
                {
                    adFormatFoldouts[format.id] = true;
                }
            }
        }

        private void LoadOrCreateConfig()
        {
            // Tìm config trong Resources
            config = Resources.Load<ThirdLibConfig>("ThirdLibConfig");

            // Nếu chưa tồn tại, tạo mới
            if (config == null)
            {
                // Tạo thư mục Resources nếu chưa có
                if (!Directory.Exists(RESOURCES_FOLDER))
                {
                    Directory.CreateDirectory(RESOURCES_FOLDER);
                    AssetDatabase.Refresh();
                }

                // Tạo ScriptableObject mới
                config = CreateInstance<ThirdLibConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Created new ThirdLibConfig at {CONFIG_PATH}");
            }

            if (config != null)
            {
                serializedConfig = new SerializedObject(config);
            }
        }

        private void LoadOrCreateCustomDefinitions()
        {
            // Tìm custom definitions trong Resources
            customDefinitions = Resources.Load<CustomAdDefinitions>("CustomAdDefinitions");

            // Nếu chưa tồn tại, tạo mới
            if (customDefinitions == null)
            {
                // Tạo thư mục Resources nếu chưa có
                if (!Directory.Exists(RESOURCES_FOLDER))
                {
                    Directory.CreateDirectory(RESOURCES_FOLDER);
                    AssetDatabase.Refresh();
                }

                // Tạo ScriptableObject mới
                customDefinitions = CreateInstance<CustomAdDefinitions>();
                AssetDatabase.CreateAsset(customDefinitions, CUSTOM_DEFINITIONS_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Created new CustomAdDefinitions at {CUSTOM_DEFINITIONS_PATH}");
            }

            if (customDefinitions != null)
            {
                serializedCustomDefinitions = new SerializedObject(customDefinitions);
            }
        }

        private void OnGUI()
        {
            if (config == null || serializedConfig == null)
            {
                EditorGUILayout.HelpBox("Config file không tồn tại. Đang tải lại...", MessageType.Warning);
                if (GUILayout.Button("Reload Config"))
                {
                    LoadOrCreateConfig();
                }
                return;
            }

            serializedConfig.Update();

            GUILayout.Label("3rd Library Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(30));
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawSDKConfigTab();
                    break;
                case 1:
                    DrawAdUnitsTab();
                    break;
                case 2:
                    DrawCustomDefinitionsTab();
                    break;
                case 3:
                    DrawDownloadModulesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawBottomButtons();

            serializedConfig.ApplyModifiedProperties();
            if (serializedCustomDefinitions != null)
            {
                serializedCustomDefinitions.ApplyModifiedProperties();
            }
        }

        private void DrawSDKConfigTab()
        {
            EditorGUILayout.HelpBox("Cấu hình các ID cho các SDK bên thứ 3. Dữ liệu này sẽ được sử dụng trong Runtime.", MessageType.Info);
            EditorGUILayout.Space();

            // App Information Section
            DrawSection("App Information", () =>
            {
                SerializedProperty androidPackageName = serializedConfig.FindProperty("androidPackageName");
                SerializedProperty appVersion = serializedConfig.FindProperty("appVersion");
                SerializedProperty versionCode = serializedConfig.FindProperty("versionCode");

                EditorGUILayout.PropertyField(androidPackageName, new GUIContent("Android Package Name"));
                EditorGUILayout.PropertyField(appVersion, new GUIContent("App Version"));
                EditorGUILayout.PropertyField(versionCode, new GUIContent("Version Code"));
            });

            EditorGUILayout.Space();

            // Google Mobile Ads Section
            DrawSection("Google Mobile Ads", () =>
            {
                SerializedProperty androidAppId = serializedConfig.FindProperty("googleAdMobAndroidAppId");
                SerializedProperty iosAppId = serializedConfig.FindProperty("googleAdMobIOSAppId");

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(androidAppId, new GUIContent("Android App ID"));
                EditorGUILayout.PropertyField(iosAppId, new GUIContent("iOS App ID"));

                // Khi App ID thay đổi, cập nhật vào GoogleMobileAdsSettings
                if (EditorGUI.EndChangeCheck())
                {
                    serializedConfig.ApplyModifiedProperties();

                    // Cập nhật GoogleMobileAdsSettings (nếu SDK đã import)
                    try
                    {
                        // Tìm type trong tất cả assemblies
                        var googleMobileAdsSettingsType = System.AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == "GoogleMobileAdsSettings");

                        if (googleMobileAdsSettingsType != null)
                        {
                            // LoadInstance() là internal, cần NonPublic flag
                            var loadInstanceMethod = googleMobileAdsSettingsType.GetMethod("LoadInstance",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (loadInstanceMethod != null)
                            {
                                var googleMobileAdsSettings = loadInstanceMethod.Invoke(null, null);
                                if (googleMobileAdsSettings != null)
                                {
                                    var androidAppIdProperty = googleMobileAdsSettingsType.GetProperty("GoogleMobileAdsAndroidAppId");
                                    var iosAppIdProperty = googleMobileAdsSettingsType.GetProperty("GoogleMobileAdsIOSAppId");

                                    if (androidAppIdProperty != null && iosAppIdProperty != null)
                                    {
                                        androidAppIdProperty.SetValue(googleMobileAdsSettings, config.googleAdMobAndroidAppId);
                                        iosAppIdProperty.SetValue(googleMobileAdsSettings, config.googleAdMobIOSAppId);

                                        EditorUtility.SetDirty((UnityEngine.Object)googleMobileAdsSettings);
                                        AssetDatabase.SaveAssets();
                                        Debug.Log($"[3rdLib] Updated Google Mobile Ads App IDs");
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[3rdLib] Could not update GoogleMobileAdsSettings: {ex.Message}");
                    }
                }
            });

            EditorGUILayout.Space();

            // Facebook Section
            DrawSection("Facebook", () =>
            {
                SerializedProperty facebookAppId = serializedConfig.FindProperty("facebookAppId");
                SerializedProperty facebookClientToken = serializedConfig.FindProperty("facebookClientToken");

                EditorGUILayout.PropertyField(facebookAppId, new GUIContent("App ID"));
                EditorGUILayout.PropertyField(facebookClientToken, new GUIContent("Client Token"));
            });

            EditorGUILayout.Space();

            // AppLovin Section
            DrawSection("AppLovin", () =>
            {
                SerializedProperty appLovinSdkKey = serializedConfig.FindProperty("appLovinSdkKey");

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(appLovinSdkKey, new GUIContent("SDK Key"));

                // Khi SDK Key thay đổi, cập nhật vào AppLovinSettings
                if (EditorGUI.EndChangeCheck())
                {
                    serializedConfig.ApplyModifiedProperties();

                    // Cập nhật AppLovinSettings ScriptableObject (nếu SDK đã import)
                    try
                    {
                        // Tìm type trong tất cả assemblies
                        var appLovinSettingsType = System.AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == "AppLovinSettings");

                        if (appLovinSettingsType != null)
                        {
                            var instanceProperty = appLovinSettingsType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (instanceProperty != null)
                            {
                                var appLovinSettings = instanceProperty.GetValue(null);
                                if (appLovinSettings != null)
                                {
                                    var sdkKeyProperty = appLovinSettingsType.GetProperty("SdkKey");
                                    if (sdkKeyProperty != null)
                                    {
                                        sdkKeyProperty.SetValue(appLovinSettings, config.appLovinSdkKey);

                                        var saveMethod = appLovinSettingsType.GetMethod("SaveAsync");
                                        if (saveMethod != null)
                                        {
                                            saveMethod.Invoke(appLovinSettings, null);
                                            Debug.Log($"[3rdLib] Updated AppLovin SDK Key: {config.appLovinSdkKey}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[3rdLib] Could not update AppLovinSettings: {ex.Message}");
                    }
                }
            });

            EditorGUILayout.Space();

            // IronSource Section
            DrawSection("IronSource", () =>
            {
                SerializedProperty ironSourceAppKey = serializedConfig.FindProperty("ironSourceAppKey");
                EditorGUILayout.PropertyField(ironSourceAppKey, new GUIContent("App Key"));
            });
        }

        private void DrawAdUnitsTab()
        {
            EditorGUILayout.HelpBox("Quản lý các đơn vị quảng cáo (Ad Units). Bạn có thể thêm, sửa, xóa các đơn vị quảng cáo theo format và network.", MessageType.Info);
            EditorGUILayout.Space();

            // Add new ad unit button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Thêm đơn vị quảng cáo", GUILayout.Height(35), GUILayout.Width(200)))
            {
                ShowAddAdUnitMenu();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (config.adUnits == null || config.adUnits.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có đơn vị quảng cáo nào. Nhấn nút 'Thêm đơn vị quảng cáo' để bắt đầu.", MessageType.Info);
                return;
            }

            // Group ad units by format
            var groupedAdUnits = config.adUnits
                .Where(ad => ad.IsValid())
                .GroupBy(ad => ad.formatId)
                .OrderBy(g => g.Key);

            foreach (var group in groupedAdUnits)
            {
                string formatId = group.Key;
                List<AdUnit> units = group.ToList();

                var formatDef = AdRegistry.GetFormat(formatId);
                string formatDisplayName = formatDef?.displayName ?? formatId;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Foldout header with count
                EditorGUILayout.BeginHorizontal();
                if (!adFormatFoldouts.ContainsKey(formatId))
                    adFormatFoldouts[formatId] = true;

                adFormatFoldouts[formatId] = EditorGUILayout.Foldout(
                    adFormatFoldouts[formatId],
                    $"{formatDisplayName} ({units.Count})",
                    true,
                    EditorStyles.foldoutHeader
                );
                EditorGUILayout.EndHorizontal();

                if (adFormatFoldouts[formatId])
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < units.Count; i++)
                    {
                        DrawAdUnit(units[i], i);

                        if (i < units.Count - 1)
                        {
                            EditorGUILayout.Space(5);
                            DrawSeparator();
                            EditorGUILayout.Space(5);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawAdUnit(AdUnit adUnit, int index)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Header with network icon and delete button
            EditorGUILayout.BeginHorizontal();

            adUnit.isActive = EditorGUILayout.Toggle(adUnit.isActive, GUILayout.Width(20));

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            if (!adUnit.isActive)
            {
                headerStyle.normal.textColor = Color.gray;
            }

            string networkDisplayName = AdEditorHelper.GetNetworkDisplayName(adUnit.networkId);
            EditorGUILayout.LabelField(networkDisplayName, headerStyle, GUILayout.Width(100));

            adUnit.name = EditorGUILayout.TextField(adUnit.name);

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Xóa Ad Unit",
                    $"Bạn có chắc muốn xóa '{adUnit.name}'?",
                    "Xóa", "Hủy"))
                {
                    config.adUnits.Remove(adUnit);
                    EditorUtility.SetDirty(config);
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Network dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Network:", GUILayout.Width(80));
            adUnit.networkId = AdEditorHelper.DrawNetworkDropdown(adUnit.networkId);
            EditorGUILayout.EndHorizontal();

            // Priority
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Priority:", GUILayout.Width(80));
            adUnit.priority = EditorGUILayout.IntField(adUnit.priority);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Android Ad Unit ID
            EditorGUILayout.LabelField("Android Ad Unit ID:", EditorStyles.boldLabel);
            adUnit.androidAdUnitId = EditorGUILayout.TextField(adUnit.androidAdUnitId);

            // iOS Ad Unit ID
            EditorGUILayout.LabelField("iOS Ad Unit ID:", EditorStyles.boldLabel);
            adUnit.iosAdUnitId = EditorGUILayout.TextField(adUnit.iosAdUnitId);

            // Notes
            if (!string.IsNullOrEmpty(adUnit.notes))
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Notes:", EditorStyles.miniLabel);
                adUnit.notes = EditorGUILayout.TextArea(adUnit.notes, GUILayout.Height(40));
            }
            else
            {
                if (GUILayout.Button("+ Add Notes", GUILayout.Height(20)))
                {
                    adUnit.notes = "";
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowAddAdUnitMenu()
        {
            AdEditorHelper.ShowAddAdUnitMenu((formatId, networkId) => AddNewAdUnit(formatId, networkId));
        }

        private void AddNewAdUnit(string formatId, string networkId)
        {
            if (config.adUnits == null)
            {
                config.adUnits = new List<AdUnit>();
            }

            AdUnit newAdUnit = new AdUnit(formatId, networkId);
            config.adUnits.Add(newAdUnit);

            EditorUtility.SetDirty(config);

            // Ensure foldout is open for the new ad unit's format
            if (!adFormatFoldouts.ContainsKey(formatId))
            {
                adFormatFoldouts[formatId] = true;
            }
            else
            {
                adFormatFoldouts[formatId] = true;
            }
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawBottomButtons()
        {
            // Save button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save Changes", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveConfig();
            }

            if (selectedTab == 0 && GUILayout.Button("Reset to Default", GUILayout.Width(150), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Config",
                    "Bạn có chắc muốn reset tất cả về giá trị mặc định?",
                    "Yes", "No"))
                {
                    ResetConfig();
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // File location info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Config file: {CONFIG_PATH}", EditorStyles.miniLabel);
            if (GUILayout.Button("Select in Project", GUILayout.Width(120)))
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private void SaveConfig()
        {
            if (serializedConfig != null)
            {
                serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("ThirdLibConfig saved successfully!");
                ShowNotification(new GUIContent("✓ Đã lưu thành công!"));
            }
        }

        private void ResetConfig()
        {
            if (config != null)
            {
                config.androidPackageName = "com.company.game";
                config.appVersion = "1.0.0";
                config.versionCode = 1;
                config.googleAdMobAndroidAppId = "ca-app-pub-xxxxxxxxxxxxxxxx~yyyyyyyyyy";
                config.googleAdMobIOSAppId = "ca-app-pub-xxxxxxxxxxxxxxxx~yyyyyyyyyy";
                config.facebookAppId = "";
                config.facebookClientToken = "";
                config.appLovinSdkKey = "";
                config.ironSourceAppKey = "";

                serializedConfig.Update();
                SaveConfig();
            }
        }

        #region Download Modules Tab

        private void DrawDownloadModulesTab()
        {
            EditorGUILayout.HelpBox(
                "Download các Ad Network Modules từ GitHub. Modules sẽ tự động được Unity compile khi SDK tương ứng đã được import.",
                MessageType.Info
            );
            EditorGUILayout.Space();

            var modules = GetAvailableModules();

            foreach (var module in modules)
            {
                DrawModuleCard(module);
                EditorGUILayout.Space();
            }
        }

        private List<ModuleInfo> GetAvailableModules()
        {
            return new List<ModuleInfo>
            {
                new ModuleInfo
                {
                    name = "Google AdMob",
                    fileName = "AdMobModule.cs",
                    downloadUrl = "https://raw.githubusercontent.com/nhathuy7996/unity3rdlibv2/develop/Assets/HuynnSDK/AdModule/AdMobModule.cs",
                    localPath = "Assets/HuynnSDK/AdModule/AdMobModule.cs",
                    description = "Google Mobile Ads SDK integration với support đầy đủ cho Banner, Interstitial, Rewarded, App Open và Rewarded Interstitial.",
                    requiredSDK = "Google Mobile Ads Unity Plugin",
                    defineSymbol = "ADMOB"
                },
                new ModuleInfo
                {
                    name = "AppLovin MAX",
                    fileName = "AppLovinModule.cs",
                    downloadUrl = "https://raw.githubusercontent.com/nhathuy7996/unity3rdlibv2/develop/Assets/HuynnSDK/AdModule/AppLovinModule.cs",
                    localPath = "Assets/HuynnSDK/AdModule/AppLovinModule.cs",
                    description = "AppLovin MAX mediation SDK với support Banner, Interstitial, Rewarded.",
                    requiredSDK = "AppLovin MAX Unity Plugin",
                    defineSymbol = "APPLOVIN"
                },
                new ModuleInfo
                {
                    name = "IronSource",
                    fileName = "IronSourceModule.cs",
                    downloadUrl = "https://raw.githubusercontent.com/nhathuy7996/unity3rdlibv2/develop/Assets/HuynnSDK/AdModule/IronSourceModule.cs",
                    localPath = "Assets/HuynnSDK/AdModule/IronSourceModule.cs",
                    description = "IronSource mediation SDK (stub implementation - cần customize).",
                    requiredSDK = "IronSource Unity Plugin",
                    defineSymbol = "IRONSOURCE"
                },
                new ModuleInfo
                {
                    name = "Unity Ads",
                    fileName = "UnityAdsModule.cs",
                    downloadUrl = "https://raw.githubusercontent.com/nhathuy7996/unity3rdlibv2/develop/Assets/HuynnSDK/AdModule/UnityAdsModule.cs",
                    localPath = "Assets/HuynnSDK/AdModule/UnityAdsModule.cs",
                    description = "Unity Ads SDK (stub implementation - cần customize).",
                    requiredSDK = "Unity Ads Package",
                    defineSymbol = "UNITY_ADS"
                },
                new ModuleInfo
                {
                    name = "Firebase Bridge",
                    fileName = "FirebaseBridge.cs",
                    downloadUrl = "https://raw.githubusercontent.com/nhathuy7996/unity3rdlibv2/develop/Assets/HuynnSDK/FirebaseBridge.cs",
                    localPath = "Assets/HuynnSDK/FirebaseBridge.cs",
                    description = "Firebase Analytics integration tự động log ad events.",
                    requiredSDK = "Firebase Unity SDK",
                    defineSymbol = "FIREBASE"
                }
            };
        }

        private void DrawModuleCard(ModuleInfo module)
        {
            bool isInstalled = File.Exists(module.localPath);
            bool isDownloading = downloadingModules.ContainsKey(module.fileName) && downloadingModules[module.fileName];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            EditorGUILayout.LabelField(module.name, titleStyle);

            if (isInstalled)
            {
                GUIStyle installedStyle = new GUIStyle(EditorStyles.miniLabel);
                installedStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
                EditorGUILayout.LabelField("✓ Installed", installedStyle, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Description
            EditorGUILayout.LabelField(module.description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(3);

            // Info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Required SDK:", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField(module.requiredSDK, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Define Symbol:", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField($"#{module.defineSymbol}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Error message
            if (downloadErrors.ContainsKey(module.fileName))
            {
                EditorGUILayout.HelpBox(downloadErrors[module.fileName], MessageType.Error);
            }

            EditorGUILayout.Space(5);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !isDownloading;

            if (isInstalled)
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Delete Module", GUILayout.Width(120), GUILayout.Height(25)))
                {
                    DeleteModule(module);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                string buttonText = isDownloading ? "Downloading..." : "Download";
                if (GUILayout.Button(buttonText, GUILayout.Width(120), GUILayout.Height(25)))
                {
                    DownloadModule(module);
                }
                GUI.backgroundColor = Color.white;
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private async void DownloadModule(ModuleInfo module)
        {
            downloadingModules[module.fileName] = true;
            downloadErrors.Remove(module.fileName);
            Repaint();

            try
            {
                Debug.Log($"[ThirdLib] Downloading {module.name} from {module.downloadUrl}");

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = System.TimeSpan.FromSeconds(30);
                    var response = await client.GetAsync(module.downloadUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(module.localPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Write file
                        File.WriteAllText(module.localPath, content);

                        // Add define symbol
                        AddDefineSymbol(module.defineSymbol);

                        // Refresh Unity
                        AssetDatabase.Refresh();

                        Debug.Log($"[ThirdLib] Successfully downloaded {module.name} to {module.localPath}");
                        Debug.Log($"[ThirdLib] Added define symbol: {module.defineSymbol}");
                        ShowNotification(new GUIContent($"✓ Downloaded {module.name}"));
                    }
                    else
                    {
                        throw new System.Exception($"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ThirdLib] Failed to download {module.name}: {ex.Message}");
                downloadErrors[module.fileName] = $"Download failed: {ex.Message}";
                ShowNotification(new GUIContent($"✗ Failed to download {module.name}"));
            }
            finally
            {
                downloadingModules[module.fileName] = false;
                Repaint();
            }
        }

        private void DeleteModule(ModuleInfo module)
        {
            if (EditorUtility.DisplayDialog(
                "Delete Module",
                $"Are you sure you want to delete {module.name}?\n\nFile: {module.fileName}",
                "Delete",
                "Cancel"))
            {
                try
                {
                    if (File.Exists(module.localPath))
                    {
                        File.Delete(module.localPath);

                        // Delete .meta file
                        string metaPath = module.localPath + ".meta";
                        if (File.Exists(metaPath))
                        {
                            File.Delete(metaPath);
                        }

                        // Remove define symbol
                        RemoveDefineSymbol(module.defineSymbol);

                        AssetDatabase.Refresh();

                        Debug.Log($"[ThirdLib] Deleted {module.name}");
                        Debug.Log($"[ThirdLib] Removed define symbol: {module.defineSymbol}");
                        ShowNotification(new GUIContent($"✓ Deleted {module.name}"));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ThirdLib] Failed to delete {module.name}: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", $"Failed to delete module:\n{ex.Message}", "OK");
                }
            }
        }

        private void AddDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            var symbolsList = defines.Split(';').ToList();

            if (!symbolsList.Contains(symbol))
            {
                symbolsList.Add(symbol);
                string newDefines = string.Join(";", symbolsList.Where(s => !string.IsNullOrEmpty(s)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
                Debug.Log($"[ThirdLib] Added define symbol '{symbol}' to {targetGroup}");
            }
        }

        private void RemoveDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

            var symbolsList = defines.Split(';').ToList();

            if (symbolsList.Contains(symbol))
            {
                symbolsList.Remove(symbol);
                string newDefines = string.Join(";", symbolsList.Where(s => !string.IsNullOrEmpty(s)));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
                Debug.Log($"[ThirdLib] Removed define symbol '{symbol}' from {targetGroup}");
            }
        }

        private class ModuleInfo
        {
            public string name;
            public string fileName;
            public string downloadUrl;
            public string localPath;
            public string description;
            public string requiredSDK;
            public string defineSymbol;
        }

        #endregion

        #region Custom Definitions Tab

        private void DrawCustomDefinitionsTab()
        {
            if (customDefinitions == null || serializedCustomDefinitions == null)
            {
                EditorGUILayout.HelpBox("CustomAdDefinitions not found. Creating...", MessageType.Warning);
                if (GUILayout.Button("Create CustomAdDefinitions"))
                {
                    LoadOrCreateCustomDefinitions();
                }
                return;
            }

            serializedCustomDefinitions.Update();

            EditorGUILayout.HelpBox(
                "Define custom ad formats and networks here. After editing, click 'Generate Constants' to create type-safe constants.\n" +
                "IDs must be lowercase without spaces (e.g., 'my_custom_banner').",
                MessageType.Info);

            EditorGUILayout.Space();

            // Custom Formats Section
            DrawSection("Custom Ad Formats", () =>
            {
                SerializedProperty customFormats = serializedCustomDefinitions.FindProperty("customFormats");
                EditorGUILayout.PropertyField(customFormats, new GUIContent("Custom Formats"), true);

                EditorGUILayout.Space(5);
                if (GUILayout.Button("+ Add Custom Format", GUILayout.Height(25)))
                {
                    customFormats.arraySize++;
                    var newFormat = customFormats.GetArrayElementAtIndex(customFormats.arraySize - 1);
                    newFormat.FindPropertyRelative("id").stringValue = "custom_format";
                    newFormat.FindPropertyRelative("displayName").stringValue = "Custom Format";
                    newFormat.FindPropertyRelative("description").stringValue = "";
                    serializedCustomDefinitions.ApplyModifiedProperties();
                }
            });

            EditorGUILayout.Space(10);

            // Custom Networks Section
            DrawSection("Custom Ad Networks", () =>
            {
                SerializedProperty customNetworks = serializedCustomDefinitions.FindProperty("customNetworks");
                EditorGUILayout.PropertyField(customNetworks, new GUIContent("Custom Networks"), true);

                EditorGUILayout.Space(5);
                if (GUILayout.Button("+ Add Custom Network", GUILayout.Height(25)))
                {
                    customNetworks.arraySize++;
                    var newNetwork = customNetworks.GetArrayElementAtIndex(customNetworks.arraySize - 1);
                    newNetwork.FindPropertyRelative("id").stringValue = "custom_network";
                    newNetwork.FindPropertyRelative("displayName").stringValue = "Custom Network";
                    newNetwork.FindPropertyRelative("description").stringValue = "";
                    newNetwork.FindPropertyRelative("editorColor").colorValue = Color.white;
                    serializedCustomDefinitions.ApplyModifiedProperties();
                }
            });

            EditorGUILayout.Space(20);

            // Action Buttons
            EditorGUILayout.BeginHorizontal();

            // Validate
            if (GUILayout.Button("Validate All", GUILayout.Height(30)))
            {
                if (customDefinitions.ValidateAll(out var errors))
                {
                    EditorUtility.DisplayDialog("Validation Success", "All definitions are valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Failed", string.Join("\n", errors), "OK");
                }
            }

            // Generate
            using (new EditorGUI.DisabledScope(customDefinitions.customFormats.Count == 0 && customDefinitions.customNetworks.Count == 0))
            {
                if (GUILayout.Button("Generate Constants Class", GUILayout.Height(30)))
                {
                    if (AdDefinitionCodeGenerator.GenerateConstantsClass(customDefinitions))
                    {
                        EditorUtility.DisplayDialog("Success",
                            "Constants class generated successfully!\n\n" +
                            "Generated file: Assets/HuynnSDK/Core/CustomAdConstants.cs\n\n" +
                            "You can now use:\n" +
                            "- CustomAdFormats.YourFormat\n" +
                            "- CustomAdNetworks.YourNetwork",
                            "OK");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Register/Unregister buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Register All to AdRegistry", GUILayout.Height(25)))
            {
                customDefinitions.RegisterAll();
                InitializeAdFormatFoldouts(); // Refresh foldouts
                EditorUtility.DisplayDialog("Success",
                    $"Registered {customDefinitions.customFormats.Count} formats and {customDefinitions.customNetworks.Count} networks to AdRegistry",
                    "OK");
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Unregister All from AdRegistry", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Unregister Custom Definitions",
                    "Are you sure you want to unregister all custom formats and networks from AdRegistry?\n\n" +
                    "This will remove them from runtime but keep them in the CustomAdDefinitions asset.",
                    "Unregister", "Cancel"))
                {
                    customDefinitions.UnregisterAll();
                    InitializeAdFormatFoldouts(); // Refresh foldouts
                    EditorUtility.DisplayDialog("Success",
                        $"Unregistered {customDefinitions.customFormats.Count} formats and {customDefinitions.customNetworks.Count} networks from AdRegistry",
                        "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Usage Example
            EditorGUILayout.HelpBox(
                "Example Usage:\n\n" +
                "// Instead of magic strings:\n" +
                "adUnit.formatId = \"my_custom_banner\";  // ❌ Error-prone\n\n" +
                "// Use generated constants:\n" +
                "adUnit.formatId = CustomAdFormats.MyCustomBanner;  // ✅ Type-safe",
                MessageType.None);

            serializedCustomDefinitions.ApplyModifiedProperties();
        }

        #endregion
    }
}