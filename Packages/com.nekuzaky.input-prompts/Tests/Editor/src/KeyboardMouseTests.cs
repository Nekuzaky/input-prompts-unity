using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts.Tests
{
    /// <summary>
    /// Keyboard and mouse are two devices but a single prompt family: switching between them must not
    /// look like switching platform.
    /// </summary>
    public class KeyboardMouseTests : InputTestFixture
    {
        #region Private and Protected

        private Sprite _keySprite;
        private Sprite _mouseSprite;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _keySprite = MakeSprite();
            _mouseSprite = MakeSprite();

            var set = ScriptableObject.CreateInstance<InputPromptSet>();
            set.m_style = InputDeviceStyle.KeyboardMouse;
            set.m_layouts = new[] { "Keyboard", "Mouse" };
            set.SetEntries(new List<InputPromptSet.Entry>
            {
                new() { m_key = "space", m_sprite = _keySprite },
                new() { m_key = "leftbutton", m_sprite = _mouseSprite },
            });

            var database = ScriptableObject.CreateInstance<InputPromptDatabase>();
            database.m_sets.Add(set);

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
        public void Switching_between_keyboard_and_mouse_keeps_one_style()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            var styles = new List<InputDeviceStyle>();
            InputPromptService.StyleChanged += styles.Add;

            Press(keyboard.spaceKey);
            Press(mouse.leftButton);
            Press(keyboard.aKey);

            InputPromptService.StyleChanged -= styles.Add;

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
            Assert.That(styles, Is.Empty, "keyboard and mouse are the same family: no style change expected");
        }

        [Test]
        public void A_keyboard_binding_keeps_its_icon_after_a_mouse_click()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            var action = new InputAction("Jump");
            action.AddBinding("<Keyboard>/space");

            Press(keyboard.spaceKey);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_keySprite));

            Press(mouse.leftButton);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_keySprite),
                "the mouse becoming active must not drop the keyboard binding");

            action.Dispose();
        }

        [Test]
        public void An_action_bound_to_both_keeps_one_icon_whichever_is_used()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            var action = new InputAction("Fire");
            action.AddBinding("<Mouse>/leftButton");
            action.AddBinding("<Keyboard>/space");

            Press(keyboard.spaceKey);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_mouseSprite),
                "the first binding of the family wins, typing must not flip the prompt");

            Press(mouse.leftButton);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_mouseSprite));

            action.Dispose();
        }

        [Test]
        public void Prefer_exact_device_makes_the_icon_follow_the_last_input()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();

            var action = new InputAction("Fire");
            action.AddBinding("<Mouse>/leftButton");
            action.AddBinding("<Keyboard>/space");

            InputPromptService.PreferExactDevice = true;
            try
            {
                Press(keyboard.spaceKey);
                Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_keySprite));

                Press(mouse.leftButton);
                Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_mouseSprite));
            }
            finally
            {
                InputPromptService.PreferExactDevice = false;
            }

            action.Dispose();
        }

        #endregion


        #region Tools and Utilities

        private static Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        #endregion
    }
}
