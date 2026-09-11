using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Tests
{
    public class SpriteAssetTests
    {
        #region Private and Protected

        private const string GeneratedPath =
            "Assets/_/Database/InputPrompts/SO_InputPromptSprites_KeyboardMouse.asset";

        #endregion


        #region Main API

        [Test]
        public void Slashes_become_underscores_in_sprite_names()
        {
            Assert.That(ControlPath.ToSpriteName("leftstick/up"), Is.EqualTo("leftstick_up"));
            Assert.That(ControlPath.ToSpriteName("space"), Is.EqualTo("space"));
            Assert.That(ControlPath.ToSpriteName(null), Is.Null);
        }

        [Test]
        public void The_generated_atlas_resolves_a_key_by_name()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(GeneratedPath);
            if (asset == null)
                Assert.Ignore("No generated sprite asset in this project, run the dashboard first.");

            Assert.That(asset.spriteSheet, Is.Not.Null, "the atlas texture must be attached");
            Assert.That(asset.material, Is.Not.Null, "TextMeshPro needs a material to draw the sprites");
            Assert.That(asset.GetSpriteIndexFromName("space"), Is.GreaterThanOrEqualTo(0));
            Assert.That(asset.GetSpriteIndexFromName("leftbutton"), Is.GreaterThanOrEqualTo(0));
            Assert.That(asset.GetSpriteIndexFromName("scroll_y"), Is.GreaterThanOrEqualTo(0),
                "a key holding a slash must be reachable through its underscore name");
        }

        [Test]
        public void Every_character_points_at_a_glyph_of_the_atlas()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(GeneratedPath);
            if (asset == null)
                Assert.Ignore("No generated sprite asset in this project, run the dashboard first.");

            foreach (var character in asset.spriteCharacterTable)
            {
                Assert.That(character.glyphIndex, Is.LessThan((uint)asset.spriteGlyphTable.Count),
                    $"{character.name} points outside the glyph table");
            }
        }

        #endregion
    }
}
