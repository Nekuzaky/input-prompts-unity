using System.Collections.Generic;
using Nekuzaky.InputPrompts.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Tests
{
    public class PackDefinitionTests
    {
        #region Private and Protected

        private const string PackFolder = "Assets/Sprites/input-prompts";
        private const string OutputFolder = "Assets/InputPromptsTestOutput";

        private readonly List<Object> _created = new();

        #endregion


        #region Unity API

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }

            _created.Clear();
            AssetDatabase.DeleteAsset(OutputFolder);
        }

        #endregion


        #region Main API

        [Test]
        public void A_custom_definition_imports_its_own_keys_from_its_own_files()
        {
            RequirePack();

            var definition = MakeDefinition(InputDeviceStyle.KeyboardMouse, "Keyboard & Mouse",
                ("jump", "keyboard_space"), ("interact", "keyboard_e"), ("broken", "not_a_file"));

            var result = InputPromptGenerator.Generate(OptionsFor(definition));
            var set = AssetDatabase.LoadAssetAtPath<InputPromptSet>(
                InputPromptGenerator.SetPathIn(OutputFolder, InputDeviceStyle.KeyboardMouse));

            Assert.That(set, Is.Not.Null);
            Assert.That(set.Find("jump"), Is.Not.Null, "a studio defined key resolves through its mapped file");
            Assert.That(set.Find("interact"), Is.Not.Null);
            Assert.That(set.Contains("space"), Is.False, "only the definition's own mappings are imported");
            Assert.That(result.m_missing, Is.EqualTo(1));
            Assert.That(result.m_report, Does.Contain("broken -> not_a_file.png"));
        }

        [Test]
        public void A_disabled_family_is_left_out_of_the_database()
        {
            RequirePack();

            var definition = MakeDefinition(InputDeviceStyle.KeyboardMouse, "Keyboard & Mouse", ("jump", "keyboard_space"));
            definition.m_families.Add(new InputPromptPackDefinition.Family
            {
                m_style = InputDeviceStyle.Xbox,
                m_folder = "Xbox Series",
                m_layouts = new[] { "XInputController" },
                m_mappings = new List<InputPromptPackDefinition.Mapping>
                {
                    new() { m_key = "buttonsouth", m_file = "xbox_button_a" },
                },
            });

            var options = OptionsFor(definition);
            options.m_disabledStyles = new[] { InputDeviceStyle.Xbox };

            var result = InputPromptGenerator.Generate(options);

            Assert.That(result.m_sets, Is.EqualTo(1));
            Assert.That(result.m_database.m_sets, Has.None.Matches<InputPromptSet>(set => set.m_style == InputDeviceStyle.Xbox));
            Assert.That(result.m_report, Does.Contain("[off] Xbox"));
        }

        [Test]
        public void Desktop_compression_writes_a_BC7_atlas()
        {
            RequirePack();

            var definition = MakeDefinition(InputDeviceStyle.KeyboardMouse, "Keyboard & Mouse",
                ("jump", "keyboard_space"), ("interact", "keyboard_e"));

            var options = OptionsFor(definition);
            options.m_generateSpriteAssets = true;
            options.m_atlasCompression = AtlasCompression.Desktop;

            InputPromptGenerator.Generate(options);

            var atlas = AssetDatabase.LoadAssetAtPath<TMPro.TMP_SpriteAsset>(
                InputPromptGenerator.SpriteAssetPathIn(OutputFolder, InputDeviceStyle.KeyboardMouse));

            Assert.That(atlas, Is.Not.Null);
            Assert.That(((Texture2D)atlas.spriteSheet).format, Is.EqualTo(TextureFormat.BC7));
            Assert.That(atlas.GetSpriteIndexFromName("jump"), Is.GreaterThanOrEqualTo(0),
                "compression must not break the name lookup");
        }

        [Test]
        public void The_built_in_table_carries_over_into_a_definition()
        {
            var definition = KenneyNameTable.CreateDefinition(coloredFaceButtons: true);
            _created.Add(definition);

            var xbox = definition.Find(InputDeviceStyle.Xbox);
            var nintendo = definition.Find(InputDeviceStyle.Switch);

            Assert.That(xbox.m_mappings, Has.Some.Matches<InputPromptPackDefinition.Mapping>(
                mapping => mapping.m_key == "buttonsouth" && mapping.m_file == "xbox_button_color_a"));
            Assert.That(nintendo.m_mappings, Has.Some.Matches<InputPromptPackDefinition.Mapping>(
                mapping => mapping.m_key == "buttonsouth" && mapping.m_file == "switch_button_b"),
                "the Nintendo face swap must survive the export");
            Assert.That(definition.Find(InputDeviceStyle.Generic).m_fallsBackToGeneric, Is.False);
            Assert.That(xbox.m_fallsBackToGeneric, Is.True);
        }

        #endregion


        #region Tools and Utilities

        private static void RequirePack()
        {
            if (!AssetDatabase.IsValidFolder(PackFolder))
                Assert.Ignore("The Kenney pack is not in this project.");
        }

        private InputPromptPackDefinition MakeDefinition(InputDeviceStyle style, string folder,
            params (string key, string file)[] mappings)
        {
            var definition = ScriptableObject.CreateInstance<InputPromptPackDefinition>();
            _created.Add(definition);

            var family = new InputPromptPackDefinition.Family
            {
                m_style = style,
                m_folder = folder,
                m_layouts = new[] { "Keyboard", "Mouse" },
                m_fallsBackToGeneric = false,
            };

            foreach (var (key, file) in mappings)
                family.m_mappings.Add(new InputPromptPackDefinition.Mapping { m_key = key, m_file = file });

            definition.m_families.Add(family);
            return definition;
        }

        private static GenerationOptions OptionsFor(InputPromptPackDefinition definition) => new()
        {
            m_packFolder = PackFolder,
            m_outputFolder = OutputFolder,
            m_definition = definition,
            m_applyTextureSettings = false,
            m_generateSpriteAssets = false,
        };

        #endregion
    }
}
