using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

namespace Nekuzaky.InputPrompts.Tests
{
    /// <summary>
    /// The promise of the package: prompts follow the device the player is actually using, with no
    /// polling and no manual checks. These tests drive real devices through the Input System and read
    /// back what the service resolves.
    /// </summary>
    public class DeviceDetectionTests : InputTestFixture
    {
        #region Private and Protected

        private InputPromptDatabase _database;
        private Sprite _keyboardSprite;
        private Sprite _gamepadSprite;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _keyboardSprite = MakeSprite();
            _gamepadSprite = MakeSprite();

            var keyboardSet = MakeSet(InputDeviceStyle.KeyboardMouse, new[] { "Keyboard", "Mouse" },
                ("space", _keyboardSprite));
            var xboxSet = MakeSet(InputDeviceStyle.Xbox, new[] { "XInputController" },
                ("buttonsouth", _gamepadSprite));

            _database = ScriptableObject.CreateInstance<InputPromptDatabase>();
            _database.m_sets.Add(keyboardSet);
            _database.m_sets.Add(xboxSet);
            _database.m_gamepadFallback = xboxSet;
            _database.m_defaultStyle = InputDeviceStyle.KeyboardMouse;

            // The fixture rebuilds the Input System around every test, which drops the service
            // subscriptions: re-arm them before each one.
            InputPromptService.Shutdown();
            InputPromptService.Database = _database;
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
        public void Pressing_a_key_selects_the_keyboard_style()
        {
            var gamepad = InputSystem.AddDevice<XInputController>();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(gamepad.buttonSouth);
            Press(keyboard.spaceKey);

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
            Assert.That(InputPromptService.ActiveDevice, Is.SameAs(keyboard));
        }

        [Test]
        public void Pressing_a_gamepad_button_selects_the_matching_style()
        {
            InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            Press(gamepad.buttonSouth);

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox));
        }

        [Test]
        public void An_unknown_gamepad_uses_the_configured_fallback()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Press(gamepad.buttonSouth);

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox));
        }

        [Test]
        public void Moving_the_mouse_does_not_take_the_prompts_off_the_gamepad()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            Press(gamepad.buttonSouth);

            Set(mouse.position, new Vector2(320f, 240f));

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox));
        }

        [Test]
        public void Clicking_the_mouse_does_take_them_back()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            Press(gamepad.buttonSouth);

            Press(mouse.leftButton);

            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
        }

        [Test]
        public void The_icon_follows_the_device_in_use()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var action = new InputAction("Jump");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<Gamepad>/buttonSouth");

            Press(keyboard.spaceKey);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_keyboardSprite),
                "keyboard press should resolve the keyboard binding");

            Press(gamepad.buttonSouth);
            Assert.That(InputPromptService.GetSprite(action), Is.SameAs(_gamepadSprite),
                "gamepad press should resolve the gamepad binding");

            action.Dispose();
        }

        [Test]
        public void A_style_change_is_raised_once_per_switch()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            Press(keyboard.spaceKey);

            var styles = new System.Collections.Generic.List<InputDeviceStyle>();
            InputPromptService.StyleChanged += styles.Add;

            Press(gamepad.buttonSouth);
            Press(gamepad.buttonNorth);
            Press(keyboard.aKey);

            InputPromptService.StyleChanged -= styles.Add;

            Assert.That(styles, Is.EqualTo(new[] { InputDeviceStyle.Xbox, InputDeviceStyle.KeyboardMouse }));
        }

        #endregion


        #region Tools and Utilities

        private static InputPromptSet MakeSet(InputDeviceStyle style, string[] layouts,
            params (string key, Sprite sprite)[] entries)
        {
            var set = ScriptableObject.CreateInstance<InputPromptSet>();
            set.m_style = style;
            set.m_layouts = layouts;

            var list = new System.Collections.Generic.List<InputPromptSet.Entry>();
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
