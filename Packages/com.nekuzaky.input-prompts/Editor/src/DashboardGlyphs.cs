using System;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace InputPrompts.Editor
{
    /// <summary>
    /// The editor UI font carries no emoji glyph, so an emoji written straight into a Label shows up as
    /// an empty box. This borrows the system emoji font when there is one, and falls back to symbols the
    /// default font is guaranteed to have. Every glyph lives in its own Label: the emoji font has no
    /// latin glyphs, so it must never be applied to a label that also holds words.
    /// </summary>
    public static class DashboardGlyphs
    {
        #region Private and Protected

        /// <summary>
        /// Emoji font of the current platform. Asking for the other ones only logs "font file not
        /// found" errors, so the list is narrowed before anything is loaded.
        /// </summary>
        private static string SystemEmojiFont => Application.platform switch
        {
            RuntimePlatform.WindowsEditor => "Segoe UI Emoji",
            RuntimePlatform.OSXEditor => "Apple Color Emoji",
            _ => "Noto Color Emoji",
        };

        /// <summary>🎮, the glyph decides whether the whole set is usable.</summary>
        private const uint ProbeCharacter = 0x1F3AE;

        private static FontAsset _emojiFont;
        private static bool _isProbed;

        #endregion


        #region Public

        /// <summary>True when a system font able to draw emoji was found.</summary>
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

        /// <summary>
        /// A one glyph Label: the emoji when the machine can draw it, <paramref name="fallback"/> otherwise.
        /// </summary>
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

            // The font being installed is not enough: TextCore cannot rasterize colour glyph formats
            // (CBDT, COLR), which is exactly what these fonts store. The glyph check decides.
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
