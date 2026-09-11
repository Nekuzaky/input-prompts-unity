using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.InputPrompts.Editor
{
    public static class DashboardGlyphs
    {
        #region Public

        public const string Controller = "controller";
        public const string Source = "folder2-open";
        public const string Output = "hdd";
        public const string Runtime = "gear";
        public const string Devices = "sliders";
        public const string Preview = "palette";
        public const string Report = "list-ul";
        public const string Demo = "play-btn";
        public const string Generate = "lightning-charge-fill";
        public const string Ping = "search";
        public const string Reload = "arrow-clockwise";
        public const string Eye = "eye";
        public const string Follow = "arrow-repeat";
        public const string Docs = "book";
        public const string Coffee = "cup-hot-fill";
        public const string Browse = "three-dots";
        public const string Keyboard = "keyboard";
        public const string Mouse = "mouse";
        public const string Dot = "circle-fill";
        public const string Ring = "circle";

        #endregion


        #region Private and Protected

        private const string IconFolder = "Packages/com.nekuzaky.input-prompts/Editor/UI/Icons";

        private static readonly Dictionary<string, VectorImage> Cache = new();

        #endregion


        #region Main API

        public static Image Icon(string iconName, params string[] classes)
        {
            var icon = new Image { vectorImage = Load(iconName), scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("ip-glyph");

            foreach (var className in classes)
                icon.AddToClassList(className);

            return icon;
        }

        public static Image Icon(string iconName, Color tint, params string[] classes)
        {
            var icon = Icon(iconName, classes);
            icon.tintColor = tint;
            return icon;
        }

        #endregion


        #region Tools and Utilities

        private static VectorImage Load(string iconName)
        {
            if (Cache.TryGetValue(iconName, out var cached) && cached != null)
                return cached;

            var image = AssetDatabase.LoadAssetAtPath<VectorImage>($"{IconFolder}/{iconName}.svg");
            Cache[iconName] = image;
            return image;
        }

        #endregion
    }
}
