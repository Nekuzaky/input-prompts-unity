using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekuz.InputPrompts.Editor
{
    /// <summary>
    /// Builds <see cref="InputPromptSet"/> assets and a <see cref="InputPromptDatabase"/> out of the
    /// Kenney "Input Prompts" pack, so the runtime never has to know about file names.
    /// </summary>
    public class InputPromptImporterWindow : EditorWindow
    {
        private const string DatabaseFolder = "Assets/InputPrompts/Resources";
        private const string DatabasePath = DatabaseFolder + "/InputPromptDatabase.asset";

        private static readonly InputDeviceStyle[] Styles =
        {
            InputDeviceStyle.KeyboardMouse,
            InputDeviceStyle.Xbox,
            InputDeviceStyle.PlayStation,
            InputDeviceStyle.Switch,
            InputDeviceStyle.SteamDeck,
            InputDeviceStyle.Generic,
        };

        [SerializeField] private string packFolder = "Assets/Sprites/input-prompts";
        [SerializeField] private string outputFolder = "Assets/InputPrompts/Generated";
        [SerializeField] private string variant = "Default";
        [SerializeField] private bool outlineIcons;
        [SerializeField] private bool coloredFaceButtons = true;
        [SerializeField] private bool applyTextureSettings = true;
        [SerializeField] private InputDeviceStyle gamepadFallbackStyle = InputDeviceStyle.Xbox;

        private Vector2 _scroll;
        private string _report;

        [MenuItem("Tools/Input Prompts/Importer")]
        public static void Open()
        {
            var window = GetWindow<InputPromptImporterWindow>();
            window.titleContent = new GUIContent("Input Prompts");
            window.minSize = new Vector2(420, 360);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Kenney pack", EditorStyles.boldLabel);
            packFolder = FolderField("Pack folder", packFolder);
            variant = EditorGUILayout.TextField(
                new GUIContent("Variant", "Sub-folder of each device: Default (1x) or Double (2x)."), variant);
            outlineIcons = EditorGUILayout.Toggle(
                new GUIContent("Outline icons", "Use the _outline version of every icon when it exists."), outlineIcons);
            coloredFaceButtons = EditorGUILayout.Toggle(
                new GUIContent("Coloured face buttons", "Xbox and PlayStation face buttons in their brand colours."),
                coloredFaceButtons);
            applyTextureSettings = EditorGUILayout.Toggle(
                new GUIContent("Fix texture settings", "Set the PNGs to Sprite, no mipmaps, clamped."), applyTextureSettings);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            outputFolder = FolderField("Prompt sets", outputFolder);
            gamepadFallbackStyle = (InputDeviceStyle)EditorGUILayout.EnumPopup(
                new GUIContent("Unknown gamepads", "Style used for gamepads that match no set."), gamepadFallbackStyle);
            EditorGUILayout.LabelField(" ", DatabasePath, EditorStyles.miniLabel);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder(packFolder)))
            {
                if (GUILayout.Button("Generate prompt sets", GUILayout.Height(30)))
                    Generate();
            }

            if (!AssetDatabase.IsValidFolder(packFolder))
                EditorGUILayout.HelpBox($"Folder not found: {packFolder}", MessageType.Warning);

            if (string.IsNullOrEmpty(_report))
                return;

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static string FolderField(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.TextField(label, value);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    var picked = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);
                    if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                        value = "Assets" + picked[Application.dataPath.Length..].Replace('\\', '/');
                }
            }
            return value;
        }

        private void Generate()
        {
            var log = new System.Text.StringBuilder();
            EnsureFolder(outputFolder);
            EnsureFolder(DatabaseFolder);

            var sets = new Dictionary<InputDeviceStyle, InputPromptSet>();
            var folders = new Dictionary<InputDeviceStyle, string>();

            foreach (var style in Styles)
            {
                var folder = $"{packFolder}/{KenneyNameTable.FolderFor(style)}/{variant}";
                if (AssetDatabase.IsValidFolder(folder))
                    folders[style] = folder;
                else
                    log.AppendLine($"[skipped] {style}: no folder at {folder}");
            }

            // Reimport first: the sprites have to exist before they can be referenced.
            if (applyTextureSettings)
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (var folder in folders.Values)
                        ApplyTextureSettings(folder);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                AssetDatabase.Refresh();
            }

            foreach (var pair in folders)
            {
                var set = BuildSet(pair.Key, pair.Value, log);
                if (set != null)
                    sets[pair.Key] = set;
            }

            WireFallbacks(sets);
            var database = BuildDatabase(sets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine($"Database: {DatabasePath} ({database.sets.Count} sets)");
            _report = log.ToString();
            Debug.Log($"[Input Prompts] Generation finished.\n{_report}", database);
            Selection.activeObject = database;
        }

        private InputPromptSet BuildSet(InputDeviceStyle style, string folder, System.Text.StringBuilder log)
        {
            var map = KenneyNameTable.For(style, coloredFaceButtons);
            if (map.Count == 0)
                return null;

            var entries = new List<InputPromptSet.Entry>(map.Count);
            var missing = new List<string>();

            foreach (var pair in map.OrderBy(p => p.Key, System.StringComparer.Ordinal))
            {
                var sprite = LoadSprite(folder, pair.Value);
                if (sprite == null)
                {
                    missing.Add($"{pair.Key} -> {pair.Value}.png");
                    continue;
                }
                entries.Add(new InputPromptSet.Entry { key = pair.Key, sprite = sprite });
            }

            var path = $"{outputFolder}/InputPromptSet_{style}.asset";
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(path);
            if (set == null)
            {
                set = CreateInstance<InputPromptSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.style = style;
            set.layouts = KenneyNameTable.LayoutsFor(style);
            set.blankSprite = LoadSprite(folder, KenneyNameTable.BlankFor(style));
            set.SetEntries(entries);
            EditorUtility.SetDirty(set);

            log.AppendLine($"[{style}] {entries.Count} icons" + (missing.Count > 0 ? $", {missing.Count} missing" : string.Empty));
            foreach (var item in missing)
                log.AppendLine($"    missing: {item}");

            return set;
        }

        private Sprite LoadSprite(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (outlineIcons)
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
                pair.Value.fallback = generic;
                EditorUtility.SetDirty(pair.Value);
            }
        }

        private InputPromptDatabase BuildDatabase(Dictionary<InputDeviceStyle, InputPromptSet> sets)
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(DatabasePath);
            if (database == null)
            {
                database = CreateInstance<InputPromptDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.sets = Styles.Where(sets.ContainsKey).Select(style => sets[style]).ToList();
            database.gamepadFallback = sets.GetValueOrDefault(gamepadFallbackStyle);
            database.defaultStyle = InputDeviceStyle.KeyboardMouse;
            EditorUtility.SetDirty(database);
            return database;
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

                // WriteImportSettingsIfDirty rather than SaveAndReimport: the reimport happens once,
                // when the surrounding StopAssetEditing lands.
                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                }
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
    }
}
