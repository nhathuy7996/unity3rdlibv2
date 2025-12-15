using System.Collections.Generic;
using UnityEngine;

namespace GameDevToi.ThirdLib.Core
{
    /// <summary>
    /// ScriptableObject để lưu custom ad formats và networks do user định nghĩa
    /// Auto-generate constants class khi có thay đổi
    /// </summary>
    [CreateAssetMenu(fileName = "CustomAdDefinitions", menuName = "ThirdLib/Custom Ad Definitions")]
    public class CustomAdDefinitions : ScriptableObject
    {
        [Header("Custom Ad Formats")]
        [Tooltip("Các ad format tùy chỉnh của bạn")]
        public List<CustomAdFormat> customFormats = new List<CustomAdFormat>();

        [Header("Custom Ad Networks")]
        [Tooltip("Các ad network tùy chỉnh của bạn")]
        public List<CustomAdNetwork> customNetworks = new List<CustomAdNetwork>();

        [System.Serializable]
        public class CustomAdFormat
        {
            [Tooltip("ID duy nhất (lowercase, no spaces, e.g., 'custom_banner')")]
            public string id;

            [Tooltip("Tên hiển thị")]
            public string displayName;

            [TextArea(2, 4)]
            [Tooltip("Mô tả")]
            public string description;

            public AdFormatDefinition ToDefinition()
            {
                return new AdFormatDefinition(id, displayName, description);
            }

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(displayName);
            }
        }

        [System.Serializable]
        public class CustomAdNetwork
        {
            [Tooltip("ID duy nhất (lowercase, no spaces, e.g., 'custom_network')")]
            public string id;

            [Tooltip("Tên hiển thị")]
            public string displayName;

            [TextArea(2, 4)]
            [Tooltip("Mô tả")]
            public string description;

            [Tooltip("Màu hiển thị trong Editor")]
            public Color editorColor = Color.white;

            public AdNetworkDefinition ToDefinition()
            {
                return new AdNetworkDefinition(id, displayName, description, editorColor);
            }

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(displayName);
            }
        }

        /// <summary>
        /// Validate tất cả definitions
        /// </summary>
        public bool ValidateAll(out List<string> errors)
        {
            errors = new List<string>();

            // Kiểm tra duplicate format IDs
            var formatIds = new HashSet<string>();
            foreach (var format in customFormats)
            {
                if (!format.IsValid())
                {
                    errors.Add($"Invalid format: ID or DisplayName is empty");
                    continue;
                }

                if (!formatIds.Add(format.id))
                {
                    errors.Add($"Duplicate format ID: {format.id}");
                }

                // Kiểm tra format có hợp lệ không (lowercase, no spaces)
                if (format.id != format.id.ToLower() || format.id.Contains(" "))
                {
                    errors.Add($"Format ID '{format.id}' must be lowercase without spaces");
                }
            }

            // Kiểm tra duplicate network IDs
            var networkIds = new HashSet<string>();
            foreach (var network in customNetworks)
            {
                if (!network.IsValid())
                {
                    errors.Add($"Invalid network: ID or DisplayName is empty");
                    continue;
                }

                if (!networkIds.Add(network.id))
                {
                    errors.Add($"Duplicate network ID: {network.id}");
                }

                // Kiểm tra network có hợp lệ không (lowercase, no spaces)
                if (network.id != network.id.ToLower() || network.id.Contains(" "))
                {
                    errors.Add($"Network ID '{network.id}' must be lowercase without spaces");
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Đăng ký tất cả custom definitions vào registry
        /// </summary>
        public void RegisterAll()
        {
            foreach (var format in customFormats)
            {
                if (format.IsValid())
                {
                    AdRegistry.RegisterFormat(format.ToDefinition());
                }
            }

            foreach (var network in customNetworks)
            {
                if (network.IsValid())
                {
                    AdRegistry.RegisterNetwork(network.ToDefinition());
                }
            }
        }

        /// <summary>
        /// Hủy đăng ký tất cả custom definitions khỏi registry
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var format in customFormats)
            {
                if (format.IsValid())
                {
                    AdRegistry.UnregisterFormat(format.id);
                }
            }

            foreach (var network in customNetworks)
            {
                if (network.IsValid())
                {
                    AdRegistry.UnregisterNetwork(network.id);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto validate khi có thay đổi trong Editor
            ValidateAll(out var errors);
            if (errors.Count > 0)
            {
                Debug.LogWarning($"[CustomAdDefinitions] Validation errors:\n{string.Join("\n", errors)}");
            }
        }
#endif
    }
}
