using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

namespace Nekuzaky.InputPrompts.Tests
{
    public class ToolkitElementTests : InputTestFixture
    {
        #region Private and Protected

        private Sprite _keySprite;
        private Sprite _padSprite;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _keySprite = MakeSprite();
            _padSprite = MakeSprite();

            var database = ScriptableObject.CreateInstance<InputPromptDatabase>();
            database.m_sets.Add(MakeSet(InputDeviceStyle.KeyboardMouse, new[] { "Keyboard", "Mouse" },
                ("space", _keySprite)));
            database.m_sets.Add(MakeSet(InputDeviceStyle.Xbox, new[] { "XInputController" },
                ("buttonsouth", _padSprite)));

            InputPromptService.Shutdown();
            InputPromptService.Database = database;
            InputPromptService.Initialize();
        }

        public override void TearDown()
        {
            InputPromptService.Shutdown();
            InputPromptService.Database = null;
            base.TearDown();
        }

        #endregion


        #region Main API

        [Test]
        public void The_element_draws_the_icon_of_the_device_in_use()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var jump = new InputAction("Jump");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            var element = new InputPromptElement { Action = jump };

            Press(keyboard.spaceKey);
            element.Refresh();
            Assert.That(element.sprite, Is.SameAs(_keySprite));

            Press(gamepad.buttonSouth);
            element.Refresh();
            Assert.That(element.sprite, Is.SameAs(_padSprite));

            jump.Dispose();
        }

        [Test]
        public void An_unbound_element_hides_itself()
        {
            InputSystem.AddDevice<Keyboard>();
            var unbound = new InputAction("Unbound");

            var element = new InputPromptElement { Action = unbound };

            Assert.That(element.visible, Is.False);

            element.HideWhenUnbound = false;
            Assert.That(element.visible, Is.True, "turning the option off keeps the element in place");

            unbound.Dispose();
        }

        [Test]
        public void The_element_can_follow_a_single_player()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            var playerOne = new InputPromptContext(device => device == keyboard);
            InputPromptService.Register(playerOne);

            var jump = new InputAction("Jump");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            var element = new InputPromptElement { Context = playerOne, Action = jump };

            Press(keyboard.spaceKey);
            Press(gamepad.buttonSouth);
            element.Refresh();

            Assert.That(element.sprite, Is.SameAs(_keySprite), "player one never touched the gamepad");

            InputPromptService.Unregister(playerOne);
            jump.Dispose();
        }

        #endregion


        #region Tools and Utilities

        private static InputPromptSet MakeSet(InputDeviceStyle style, string[] layouts,
            params (string key, Sprite sprite)[] entries)
        {
            var set = ScriptableObject.CreateInstance<InputPromptSet>();
            set.m_style = style;
            set.m_layouts = layouts;

            var list = new List<InputPromptSet.Entry>();
            foreach (var (key, sprite) in entries)
                list.Add(new InputPromptSet.Entry { m_key = key, m_sprite = sprite });

            set.SetEntries(list);
            return set;
        }

        private static Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        #endregion
    }
}
