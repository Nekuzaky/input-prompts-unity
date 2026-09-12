using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

namespace Nekuzaky.InputPrompts.Tests
{
    public class LocalCoopTests : InputTestFixture
    {
        #region Private and Protected

        private readonly List<InputPromptContext> _registered = new();

        private Sprite _keySprite;
        private Sprite _padSprite;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _keySprite = MakeSprite();
            _padSprite = MakeSprite();

            var keyboardSet = MakeSet(InputDeviceStyle.KeyboardMouse, new[] { "Keyboard", "Mouse" },
                ("space", _keySprite));
            var xboxSet = MakeSet(InputDeviceStyle.Xbox, new[] { "XInputController" },
                ("buttonsouth", _padSprite));

            var database = ScriptableObject.CreateInstance<InputPromptDatabase>();
            database.m_sets.Add(keyboardSet);
            database.m_sets.Add(xboxSet);
            database.m_gamepadFallback = xboxSet;

            InputPromptService.Shutdown();
            InputPromptService.Database = database;
            InputPromptService.Initialize();
        }

        public override void TearDown()
        {
            foreach (var context in _registered)
                InputPromptService.Unregister(context);

            _registered.Clear();
            InputPromptService.ControlNameTranslator = null;
            InputPromptService.Shutdown();
            InputPromptService.Database = null;
            base.TearDown();
        }

        #endregion


        #region Main API

        [Test]
        public void Each_player_follows_only_its_own_device()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            var playerOne = Register(device => device == keyboard);
            var playerTwo = Register(device => device == gamepad);

            Press(keyboard.spaceKey);
            Press(gamepad.buttonSouth);

            Assert.That(playerOne.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse),
                "player one never touched the gamepad");
            Assert.That(playerTwo.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox));
            Assert.That(InputPromptService.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox),
                "the global context still follows whoever acted last");
        }

        [Test]
        public void The_same_action_shows_each_player_their_own_icon()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            var playerOne = Register(device => device == keyboard);
            var playerTwo = Register(device => device == gamepad);

            var jump = new InputAction("Jump");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            Press(keyboard.spaceKey);
            Press(gamepad.buttonSouth);

            Assert.That(playerOne.GetSprite(jump), Is.SameAs(_keySprite));
            Assert.That(playerTwo.GetSprite(jump), Is.SameAs(_padSprite));

            jump.Dispose();
        }

        [Test]
        public void A_player_is_only_told_to_repaint_when_its_own_device_changes()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            var playerOne = Register(device => device == keyboard);
            Press(keyboard.spaceKey);

            var repaints = 0;
            void OnPromptsChanged() => repaints++;
            playerOne.PromptsChanged += OnPromptsChanged;

            Press(gamepad.buttonSouth);
            Press(gamepad.buttonNorth);

            playerOne.PromptsChanged -= OnPromptsChanged;

            Assert.That(repaints, Is.Zero, "another player's gamepad must not repaint player one's prompts");
        }

        [Test]
        public void An_unregistered_player_stops_following_input()
        {
            var gamepad = InputSystem.AddDevice<XInputController>();
            InputSystem.AddDevice<Keyboard>();
            var player = Register(device => device == gamepad);

            InputPromptService.Unregister(player);
            Press(gamepad.buttonSouth);

            Assert.That(player.ActiveDevice, Is.Null);
        }

        [Test]
        public void A_player_losing_its_gamepad_falls_back_to_a_device_it_still_owns()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();
            var player = Register(device => device == keyboard || device == gamepad);

            Press(gamepad.buttonSouth);
            Assert.That(player.ActiveDevice, Is.SameAs(gamepad));

            InputSystem.RemoveDevice(gamepad);

            Assert.That(player.ActiveDevice, Is.SameAs(keyboard));
            Assert.That(player.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
        }

        [Test]
        public void Control_names_go_through_the_translator()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var jump = new InputAction("Jump", binding: "<Keyboard>/space");
            Press(keyboard.spaceKey);

            InputPromptService.ControlNameTranslator = (key, name) => key == "space" ? "Espace" : name;

            Assert.That(InputPromptService.GetDisplayString(jump), Is.EqualTo("Espace"));

            jump.Dispose();
        }

        #endregion


        #region Tools and Utilities

        private InputPromptContext Register(System.Func<InputDevice, bool> owns)
        {
            var context = new InputPromptContext(owns);
            InputPromptService.Register(context);
            _registered.Add(context);
            return context;
        }

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
