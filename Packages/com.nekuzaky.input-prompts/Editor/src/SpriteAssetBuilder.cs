using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace Nekuzaky.InputPrompts.Editor
{
    public static class SpriteAssetBuilder
    {
        #region Private and Protected

        private const int Padding = 2;
        private const int MaxAtlasSize = 4096;
        private const float BearingRatio = 0.8f;
        private const string AssetVersion = "1.1.0";

        #endregion


        #region Main API

        public static TMP_SpriteAsset Build(InputPromptSet set, string path,
            AtlasCompression compression = AtlasCompression.None)
        {
            if (set == null || set.Entries.Count == 0)
                return null;

            var sprites = new List<Sprite>();
            var keysBySprite = new Dictionary<Sprite, List<string>>();

            foreach (var entry in set.Entries)
            {
                if (entry.m_sprite == null || string.IsNullOrEmpty(entry.m_key))
                    continue;

                if (!keysBySprite.TryGetValue(entry.m_sprite, out var keys))
                {
                    keys = new List<string>();
                    keysBySprite[entry.m_sprite] = keys;
                    sprites.Add(entry.m_sprite);
                }

                keys.Add(entry.m_key);
            }

            if (sprites.Count == 0)
                return null;

            var textures = new Texture2D[sprites.Count];
            for (var i = 0; i < sprites.Count; i++)
                textures[i] = ReadTexture(sprites[i]);

            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var rects = atlas.PackTextures(textures, Padding, MaxAtlasSize, false);

            foreach (var texture in textures)
                Object.DestroyImmediate(texture);

            Compress(atlas, compression);

            var asset = LoadOrCreate(path);
            FillTables(asset, sprites, keysBySprite, rects, atlas);
            Attach(asset, atlas, path);
            StampVersion(asset);

            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        #endregion


        #region Tools and Utilities

        private static void Compress(Texture2D atlas, AtlasCompression compression)
        {
            switch (compression)
            {
                case AtlasCompression.Desktop:
                    EditorUtility.CompressTexture(atlas, TextureFormat.BC7, TextureCompressionQuality.Normal);
                    break;

                case AtlasCompression.Mobile:
                    EditorUtility.CompressTexture(atlas, TextureFormat.ASTC_6x6, TextureCompressionQuality.Normal);
                    break;
            }
        }

        private static Texture2D ReadTexture(Sprite sprite)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var assetPath = AssetDatabase.GetAssetPath(sprite);

            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
                texture.LoadImage(File.ReadAllBytes(assetPath));

            return texture;
        }

        private static TMP_SpriteAsset LoadOrCreate(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
            if (asset != null)
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub != asset)
                        Object.DestroyImmediate(sub, true);
                }

                return asset;
            }

            asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void FillTables(TMP_SpriteAsset asset, List<Sprite> sprites,
            Dictionary<Sprite, List<string>> keysBySprite, Rect[] rects, Texture2D atlas)
        {
            var glyphs = new List<TMP_SpriteGlyph>(sprites.Count);
            var characters = new List<TMP_SpriteCharacter>(sprites.Count);

            for (var i = 0; i < sprites.Count; i++)
            {
                var rect = rects[i];
                var width = rect.width * atlas.width;
                var height = rect.height * atlas.height;

                var metrics = new GlyphMetrics(width, height, 0f, height * BearingRatio, width);
                var glyphRect = new GlyphRect(
                    Mathf.RoundToInt(rect.x * atlas.width),
                    Mathf.RoundToInt(rect.y * atlas.height),
                    Mathf.RoundToInt(width),
                    Mathf.RoundToInt(height));

                var glyph = new TMP_SpriteGlyph((uint)i, metrics, glyphRect, 1f, 0) { sprite = sprites[i] };
                glyphs.Add(glyph);

                foreach (var key in keysBySprite[sprites[i]])
                {
                    characters.Add(new TMP_SpriteCharacter(0, asset, glyph)
                    {
                        name = ControlPath.ToSpriteName(key),
                        scale = 1f,
                    });
                }
            }

            asset.spriteGlyphTable.Clear();
            asset.spriteGlyphTable.AddRange(glyphs);
            asset.spriteCharacterTable.Clear();
            asset.spriteCharacterTable.AddRange(characters);
        }

        private static void StampVersion(TMP_SpriteAsset asset)
        {
            asset.spriteInfoList ??= new List<TMP_Sprite>();

            var serialized = new SerializedObject(asset);
            var version = serialized.FindProperty("m_Version");
            if (version == null || !string.IsNullOrEmpty(version.stringValue))
                return;

            version.stringValue = AssetVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Attach(TMP_SpriteAsset asset, Texture2D atlas, string path)
        {
            atlas.name = $"{Path.GetFileNameWithoutExtension(path)} Atlas";
            atlas.Apply(false, true);
            asset.spriteSheet = atlas;
            AssetDatabase.AddObjectToAsset(atlas, asset);

            ShaderUtilities.GetShaderPropertyIDs();
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Sprite Material" };
            material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            asset.material = material;
            AssetDatabase.AddObjectToAsset(material, asset);
        }

        #endregion
    }
}
