using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InputPrompts.Editor
{
    /// <summary>
    /// Builds <see cref="InputPromptSet"/> assets and a <see cref="InputPromptDatabase"/> out of the
    /// Kenney "Input Prompts" pack, so the runtime never has to know about file names.
    /// </summary>
    public class InputPromptImporterWindow : EditorWindow
    {
        #region Private and Protected

        private const string DatabaseName = "SO_InputPromptDatabase";
        private const string SetPrefix = "SO_InputPromptSet_";

        private static readonly InputDeviceStyle[] Styles =
        {
            InputDeviceStyle.KeyboardMouse,
            InputDeviceStyle.Xbox,
            InputDeviceStyle.PlayStation,
            InputDeviceStyle.Switch,
            InputDeviceStyle.SteamDeck,
            InputDeviceStyle.Generic,
        };

        [SerializeField] private string _packFolder = "Assets/Sprites/input-prompts";
        [SerializeField] private string _outputFolder = "Assets/_/Database/InputPrompts";
        [SerializeField] private string _variant = "Default";
        [SerializeField] private bool _outlineIcons;
        [SerializeField] private bool _coloredFaceButtons = true;
        [SerializeField] private bool _applyTextureSettings = true;
        [SerializeField] private InputDeviceStyle _gamepadFallbackStyle = InputDeviceStyle.Xbox;

        private Vector2 _scroll;
        private string _report;

        /// <summary>The database has to sit in a Resources folder for the runtime to load it.</summary>
        private string DatabasePath => $"{_outputFolder}/Resources/{DatabaseName}.asset";

        #endregion


        #region Unity API

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Kenney pack", EditorStyles.boldLabel);
            _packFolder = FolderField("Pack folder", _packFolder);
            _variant = EditorGUILayout.TextField(
                new GUIContent("Variant", "Sub-folder of each device: Default (1x) or Double (2x)."), _variant);
            _outlineIcons = EditorGUILayout.Toggle(
                new GUIContent("Outline icons", "Use the _outline version of every icon when it exists."), _outlineIcons);
            _coloredFaceButtons = EditorGUILayout.Toggle(
                new GUIContent("Coloured face buttons", "Xbox and PlayStation face buttons in their brand colours."),
                _coloredFaceButtons);
            _applyTextureSettings = EditorGUILayout.Toggle(
                new GUIContent("Fix texture settings", "Set the PNGs to Sprite, no mipmaps, clamped."),
                _applyTextureSettings);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            _outputFolder = FolderField("Prompt sets", _outputFolder);
            _gamepadFallbackStyle = (InputDeviceStyle)EditorGUILayout.EnumPopup(
                new GUIContent("Unknown gamepads", "Style used for gamepads that match no set."), _gamepadFallbackStyle);
            EditorGUILayout.LabelField(" ", DatabasePath, EditorStyles.miniLabel);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder(_packFolder)))
            {
                if (GUILayout.Button("Generate prompt sets", GUILayout.Height(30)))
                    Generate();
            }

            if (!AssetDatabase.IsValidFolder(_packFolder))
                EditorGUILayout.HelpBox($"Folder not found: {_packFolder}", MessageType.Warning);

            if (string.IsNullOrEmpty(_report))
                return;

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        #endregion


        #region Main API

        [MenuItem("Tools/Input Prompts/Importer")]
        public static void Open()
        {
            var window = GetWindow<InputPromptImporterWindow>();
            window.titleContent = new GUIContent("Input Prompts");
            window.minSize = new Vector2(420, 360);
        }

        /// <summary>
        /// Runs the import with the default settings, for a menu click or a batchmode build step:
        /// <c>-executeMethod InputPrompts.Editor.InputPromptImporterWindow.GenerateWithDefaults</c>.
        /// </summary>
        [MenuItem("Tools/Input Prompts/Generate With Defaults")]
        public static void GenerateWithDefaults()
        {
            var window = CreateInstance<InputPromptImporterWindow>();
            try
            {
                window.Generate();
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        /// <summary>Imports the pack and writes one set per device family plus the database.</summary>
        public void Generate()
        {
            var log = new StringBuilder();
            EnsureFolder(_outputFolder);
            EnsureFolder($"{_outputFolder}/Resources");

            var folders = CollectFolders(log);
            if (_applyTextureSettings)
                FixTextures(folders.Values);

            var sets = new Dictionary<InputDeviceStyle, InputPromptSet>();
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
            log.AppendLine($"Database: {DatabasePath} ({database.m_sets.Count} sets)");
            _report = log.ToString();
            Debug.Log($"[Input Prompts] Generation finished.\n{_report}", database);
            Selection.activeObject = database;
        }

        #endregion


        #region Tools and Utilities

        private Dictionary<InputDeviceStyle, string> CollectFolders(StringBuilder log)
        {
            var folders = new Dictionary<InputDeviceStyle, string>();

            foreach (var style in Styles)
            {
                var folder = $"{_packFolder}/{KenneyNameTable.FolderFor(style)}/{_variant}";
                if (AssetDatabase.IsValidFolder(folder))
                    folders[style] = folder;
                else
                    log.AppendLine($"[skipped] {style}: no folder at {folder}");
            }

            return folders;
        }

        private InputPromptSet BuildSet(InputDeviceStyle style, string folder, StringBuilder log)
        {
            var map = KenneyNameTable.For(style, _coloredFaceButtons);
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

                entries.Add(new InputPromptSet.Entry { m_key = pair.Key, m_sprite = sprite });
            }

            var path = $"{_outputFolder}/{SetPrefix}{style}.asset";
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(path);
            if (set == null)
            {
                set = CreateInstance<InputPromptSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.m_style = style;
            set.m_layouts = KenneyNameTable.LayoutsFor(style);
            set.m_blankSprite = LoadSprite(folder, KenneyNameTable.BlankFor(style));
            set.SetEntries(entries);
            EditorUtility.SetDirty(set);

            log.AppendLine($"[{style}] {entries.Count} icons" +
                           (missing.Count > 0 ? $", {missing.Count} missing" : string.Empty));
            foreach (var item in missing)
                log.AppendLine($"    missing: {item}");

            return set;
        }

        private Sprite LoadSprite(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (_outlineIcons)
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

        private InputPromptDatabase BuildDatabase(Dictionary<InputDeviceStyle, InputPromptSet> sets)
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(DatabasePath);
            if (database == null)
            {
                database = CreateInstance<InputPromptDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.m_sets = Styles.Where(sets.ContainsKey).Select(style => sets[style]).ToList();
            database.m_gamepadFallback = sets.GetValueOrDefault(_gamepadFallbackStyle);
            database.m_defaultStyle = InputDeviceStyle.KeyboardMouse;
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

        private static string FolderField(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.TextField(label, value);
                if (!GUILayout.Button("...", GUILayout.Width(28)))
                    return value;

                var picked = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                    value = "Assets" + picked[Application.dataPath.Length..].Replace('\\', '/');
            }

            return value;
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
