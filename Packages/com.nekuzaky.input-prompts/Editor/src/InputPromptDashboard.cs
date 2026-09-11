using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.InputPrompts.Editor
{
    public class InputPromptDashboard : EditorWindow
    {
        #region Private and Protected

        private const string StyleSheetPath =
            "Packages/com.nekuzaky.input-prompts/Editor/UI/InputPromptDashboard.uss";

        private const string RepositoryUrl = "https://github.com/Nekuzaky/input-prompts-unity";
        private const string CoffeeUrl = "https://buymeacoffee.com/nekuzaky";

        private const float CompactWidth = 760f;

        private static readonly string[] GamepadPreviewKeys =
        {
            "buttonsouth", "buttoneast", "buttonwest", "buttonnorth",
            "dpad/up", "dpad/down", "dpad/left", "dpad/right",
            "leftstick", "rightstick", "leftstickpress", "rightstickpress",
            "leftshoulder", "rightshoulder", "lefttrigger", "righttrigger",
            "start", "select",
        };

        private static readonly string[] KeyboardPreviewKeys =
        {
            "w", "a", "s", "d", "space", "enter", "escape", "tab",
            "leftshift", "leftctrl", "e", "f", "1", "uparrow",
            "leftbutton", "rightbutton", "middlebutton", "scroll/y",
        };

        private InputPromptSettings _settings;
        private InputDeviceStyle _previewStyle = InputDeviceStyle.KeyboardMouse;

        private Label _statusPill;
        private Label _databaseNote;
        private VisualElement _header;
        private VisualElement _footer;
        private VisualElement _columns;
        private VisualElement _deviceList;
        private VisualElement _previewGrid;
        private Label _previewTitle;
        private TextField _report;

        #endregion


        #region Unity API

        private void CreateGUI()
        {
            _settings = InputPromptSettings.instance;

            var root = rootVisualElement;
            root.AddToClassList("ip-root");

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (sheet != null)
                root.styleSheets.Add(sheet);

            root.Add(BuildHeader());
            root.Add(BuildBody());
            root.Add(BuildFooter());
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            RefreshAll();
        }

        private void OnFocus()
        {
            if (_deviceList != null)
                RefreshAll();
        }

        #endregion


        #region Main API

        [MenuItem("Tools/Input Prompts/Dashboard", false, 0)]
        public static void Open()
        {
            var window = GetWindow<InputPromptDashboard>();
            window.titleContent = new GUIContent("Input Prompts");
            window.minSize = new Vector2(420, 380);
            window.Show();
        }

        public static (string emoji, string fallback) BadgeFor(InputDeviceStyle style) => style switch
        {
            InputDeviceStyle.KeyboardMouse => ("⌨️", "▤"),
            InputDeviceStyle.Xbox => ("🟢", "●"),
            InputDeviceStyle.PlayStation => ("🔵", "●"),
            InputDeviceStyle.Switch => ("🔴", "●"),
            InputDeviceStyle.SteamDeck => ("🟣", "●"),
            InputDeviceStyle.SteamController => ("⚫", "●"),
            InputDeviceStyle.Touch => ("👆", "◍"),
            _ => ("⚪", "○"),
        };

        #endregion


        #region Tools and Utilities

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("ip-header");
            _header = header;

            var titles = new VisualElement();
            titles.AddToClassList("ip-header__titles");

            var titleRow = MakeRow();
            titleRow.Add(DashboardGlyphs.Icon("🎮", "◆", "ip-glyph--title"));
            titleRow.Add(MakeLabel("Input Prompts", "ip-title"));
            titles.Add(titleRow);
            titles.Add(MakeLabel("Key and button icons that follow the device the player is using",
                "ip-subtitle"));
            header.Add(titles);

            var actions = MakeRow("ip-header__actions");
            _statusPill = MakeLabel("…", "ip-pill");
            actions.Add(_statusPill);
            actions.Add(MakeButton("🎬", "▷", "Demo", InputPromptMenu.CreateDemo, "ip-button"));
            actions.Add(MakeButton("⚡", "▶", "Generate", RunGeneration, "ip-button", "ip-button--primary"));
            header.Add(actions);

            return header;
        }

        private VisualElement BuildBody()
        {
            var scroll = new ScrollView();
            scroll.AddToClassList("ip-body");

            var columns = new VisualElement();
            columns.AddToClassList("ip-columns");
            _columns = columns;

            var left = new VisualElement();
            left.AddToClassList("ip-column");
            left.Add(BuildSourceCard());
            left.Add(BuildOutputCard());
            left.Add(BuildRuntimeCard());

            var right = new VisualElement();
            right.AddToClassList("ip-column");
            right.Add(BuildDevicesCard());
            right.Add(BuildPreviewCard());

            columns.Add(left);
            columns.Add(right);
            scroll.Add(columns);
            scroll.Add(BuildReportCard());

            return scroll;
        }

        private VisualElement BuildSourceCard()
        {
            var card = MakeCard("📁", "▤", "Source", "Kenney pack", out var content);

            content.Add(MakeFolderField("Pack folder", _settings.m_packFolder, value =>
            {
                _settings.m_packFolder = value;
                _settings.SaveSettings();
                RefreshDevices();
            }));

            content.Add(MakeDropdown("Variant", Enum.GetNames(typeof(IconVariant)), _settings.m_variant.ToString(),
                value =>
                {
                    _settings.m_variant = Enum.Parse<IconVariant>(value);
                    _settings.SaveSettings();
                    RefreshDevices();
                }));

            content.Add(MakeToggle("Outline icons", _settings.m_outlineIcons, value =>
            {
                _settings.m_outlineIcons = value;
                _settings.SaveSettings();
            }));

            content.Add(MakeToggle("Coloured face buttons", _settings.m_coloredFaceButtons, value =>
            {
                _settings.m_coloredFaceButtons = value;
                _settings.SaveSettings();
            }));

            content.Add(MakeToggle("Fix texture settings", _settings.m_applyTextureSettings, value =>
            {
                _settings.m_applyTextureSettings = value;
                _settings.SaveSettings();
            }));

            content.Add(MakeLabel("Default = 1x, Double = 2x. Settings are shared through ProjectSettings.",
                "ip-note"));

            return card;
        }

        private VisualElement BuildOutputCard()
        {
            var card = MakeCard("💾", "▣", "Output", "Generated assets", out var content);

            content.Add(MakeFolderField("Prompt sets", _settings.m_outputFolder, value =>
            {
                _settings.m_outputFolder = value;
                _settings.SaveSettings();
                RefreshStatus();
            }));

            content.Add(MakeFolderField("Demo prefab", _settings.m_prefabFolder, value =>
            {
                _settings.m_prefabFolder = value;
                _settings.SaveSettings();
            }));

            content.Add(MakeDropdown("Unknown gamepads", StyleNames(), _settings.m_gamepadFallbackStyle.ToString(),
                value =>
                {
                    _settings.m_gamepadFallbackStyle = Enum.Parse<InputDeviceStyle>(value);
                    _settings.SaveSettings();
                }));

            content.Add(MakeDropdown("Startup style", StyleNames(), _settings.m_defaultStyle.ToString(), value =>
            {
                _settings.m_defaultStyle = Enum.Parse<InputDeviceStyle>(value);
                _settings.SaveSettings();
            }));

            _databaseNote = MakeLabel(string.Empty, "ip-note");
            content.Add(_databaseNote);

            var actions = MakeRow();
            actions.Add(MakeButton("🔍", "→", "Ping database", PingDatabase, "ip-button", "ip-button--ghost"));
            actions.Add(MakeButton("♻️", "↻", "Reload", RefreshAll, "ip-button", "ip-button--ghost"));
            content.Add(actions);

            return card;
        }

        private VisualElement BuildRuntimeCard()
        {
            var card = MakeCard("🔧", "⚙", "Runtime", "Written into the database", out var content);

            content.Add(MakeToggle("Pointer motion switches style", _settings.m_pointerMotionSwitchesStyle, value =>
            {
                _settings.m_pointerMotionSwitchesStyle = value;
                _settings.SaveSettings();
                ApplyRuntimeToDatabase();
            }));

            content.Add(MakeToggle("Use keyboard layout labels", _settings.m_useKeyboardLayoutLabels, value =>
            {
                _settings.m_useKeyboardLayoutLabels = value;
                _settings.SaveSettings();
                ApplyRuntimeToDatabase();
            }));

            content.Add(MakeToggle("Prefer exact device", _settings.m_preferExactDevice, value =>
            {
                _settings.m_preferExactDevice = value;
                _settings.SaveSettings();
                ApplyRuntimeToDatabase();
            }));

            content.Add(MakeLabel(
                "AZERTY: <Keyboard>/w draws the Z key. Uncheck to keep the QWERTY label.\n"
                + "Prefer exact device: an action bound to both a key and a mouse button follows the last "
                + "device used, instead of keeping the first binding of the family.",
                "ip-note"));

            return card;
        }

        private VisualElement BuildDevicesCard()
        {
            var card = MakeCard("🎛️", "◈", "Devices", "Click to preview", out var content);

            _deviceList = new VisualElement();
            content.Add(_deviceList);
            content.Add(MakeLabel("Uncheck a device to leave it out of the generation.", "ip-note"));

            return card;
        }

        private VisualElement BuildPreviewCard()
        {
            var card = MakeCard("🎨", "◐", "Preview", "Generated icons", out var content);

            _previewTitle = MakeLabel(string.Empty, "ip-card__hint");
            content.Add(_previewTitle);

            _previewGrid = new VisualElement();
            _previewGrid.AddToClassList("ip-preview");
            content.Add(_previewGrid);

            var actions = MakeRow();
            actions.Add(MakeButton("👁️", "◉", "Force this style", ForcePreviewStyle, "ip-button", "ip-button--ghost"));
            actions.Add(MakeButton("🔄", "↺", "Follow device", ClearPreviewStyle, "ip-button", "ip-button--ghost"));
            content.Add(actions);

            return card;
        }

        private VisualElement BuildReportCard()
        {
            var card = MakeCard("📊", "≡", "Report", "Last generation", out var content);
            card.style.marginLeft = 6;
            card.style.marginRight = 6;

            var scroll = new ScrollView();
            scroll.AddToClassList("ip-report");

            _report = new TextField { multiline = true, isReadOnly = true, value = "—" };
            _report.AddToClassList("ip-report__text");
            scroll.Add(_report);
            content.Add(scroll);

            return card;
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            _footer = footer;
            footer.AddToClassList("ip-footer");

            var version = UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly)?.version;
            footer.Add(MakeLabel($"com.nekuzaky.input-prompts {(version != null ? "v" + version : "(embedded)")}"
                                 + "   ·   Kenney icons, CC0   ·   0 coroutines, 0 Update", "ip-footer__text"));

            var actions = MakeRow("ip-footer__actions");
            actions.Add(MakeTextureButton("d_TextAsset Icon", "Docs", () => Application.OpenURL(RepositoryUrl),
                "ip-button", "ip-button--ghost"));
            actions.Add(MakeButton("☕", "♥", "Buy me a coffee", () => Application.OpenURL(CoffeeUrl),
                "ip-button", "ip-button--coffee"));
            footer.Add(actions);

            return footer;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var isCompact = evt.newRect.width < CompactWidth;
            _columns?.EnableInClassList("ip-columns--compact", isCompact);
            _header?.EnableInClassList("ip-header--compact", isCompact);
            _footer?.EnableInClassList("ip-footer--compact", isCompact);
        }


        private void RefreshAll()
        {
            RefreshStatus();
            RefreshDevices();
            RefreshPreview();
        }

        private void RefreshStatus()
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(_settings.DatabasePath);
            var packExists = AssetDatabase.IsValidFolder(_settings.m_packFolder);

            _statusPill.RemoveFromClassList("ip-pill--ok");
            _statusPill.RemoveFromClassList("ip-pill--warn");
            _statusPill.RemoveFromClassList("ip-pill--error");

            if (!packExists)
            {
                _statusPill.text = "Pack not found";
                _statusPill.AddToClassList("ip-pill--error");
            }
            else if (database == null)
            {
                _statusPill.text = "Not generated yet";
                _statusPill.AddToClassList("ip-pill--warn");
            }
            else
            {
                _statusPill.text = $"{database.m_sets.Count} sets ready";
                _statusPill.AddToClassList("ip-pill--ok");
            }

            if (_databaseNote == null)
                return;

            _databaseNote.text = database != null
                ? $"Database: {_settings.DatabasePath}"
                : $"Will be created at: {_settings.DatabasePath}";
            _databaseNote.EnableInClassList("ip-note--warn", database == null);
        }

        private void RefreshDevices()
        {
            if (_deviceList == null)
                return;

            _deviceList.Clear();

            foreach (var style in InputPromptGenerator.SupportedStyles)
                _deviceList.Add(BuildDeviceRow(style));
        }

        private VisualElement BuildDeviceRow(InputDeviceStyle style)
        {
            var row = MakeRow("ip-device");
            row.EnableInClassList("ip-device--selected", style == _previewStyle);

            var toggle = new Toggle { value = _settings.IsStyleEnabled(style) };
            toggle.RegisterValueChangedCallback(evt =>
            {
                _settings.SetStyleEnabled(style, evt.newValue);
                evt.StopPropagation();
            });
            row.Add(toggle);

            var (emoji, fallback) = BadgeFor(style);
            row.Add(DashboardGlyphs.Icon(emoji, fallback, "ip-device__badge", $"ip-badge--{style}"));
            row.Add(MakeLabel(ObjectNames.NicifyVariableName(style.ToString()), "ip-device__name"));

            var layouts = KenneyNameTable.LayoutsFor(style);
            row.Add(MakeLabel(layouts.Length > 0 ? string.Join(", ", layouts) : "—", "ip-device__layout"));

            var count = InputPromptGenerator.CountIcons(_settings, style);
            var hasFolder = AssetDatabase.IsValidFolder(_settings.FolderFor(style));
            row.Add(MakeLabel(count >= 0 ? $"{count} icons" : hasFolder ? "to generate" : "folder?",
                "ip-device__count"));

            row.RegisterCallback<ClickEvent>(_ =>
            {
                _previewStyle = style;
                RefreshDevices();
                RefreshPreview();
            });

            return row;
        }

        private void RefreshPreview()
        {
            if (_previewGrid == null)
                return;

            _previewGrid.Clear();

            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(_settings.SetPathFor(_previewStyle));
            _previewTitle.text = set != null
                ? $"{_previewStyle} · {set.Entries.Count} icônes"
                : $"{_previewStyle} · set not generated";

            if (set == null)
                return;

            var keys = _previewStyle == InputDeviceStyle.KeyboardMouse ? KeyboardPreviewKeys : GamepadPreviewKeys;
            foreach (var key in keys)
                _previewGrid.Add(BuildTile(key, set.Find(key)));
        }

        private static VisualElement BuildTile(string key, Sprite sprite)
        {
            var tile = new VisualElement();
            tile.AddToClassList("ip-tile");
            tile.EnableInClassList("ip-tile--missing", sprite == null);

            var icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("ip-tile__icon");
            tile.Add(icon);
            tile.Add(MakeLabel(key, "ip-tile__label"));

            return tile;
        }


        private void RunGeneration()
        {
            var result = InputPromptGenerator.Generate(_settings);
            _report.value = string.IsNullOrEmpty(result.m_report) ? "—" : result.m_report.TrimEnd();
            RefreshAll();
        }

        private void PingDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(_settings.DatabasePath);
            if (database == null)
            {
                _report.value = $"No database at {_settings.DatabasePath}. Run Generate.";
                return;
            }

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
        }

        private void ApplyRuntimeToDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<InputPromptDatabase>(_settings.DatabasePath);
            if (database == null)
                return;

            database.m_pointerMotionSwitchesStyle = _settings.m_pointerMotionSwitchesStyle;
            database.m_useKeyboardLayoutLabels = _settings.m_useKeyboardLayoutLabels;
            database.m_preferExactDevice = _settings.m_preferExactDevice;
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
        }

        private void ForcePreviewStyle()
        {
            InputPromptService.EditorPreviewStyle = _previewStyle;
            InputPromptService.Refresh();
            SceneView.RepaintAll();
        }

        private static void ClearPreviewStyle()
        {
            InputPromptService.EditorPreviewStyle = null;
            InputPromptService.Refresh();
            SceneView.RepaintAll();
        }


        private static string[] StyleNames() => Enum.GetNames(typeof(InputDeviceStyle));

        private static Label MakeLabel(string content, params string[] classes)
        {
            var label = new Label(content);
            foreach (var className in classes)
                label.AddToClassList(className);
            return label;
        }

        private static Button MakeButton(string emoji, string fallback, string content, Action action,
            params string[] classes)
        {
            var button = new Button(action);
            foreach (var className in classes)
                button.AddToClassList(className);

            button.Add(DashboardGlyphs.Icon(emoji, fallback));
            button.Add(MakeLabel(content, "ip-button__text"));
            return button;
        }

        private static Button MakeTextureButton(string iconName, string content, Action action,
            params string[] classes)
        {
            var button = new Button(action);
            foreach (var className in classes)
                button.AddToClassList(className);

            var texture = EditorGUIUtility.IconContent(iconName)?.image
                          ?? EditorGUIUtility.IconContent(iconName.Replace("d_", string.Empty))?.image;
            if (texture != null)
            {
                var icon = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
                icon.AddToClassList("ip-glyph");
                icon.AddToClassList("ip-glyph--texture");
                button.Add(icon);
            }

            button.Add(MakeLabel(content, "ip-button__text"));
            return button;
        }

        private static VisualElement MakeRow(params string[] classes)
        {
            var row = new VisualElement();
            row.AddToClassList("ip-row");
            foreach (var className in classes)
                row.AddToClassList(className);
            return row;
        }

        private static VisualElement MakeCard(string emoji, string fallback, string title, string hint,
            out VisualElement content)
        {
            var card = new VisualElement();
            card.AddToClassList("ip-card");

            var header = new VisualElement();
            header.AddToClassList("ip-card__header");
            header.Add(DashboardGlyphs.Icon(emoji, fallback));
            header.Add(MakeLabel(title, "ip-card__title"));
            header.Add(MakeLabel(hint, "ip-card__hint"));
            card.Add(header);

            content = new VisualElement();
            content.AddToClassList("ip-card__content");
            card.Add(content);

            return card;
        }

        private static VisualElement MakeFolderField(string label, string value, Action<string> onChanged)
        {
            var row = MakeRow();

            var field = new TextField(label) { value = value, isDelayed = true };
            field.AddToClassList("ip-field");
            field.AddToClassList("ip-row__grow");
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(field);

            row.Add(MakeButton("📂", "…", string.Empty, () =>
            {
                var picked = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);
                if (string.IsNullOrEmpty(picked) || !picked.StartsWith(Application.dataPath))
                    return;

                field.value = "Assets" + picked[Application.dataPath.Length..].Replace('\\', '/');
            }, "ip-button", "ip-button--ghost"));

            return row;
        }

        private static DropdownField MakeDropdown(string label, IEnumerable<string> choices, string value,
            Action<string> onChanged)
        {
            var list = choices.ToList();
            var index = Mathf.Max(0, list.IndexOf(value));

            var field = new DropdownField(label, list, index);
            field.AddToClassList("ip-field");
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return field;
        }

        private static Toggle MakeToggle(string label, bool value, Action<bool> onChanged)
        {
            var field = new Toggle(label) { value = value };
            field.AddToClassList("ip-field");
            field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return field;
        }

        #endregion
    }
}
