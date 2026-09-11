using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Editor
{
    /// <summary>
    /// Builds <see cref="InputPromptSet"/> assets and a <see cref="InputPromptDatabase"/> out of the
    /// Kenney "Input Prompts" pack, so the runtime never has to know about file names.
    /// </summary>
    public static class InputPromptGenerator
    {
        #region Public

        public const string DatabaseName = "SO_InputPromptDatabase";
        public const string SetPrefix = "SO_InputPromptSet_";

        /// <summary>Device families the importer knows how to build, in database priority order.</summary>
        public static readonly InputDeviceStyle[] SupportedStyles =
        {
            InputDeviceStyle.KeyboardMouse,
            InputDeviceStyle.Xbox,
            InputDeviceStyle.PlayStation,
            InputDeviceStyle.Switch,
            InputDeviceStyle.SteamDeck,
            InputDeviceStyle.Generic,
        };

        /// <summary>What one generation run produced, for the dashboard to display.</summary>
        public struct Result
        {
            public int m_icons;
            public int m_missing;
            public int m_sets;
            public string m_report;
            public InputPromptDatabase m_database;
        }

        #endregion


        #region Main API

        /// <summary>
        /// Runs the import with the stored settings, for a menu click or a batchmode build step:
        /// <c>-executeMethod InputPrompts.Editor.InputPromptGenerator.GenerateWithDefaults</c>.
        /// </summary>
        [MenuItem("Tools/Input Prompts/Generate Prompt Sets")]
        public static void GenerateWithDefaults() => Generate(InputPromptSettings.instance);

        /// <summary>Imports the pack and writes one set per enabled device family, plus the database.</summary>
        public static Result Generate(InputPromptSettings settings)
        {
            var log = new StringBuilder();
            var result = new Result();

            EnsureFolder(settings.m_outputFolder);
            EnsureFolder($"{settings.m_outputFolder}/Resources");

            var folders = CollectFolders(settings, log);
            if (settings.m_applyTextureSettings)
                FixTextures(folders.Values);

            var sets = new Dictionary<InputDeviceStyle, InputPromptSet>();
            foreach (var pair in folders)
            {
                var set = BuildSet(settings, pair.Key, pair.Value, log, ref result);
                if (set != null)
                    sets[pair.Key] = set;
            }

            WireFallbacks(sets);
            result.m_database = BuildDatabase(settings, sets);
            result.m_sets = sets.Count;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine($"Database: {settings.DatabasePath} ({result.m_sets} sets, {result.m_icons} icons)");
            result.m_report = log.ToString();

            Debug.Log($"[Input Prompts] Generation finished.\n{result.m_report}", result.m_database);
            return result;
        }

        /// <summary>Number of icons a generated set holds, or -1 when it has not been generated yet.</summary>
        public static int CountIcons(InputPromptSettings settings, InputDeviceStyle style)
        {
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(settings.SetPathFor(style));
            return set != null ? set.Entries.Count : -1;
        }

        #endregion


        #region Tools and Utilities

        private static Dictionary<InputDeviceStyle, string> CollectFolders(InputPromptSettings settings,
            StringBuilder log)
        {
            var folders = new Dictionary<InputDeviceStyle, string>();

            foreach (var style in SupportedStyles)
            {
                if (!settings.IsStyleEnabled(style))
                {
                    log.AppendLine($"[off] {style}: disabled in the dashboard");
                    continue;
                }

                var folder = settings.FolderFor(style);
                if (AssetDatabase.IsValidFolder(folder))
                    folders[style] = folder;
                else
                    log.AppendLine($"[skipped] {style}: no folder at {folder}");
            }

            return folders;
        }

        private static InputPromptSet BuildSet(InputPromptSettings settings, InputDeviceStyle style, string folder,
            StringBuilder log, ref Result result)
        {
            var map = KenneyNameTable.For(style, settings.m_coloredFaceButtons);
            if (map.Count == 0)
                return null;

            var entries = new List<InputPromptSet.Entry>(map.Count);
            var missing = new List<string>();

            foreach (var pair in map.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var sprite = LoadSprite(settings, folder, pair.Value);
                if (sprite == null)
                {
                    missing.Add($"{pair.Key} -> {pair.Value}.png");
                    continue;
                }

                entries.Add(new InputPromptSet.Entry { m_key = pair.Key, m_sprite = sprite });
            }

            var path = settings.SetPathFor(style);
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<InputPromptSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.m_style = style;
            set.m_layouts = KenneyNameTable.LayoutsFor(style);
            set.m_blankSprite = LoadSprite(settings, folder, KenneyNameTable.BlankFor(style));
            set.SetEntries(entries);
            EditorUtility.SetDirty(set);

            result.m_icons += entries.Count;
            result.m_missing += missing.Count;

            log.AppendLine($"[{style}] {entries.Count} icons" +
                           (missing.Count > 0 ? $", {missing.Count} missing" : string.Empty));
            foreach (var item in missing)
                log.AppendLine($"    missing: {item}");

            return set;
        }

        private static Sprite LoadSprite(InputPromptSettings settings, string folder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (settings.m_outlineIcons)
            {
                var outlined = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{fileName}_outline.png");
                if (outlined != null)
                    return outlined;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{fileName}.png");
        }

        private static void WireFallbacks(Dictionary<InputDeviceStyle, InputPromptSet> sets)
        {
            if (!sets.TryGetValue(InputDeviceStyle.Generic, out var generic))
                return;

            foreach (var pair in sets)
            {
                if (pair.Key == InputDeviceStyle.Generic || pair.Key == InputDeviceStyle.KeyboardMouse)
                    continue;

                pair.Value.m_fallback = generic;
                EditorUtility.SetDirty(pair.Value);
            }
        }

        private static InputPromptDatabase BuildDatabase(InputPromptSettings settings,
            Dictionary<InputDeviceStyle, InputPromptSet> sets)
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(settings.DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<InputPromptDatabase>();
                AssetDatabase.CreateAsset(database, settings.DatabasePath);
            }

            database.m_sets = SupportedStyles.Where(sets.ContainsKey).Select(style => sets[style]).ToList();
            database.m_gamepadFallback = sets.GetValueOrDefault(settings.m_gamepadFallbackStyle);
            database.m_defaultStyle = settings.m_defaultStyle;
            database.m_pointerMotionSwitchesStyle = settings.m_pointerMotionSwitchesStyle;
            database.m_useKeyboardLayoutLabels = settings.m_useKeyboardLayoutLabels;
            database.m_preferExactDevice = settings.m_preferExactDevice;
            EditorUtility.SetDirty(database);
            return database;
        }

        /// <summary>One reimport for the whole pack instead of one per file.</summary>
        private static void FixTextures(IEnumerable<string> folders)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var folder in folders)
                    ApplyTextureSettings(folder);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
        }

        private static void ApplyTextureSettings(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;

                var changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (!changed)
                    continue;

                EditorUtility.SetDirty(importer);
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        #endregion
    }
}
