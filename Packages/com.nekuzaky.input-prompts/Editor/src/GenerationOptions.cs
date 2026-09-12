using System;
using System.Collections.Generic;

namespace Nekuzaky.InputPrompts.Editor
{
    public class GenerationOptions
    {
        #region Public

        public string m_packFolder = "Assets/Sprites/input-prompts";
        public string m_outputFolder = "Assets/_/Database/InputPrompts";
        public IconVariant m_variant = IconVariant.Default;
        public bool m_outlineIcons;
        public bool m_coloredFaceButtons = true;
        public bool m_applyTextureSettings = true;
        public bool m_generateSpriteAssets = true;
        public AtlasCompression m_atlasCompression = AtlasCompression.None;

        public InputDeviceStyle m_gamepadFallbackStyle = InputDeviceStyle.Xbox;
        public InputDeviceStyle m_defaultStyle = InputDeviceStyle.KeyboardMouse;
        public bool m_pointerMotionSwitchesStyle;
        public bool m_useKeyboardLayoutLabels = true;
        public bool m_preferExactDevice;

        public InputPromptPackDefinition m_definition;
        public ICollection<InputDeviceStyle> m_disabledStyles = Array.Empty<InputDeviceStyle>();

        public string DatabasePath => InputPromptGenerator.DatabasePathIn(m_outputFolder);

        #endregion


        #region Main API

        public static GenerationOptions From(InputPromptSettings settings) => new()
        {
            m_packFolder = settings.m_packFolder,
            m_outputFolder = settings.m_outputFolder,
            m_variant = settings.m_variant,
            m_outlineIcons = settings.m_outlineIcons,
            m_coloredFaceButtons = settings.m_coloredFaceButtons,
            m_applyTextureSettings = settings.m_applyTextureSettings,
            m_generateSpriteAssets = settings.m_generateSpriteAssets,
            m_atlasCompression = settings.m_atlasCompression,
            m_gamepadFallbackStyle = settings.m_gamepadFallbackStyle,
            m_defaultStyle = settings.m_defaultStyle,
            m_pointerMotionSwitchesStyle = settings.m_pointerMotionSwitchesStyle,
            m_useKeyboardLayoutLabels = settings.m_useKeyboardLayoutLabels,
            m_preferExactDevice = settings.m_preferExactDevice,
            m_definition = settings.m_packDefinition,
            m_disabledStyles = settings.m_disabledStyles,
        };

        public bool IsStyleEnabled(InputDeviceStyle style) => !m_disabledStyles.Contains(style);

        public string SetPathFor(InputDeviceStyle style) => InputPromptGenerator.SetPathIn(m_outputFolder, style);

        public string SpriteAssetPathFor(InputDeviceStyle style) =>
            InputPromptGenerator.SpriteAssetPathIn(m_outputFolder, style);

        #endregion
    }

    public enum AtlasCompression
    {
        None = 0,
        Desktop = 1,
        Mobile = 2,
    }
}
