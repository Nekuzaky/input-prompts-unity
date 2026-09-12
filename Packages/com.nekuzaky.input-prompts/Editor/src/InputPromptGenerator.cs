using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Editor
{
    public static class InputPromptGenerator
    {
        #region Public

        public const string DatabaseName = "SO_InputPromptDatabase";
        public const string SetPrefix = "SO_InputPromptSet_";
        public const string SpriteAssetPrefix = "SO_InputPromptSprites_";
        public const string DefinitionName = "SO_InputPromptPack_Kenney";

        public struct Result
        {
            public int m_icons;
            public int m_missing;
            public int m_sets;
            public string m_report;
            public InputPromptDatabase m_database;
        }

        public readonly struct FamilyInfo
        {
            public readonly InputDeviceStyle m_style;
            public readonly string m_folder;
            public readonly string[] m_layouts;

            public FamilyInfo(InputDeviceStyle style, string folder, string[] layouts)
            {
                m_style = style;
                m_folder = folder;
                m_layouts = layouts;
            }
        }

        #endregion


        #region Main API

        [MenuItem("Tools/Input Prompts/Generate Prompt Sets")]
        public static void GenerateWithDefaults() => Generate(InputPromptSettings.instance);

        public static Result Generate(InputPromptSettings settings) => Generate(GenerationOptions.From(settings));

        public static Result Generate(GenerationOptions options)
        {
            var isTemporary = options.m_definition == null;
            var definition = isTemporary
                ? KenneyNameTable.CreateDefinition(options.m_coloredFaceButtons)
                : options.m_definition;

            try
            {
                return Generate(options, definition);
            }
            finally
            {
                if (isTemporary)
                    UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        public static List<FamilyInfo> DescribeFamilies(InputPromptSettings settings)
        {
            var families = new List<FamilyInfo>();

            if (settings.m_packDefinition != null)
            {
                foreach (var family in settings.m_packDefinition.m_families)
                {
                    if (family != null)
                        families.Add(new FamilyInfo(family.m_style,
                            FolderFor(settings.m_packFolder, settings.m_variant, settings.m_packDefinition, family),
                            family.m_layouts ?? Array.Empty<string>()));
                }

                return families;
            }

            foreach (var style in KenneyNameTable.Families)
            {
                families.Add(new FamilyInfo(style,
                    $"{settings.m_packFolder}/{KenneyNameTable.FolderFor(style)}/{settings.m_variant}",
                    KenneyNameTable.LayoutsFor(style)));
            }

            return families;
        }

        public static int CountIcons(InputPromptSettings settings, InputDeviceStyle style)
        {
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(settings.SetPathFor(style));
            return set != null ? set.Entries.Count : -1;
        }

        public static InputPromptPackDefinition ExportKenneyDefinition(InputPromptSettings settings)
        {
            EnsureFolder(settings.m_outputFolder);

            var path = $"{settings.m_outputFolder}/{DefinitionName}.asset";
            var definition = KenneyNameTable.CreateDefinition(settings.m_coloredFaceButtons);
            AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            return definition;
        }

        public static string DatabasePathIn(string outputFolder) => $"{outputFolder}/Resources/{DatabaseName}.asset";

        public static string SetPathIn(string outputFolder, InputDeviceStyle style) =>
            $"{outputFolder}/{SetPrefix}{style}.asset";

        public static string SpriteAssetPathIn(string outputFolder, InputDeviceStyle style) =>
            $"{outputFolder}/{SpriteAssetPrefix}{style}.asset";

        #endregion


        #region Tools and Utilities

        private static Result Generate(GenerationOptions options, InputPromptPackDefinition definition)
        {
            var log = new StringBuilder();
            var result = new Result();

            EnsureFolder(options.m_outputFolder);
            EnsureFolder($"{options.m_outputFolder}/Resources");

            var families = CollectFamilies(options, definition, log);
            if (options.m_applyTextureSettings)
                FixTextures(families.Select(family => family.folder));

            var sets = new Dictionary<InputDeviceStyle, InputPromptSet>();
            var order = new List<InputDeviceStyle>();

            foreach (var (family, folder) in families)
            {
                var set = BuildSet(options, definition, family, folder, log, ref result);
                if (set == null)
                    continue;

                sets[family.m_style] = set;
                order.Add(family.m_style);
            }

            WireFallbacks(definition, sets);
            result.m_database = BuildDatabase(options, sets, order);
            result.m_sets = sets.Count;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine($"Database: {options.DatabasePath} ({result.m_sets} sets, {result.m_icons} icons)");
            result.m_report = log.ToString();

            Debug.Log($"[Input Prompts] Generation finished.\n{result.m_report}", result.m_database);
            return result;
        }

        private static List<(InputPromptPackDefinition.Family family, string folder)> CollectFamilies(
            GenerationOptions options, InputPromptPackDefinition definition, StringBuilder log)
        {
            var families = new List<(InputPromptPackDefinition.Family, string)>();

            foreach (var family in definition.m_families)
            {
                if (family == null)
                    continue;

                if (!options.IsStyleEnabled(family.m_style))
                {
                    log.AppendLine($"[off] {family.m_style}: disabled in the dashboard");
                    continue;
                }

                var folder = FolderFor(options.m_packFolder, options.m_variant, definition, family);
                if (AssetDatabase.IsValidFolder(folder))
                    families.Add((family, folder));
                else
                    log.AppendLine($"[skipped] {family.m_style}: no folder at {folder}");
            }

            return families;
        }

        private static string FolderFor(string packFolder, IconVariant variant, InputPromptPackDefinition definition,
            InputPromptPackDefinition.Family family)
        {
            var folder = string.IsNullOrEmpty(family.m_folder) ? packFolder : $"{packFolder}/{family.m_folder}";
            return definition.m_usesVariantFolders ? $"{folder}/{variant}" : folder;
        }

        private static InputPromptSet BuildSet(GenerationOptions options, InputPromptPackDefinition definition,
            InputPromptPackDefinition.Family family, string folder, StringBuilder log, ref Result result)
        {
            if (family.m_mappings == null || family.m_mappings.Count == 0)
                return null;

            var entries = new List<InputPromptSet.Entry>(family.m_mappings.Count);
            var missing = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var mapping in family.m_mappings)
            {
                if (string.IsNullOrEmpty(mapping.m_key) || !seen.Add(mapping.m_key.ToLowerInvariant()))
                    continue;

                var sprite = LoadSprite(options, definition, folder, mapping.m_file);
                if (sprite == null)
                {
                    missing.Add($"{mapping.m_key} -> {mapping.m_file}{definition.m_extension}");
                    continue;
                }

                entries.Add(new InputPromptSet.Entry { m_key = mapping.m_key.ToLowerInvariant(), m_sprite = sprite });
            }

            var style = family.m_style;
            var path = options.SetPathFor(style);
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<InputPromptSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.m_style = style;
            set.m_layouts = family.m_layouts ?? Array.Empty<string>();
            set.m_blankSprite = LoadSprite(options, definition, folder, family.m_blankIcon);
            set.m_fallback = null;
            set.SetEntries(entries);

            set.m_spriteAsset = options.m_generateSpriteAssets
                ? SpriteAssetBuilder.Build(set, options.SpriteAssetPathFor(style), options.m_atlasCompression)
                : null;

            EditorUtility.SetDirty(set);

            result.m_icons += entries.Count;
            result.m_missing += missing.Count;

            log.AppendLine($"[{style}] {entries.Count} icons" +
                           (missing.Count > 0 ? $", {missing.Count} missing" : string.Empty));
            foreach (var item in missing)
                log.AppendLine($"    missing: {item}");

            return set;
        }

        private static Sprite LoadSprite(GenerationOptions options, InputPromptPackDefinition definition,
            string folder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (options.m_outlineIcons && !string.IsNullOrEmpty(definition.m_outlineSuffix))
            {
                var outlined = AssetDatabase.LoadAssetAtPath<Sprite>(
                    $"{folder}/{fileName}{definition.m_outlineSuffix}{definition.m_extension}");
                if (outlined != null)
                    return outlined;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{fileName}{definition.m_extension}");
        }

        private static void WireFallbacks(InputPromptPackDefinition definition,
            Dictionary<InputDeviceStyle, InputPromptSet> sets)
        {
            if (!sets.TryGetValue(InputDeviceStyle.Generic, out var generic))
                return;

            foreach (var family in definition.m_families)
            {
                if (family == null || !family.m_fallsBackToGeneric || family.m_style == InputDeviceStyle.Generic)
                    continue;

                if (!sets.TryGetValue(family.m_style, out var set))
                    continue;

                set.m_fallback = generic;
                EditorUtility.SetDirty(set);
            }
        }

        private static InputPromptDatabase BuildDatabase(GenerationOptions options,
            Dictionary<InputDeviceStyle, InputPromptSet> sets, List<InputDeviceStyle> order)
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(options.DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<InputPromptDatabase>();
                AssetDatabase.CreateAsset(database, options.DatabasePath);
            }

            database.m_sets = order.Select(style => sets[style]).ToList();
            database.m_gamepadFallback = sets.GetValueOrDefault(options.m_gamepadFallbackStyle);
            database.m_defaultStyle = options.m_defaultStyle;
            database.m_pointerMotionSwitchesStyle = options.m_pointerMotionSwitchesStyle;
            database.m_useKeyboardLayoutLabels = options.m_useKeyboardLayoutLabels;
            database.m_preferExactDevice = options.m_preferExactDevice;
            EditorUtility.SetDirty(database);
            return database;
        }

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
