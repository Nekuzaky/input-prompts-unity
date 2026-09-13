using System.Collections.Generic;
using System.Linq;
using Nekuzaky.InputPrompts.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts.Tests
{
    public class ValidatorTests
    {
        #region Private and Protected

        private readonly List<Object> _created = new();

        private InputPromptDatabase _database;
        private InputActionAsset _actions;
        private InputActionMap _map;

        #endregion


        #region Unity API

        [SetUp]
        public void SetUp()
        {
            var sprite = Track(Sprite.Create(new Texture2D(4, 4), new Rect(0f, 0f, 4f, 4f), Vector2.zero));

            _database = Track(ScriptableObject.CreateInstance<InputPromptDatabase>());
            _database.m_sets.Add(MakeSet(InputDeviceStyle.KeyboardMouse, new[] { "Keyboard", "Mouse" },
                sprite, "space", "w", "a", "s", "d"));
            _database.m_sets.Add(MakeSet(InputDeviceStyle.Xbox, new[] { "XInputController" },
                sprite, "buttonsouth", "leftstick"));

            _actions = Track(ScriptableObject.CreateInstance<InputActionAsset>());
            _map = _actions.AddActionMap("Player");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        #endregion


        #region Main API

        [Test]
        public void A_fully_covered_asset_raises_nothing()
        {
            var jump = _map.AddAction("Jump", binding: "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            Assert.That(InputPromptValidator.Validate(_actions, _database), Is.Empty);
        }

        [Test]
        public void A_key_with_no_icon_is_reported_with_its_family()
        {
            _map.AddAction("Debug", binding: "<Keyboard>/f13");

            var issue = InputPromptValidator.Validate(_actions, _database).Single();

            Assert.That(issue.m_kind, Is.EqualTo(InputPromptValidator.IssueKind.MissingIcon));
            Assert.That(issue.m_style, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
            Assert.That(issue.m_key, Is.EqualTo("f13"));
            Assert.That(issue.m_action, Is.EqualTo("Player/Debug"));
        }

        [Test]
        public void A_generic_gamepad_binding_is_checked_against_every_family_it_reaches()
        {
            _database.m_sets.Add(MakeSet(InputDeviceStyle.Generic, new[] { "Gamepad" },
                _database.m_sets[0].Entries[0].m_sprite, "leftstick"));

            _map.AddAction("Jump", binding: "<Gamepad>/buttonSouth");

            var issues = InputPromptValidator.Validate(_actions, _database);

            Assert.That(issues.Select(issue => issue.m_style), Is.EquivalentTo(new[] { InputDeviceStyle.Generic }),
                "Xbox has the icon, Generic does not: only Generic is reported");
        }

        [Test]
        public void A_catch_all_set_behind_a_gamepad_fallback_is_not_reported()
        {
            var generic = MakeSet(InputDeviceStyle.Generic, new[] { "Gamepad" },
                _database.m_sets[0].Entries[0].m_sprite, "leftstick");
            _database.m_sets.Add(generic);
            _database.m_gamepadFallback = _database.m_sets[1];

            _map.AddAction("Jump", binding: "<Gamepad>/buttonSouth");

            Assert.That(InputPromptValidator.Validate(_actions, _database), Is.Empty,
                "unknown gamepads use the Xbox fallback, so the Generic set is never shown for this binding");
        }

        [Test]
        public void A_usage_path_is_reported_as_unresolvable()
        {
            _map.AddAction("Submit", binding: "<Gamepad>/{Submit}");

            var issue = InputPromptValidator.Validate(_actions, _database).Single();

            Assert.That(issue.m_kind, Is.EqualTo(InputPromptValidator.IssueKind.Unresolvable));
        }

        [Test]
        public void A_device_no_family_covers_is_reported()
        {
            _map.AddAction("Fly", binding: "<Joystick>/trigger");

            var issue = InputPromptValidator.Validate(_actions, _database).Single();

            Assert.That(issue.m_kind, Is.EqualTo(InputPromptValidator.IssueKind.NoFamily));
        }

        [Test]
        public void Composite_parts_are_checked_one_by_one()
        {
            _map.AddAction("Move").AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/f14");

            var issue = InputPromptValidator.Validate(_actions, _database).Single();

            Assert.That(issue.m_part, Is.EqualTo("Right"));
            Assert.That(issue.m_key, Is.EqualTo("f14"));
        }

        #endregion


        #region Tools and Utilities

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private InputPromptSet MakeSet(InputDeviceStyle style, string[] layouts, Sprite sprite, params string[] keys)
        {
            var set = Track(ScriptableObject.CreateInstance<InputPromptSet>());
            set.m_style = style;
            set.m_layouts = layouts;
            set.SetEntries(keys.Select(key => new InputPromptSet.Entry { m_key = key, m_sprite = sprite }));
            return set;
        }

        #endregion
    }
}
