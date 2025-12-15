using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using GameDevToi.ThirdLib.Core;

namespace GameDevToi.ThirdLib.Editor
{
    /// <summary>
    /// Code generator để tự động tạo constants class từ custom ad definitions
    /// </summary>
    public static class AdDefinitionCodeGenerator
    {
        private const string GENERATED_FILE_PATH = "Assets/HuynnSDK/Core/CustomAdConstants.cs";

        /// <summary>
        /// Generate constants class từ CustomAdDefinitions
        /// </summary>
        public static bool GenerateConstantsClass(CustomAdDefinitions definitions)
        {
            if (definitions == null)
            {
                Debug.LogError("[AdDefinitionCodeGenerator] CustomAdDefinitions is null");
                return false;
            }

            // Validate
            if (!definitions.ValidateAll(out var errors))
            {
                Debug.LogError($"[AdDefinitionCodeGenerator] Validation failed:\n{string.Join("\n", errors)}");
                return false;
            }

            StringBuilder sb = new StringBuilder();

            // File header
            sb.AppendLine("// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY");
            sb.AppendLine("// Generated from CustomAdDefinitions");
            sb.AppendLine("// Use ThirdLib Window > Custom Definitions tab to modify");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace GameDevToi.ThirdLib.Core");
            sb.AppendLine("{");

            // Generate Custom Formats class
            if (definitions.customFormats.Count > 0)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Custom Ad Format IDs (auto-generated)");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    public static class CustomAdFormats");
                sb.AppendLine("    {");

                foreach (var format in definitions.customFormats)
                {
                    if (!format.IsValid()) continue;

                    string constantName = ToConstantName(format.displayName);
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {format.displayName}");
                    if (!string.IsNullOrEmpty(format.description))
                    {
                        sb.AppendLine($"        /// {format.description}");
                    }
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public const string {constantName} = \"{format.id}\";");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Generate Custom Networks class
            if (definitions.customNetworks.Count > 0)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Custom Ad Network IDs (auto-generated)");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    public static class CustomAdNetworks");
                sb.AppendLine("    {");

                foreach (var network in definitions.customNetworks)
                {
                    if (!network.IsValid()) continue;

                    string constantName = ToConstantName(network.displayName);
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {network.displayName}");
                    if (!string.IsNullOrEmpty(network.description))
                    {
                        sb.AppendLine($"        /// {network.description}");
                    }
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public const string {constantName} = \"{network.id}\";");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            // Ensure directory exists
            string directory = Path.GetDirectoryName(GENERATED_FILE_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write file
            File.WriteAllText(GENERATED_FILE_PATH, sb.ToString());
            AssetDatabase.ImportAsset(GENERATED_FILE_PATH);

            Debug.Log($"[AdDefinitionCodeGenerator] Generated constants at {GENERATED_FILE_PATH}");
            return true;
        }

        /// <summary>
        /// Convert display name to constant name (e.g., "My Custom Banner" -> "MyCustomBanner")
        /// </summary>
        private static string ToConstantName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "Unknown";

            // Remove special characters and split by space
            var words = displayName
                .Replace("-", " ")
                .Replace("_", " ")
                .Split(' ')
                .Where(w => !string.IsNullOrEmpty(w))
                .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower());

            return string.Join("", words);
        }

        /// <summary>
        /// Xóa generated file
        /// </summary>
        public static void DeleteGeneratedFile()
        {
            if (File.Exists(GENERATED_FILE_PATH))
            {
                File.Delete(GENERATED_FILE_PATH);
                File.Delete(GENERATED_FILE_PATH + ".meta");
                AssetDatabase.Refresh();
                Debug.Log("[AdDefinitionCodeGenerator] Deleted generated file");
            }
        }
    }

    /// <summary>
    /// Custom Editor cho CustomAdDefinitions với button generate
    /// </summary>
    [CustomEditor(typeof(CustomAdDefinitions))]
    public class CustomAdDefinitionsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            CustomAdDefinitions definitions = (CustomAdDefinitions)target;

            // Validate button
            if (GUILayout.Button("Validate Definitions", GUILayout.Height(30)))
            {
                if (definitions.ValidateAll(out var errors))
                {
                    EditorUtility.DisplayDialog("Validation Success", "All definitions are valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Failed",
                        string.Join("\n", errors), "OK");
                }
            }

            EditorGUILayout.Space(5);

            // Generate button
            using (new EditorGUI.DisabledScope(definitions.customFormats.Count == 0 && definitions.customNetworks.Count == 0))
            {
                if (GUILayout.Button("Generate Constants Class", GUILayout.Height(30)))
                {
                    if (AdDefinitionCodeGenerator.GenerateConstantsClass(definitions))
                    {
                        EditorUtility.DisplayDialog("Success",
                            "Constants class generated successfully!\nYou can now use CustomAdFormats and CustomAdNetworks classes.",
                            "OK");
                    }
                }
            }

            EditorGUILayout.Space(5);

            // Register button
            if (GUILayout.Button("Register All to AdRegistry", GUILayout.Height(25)))
            {
                definitions.RegisterAll();
                EditorUtility.DisplayDialog("Success",
                    $"Registered {definitions.customFormats.Count} formats and {definitions.customNetworks.Count} networks",
                    "OK");
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "1. Add custom formats/networks above\n" +
                "2. Click 'Validate' to check for errors\n" +
                "3. Click 'Generate Constants' to create type-safe constants\n" +
                "4. Use CustomAdFormats.YourFormat and CustomAdNetworks.YourNetwork in code",
                MessageType.Info);
        }
    }
}
