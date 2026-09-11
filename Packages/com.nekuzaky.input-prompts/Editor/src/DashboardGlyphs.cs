using System;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Nekuzaky.InputPrompts.Editor
{
    public static class DashboardGlyphs
    {
        #region Private and Protected

        private static string SystemEmojiFont => Application.platform switch
        {
            RuntimePlatform.WindowsEditor => "Segoe UI Emoji",
            RuntimePlatform.OSXEditor => "Apple Color Emoji",
            _ => "Noto Color Emoji",
        };

        private const uint ProbeCharacter = 0x1F3AE;

        private static FontAsset _emojiFont;
        private static bool _isProbed;

        #endregion


        #region Public

        public static bool HasEmoji
        {
            get
            {
                Probe();
                return _emojiFont != null;
            }
        }

        #endregion


        #region Main API

        public static Label Icon(string emoji, string fallback, params string[] classes)
        {
            var label = new Label(HasEmoji ? emoji : fallback);
            label.AddToClassList("ip-glyph");

            foreach (var className in classes)
                label.AddToClassList(className);

            if (HasEmoji)
                label.style.unityFontDefinition = new StyleFontDefinition(_emojiFont);

            return label;
        }

        #endregion


        #region Tools and Utilities

        private static void Probe()
        {
            if (_isProbed)
                return;

            _isProbed = true;

            var fontName = SystemEmojiFont;
            if (!Font.GetOSInstalledFontNames().Any(installed =>
                    installed.StartsWith(fontName, StringComparison.OrdinalIgnoreCase)))
                return;

            var osFont = Font.CreateDynamicFontFromOSFont(fontName, 16);
            if (osFont == null)
                return;

            osFont.hideFlags = HideFlags.DontSave;
            var asset = FontAsset.CreateFontAsset(osFont);
            if (asset == null)
                return;

            asset.hideFlags = HideFlags.DontSave;
            if (asset.HasCharacter(ProbeCharacter))
                _emojiFont = asset;
            else
                UnityEngine.Object.DestroyImmediate(asset);
        }

        #endregion
    }
}
