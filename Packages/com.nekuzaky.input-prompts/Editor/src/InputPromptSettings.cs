using System.Collections.Generic;
using UnityEditor;

namespace InputPrompts.Editor
{
    /// <summary>Icon resolution shipped by the Kenney pack.</summary>
    public enum IconVariant
    {
        Default = 0,
        Double = 1,
    }

    /// <summary>
    /// Every dashboard setting, stored in ProjectSettings so the whole team shares the same import
    /// configuration without it landing in the Assets folder.
    /// </summary>
    [FilePath("ProjectSettings/InputPromptsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class InputPromptSettings : ScriptableSingleton<InputPromptSettings>
    {
        #region Public

        public string m_packFolder = "Assets/Sprites/input-prompts";
        public string m_outputFolder = "Assets/_/Database/InputPrompts";
        public IconVariant m_variant = IconVariant.Default;
        public bool m_outlineIcons;
        public bool m_coloredFaceButtons = true;
        public bool m_applyTextureSettings = true;

        public InputDeviceStyle m_gamepadFallbackStyle = InputDeviceStyle.Xbox;
        public InputDeviceStyle m_defaultStyle = InputDeviceStyle.KeyboardMouse;

        public bool m_pointerMotionSwitchesStyle;
        public bool m_useKeyboardLayoutLabels = true;

        public List<InputDeviceStyle> m_disabledStyles = new();

        #endregion


        #region Main API

        /// <summary>The database has to sit in a Resources folder for the runtime to load it.</summary>
        public string DatabasePath => $"{m_outputFolder}/Resources/{InputPromptGenerator.DatabaseName}.asset";

        public string FolderFor(InputDeviceStyle style) =>
            $"{m_packFolder}/{KenneyNameTable.FolderFor(style)}/{m_variant}";

        public string SetPathFor(InputDeviceStyle style) =>
            $"{m_outputFolder}/{InputPromptGenerator.SetPrefix}{style}.asset";

        public bool IsStyleEnabled(InputDeviceStyle style) => !m_disabledStyles.Contains(style);

        public void SetStyleEnabled(InputDeviceStyle style, bool isEnabled)
        {
            if (isEnabled)
                m_disabledStyles.Remove(style);
            else if (!m_disabledStyles.Contains(style))
                m_disabledStyles.Add(style);

            SaveSettings();
        }

        public void SaveSettings() => Save(saveAsText: true);

        #endregion
    }
}
