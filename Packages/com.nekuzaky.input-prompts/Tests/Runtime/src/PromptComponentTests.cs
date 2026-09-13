using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Nekuzaky.InputPrompts.Tests
{
    public class PromptComponentTests : InputTestFixture
    {
        #region Private and Protected

        private readonly List<GameObject> _spawned = new();

        private InputPromptDatabase _database;
        private Sprite _keyboardSprite;
        private Sprite _gamepadSprite;
        private Sprite _blankSprite;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _keyboardSprite = MakeSprite();
            _gamepadSprite = MakeSprite();
            _blankSprite = MakeSprite();

            var keyboardSet = MakeSet(InputDeviceStyle.KeyboardMouse, new[] { "Keyboard", "Mouse" },
                ("space", _keyboardSprite), ("w", _keyboardSprite), ("a", _keyboardSprite),
                ("s", _keyboardSprite), ("d", _keyboardSprite));
            keyboardSet.m_blankSprite = _blankSprite;

            var xboxSet = MakeSet(InputDeviceStyle.Xbox, new[] { "XInputController" },
                ("buttonsouth", _gamepadSprite), ("leftstick", _gamepadSprite));

            _database = ScriptableObject.CreateInstance<InputPromptDatabase>();
            _database.m_sets.Add(keyboardSet);
            _database.m_sets.Add(xboxSet);
            _database.m_gamepadFallback = xboxSet;

            InputPromptService.Shutdown();
            InputPromptService.Database = _database;
            InputPromptService.Initialize();
        }

        public override void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _spawned.Clear();
            InputPromptService.Shutdown();
            InputPromptService.Database = null;
            base.TearDown();
        }

        #endregion


        #region Main API

        [UnityTest]
        public IEnumerator Icon_swaps_its_sprite_when_the_player_changes_device()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var action = new InputAction("Jump");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<Gamepad>/buttonSouth");

            var icon = MakeIcon();
            icon.Action = action;
            yield return null;

            Press(keyboard.spaceKey);
            yield return null;
            Assert.That(icon.GetComponent<Image>().sprite, Is.SameAs(_keyboardSprite));

            Press(gamepad.buttonSouth);
            yield return null;
            Assert.That(icon.GetComponent<Image>().sprite, Is.SameAs(_gamepadSprite),
                "the icon should repaint itself, without anything calling Refresh");

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator Group_shows_four_keys_on_keyboard_and_one_stick_on_gamepad()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var action = new InputAction("Move", InputActionType.Value);
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            action.AddBinding("<Gamepad>/leftStick");

            var group = MakeGroup(action);
            yield return null;

            Press(keyboard.spaceKey);
            group.Rebuild();
            yield return null;
            Assert.That(ActiveIcons(group), Is.EqualTo(4), "WASD should show one icon per direction");

            Press(gamepad.buttonSouth);
            yield return null;
            Assert.That(ActiveIcons(group), Is.EqualTo(1), "the gamepad binding is a single stick");

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator Icon_falls_back_to_a_blank_cap_for_a_key_without_an_icon()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            var action = new InputAction("Debug");
            action.AddBinding("<Keyboard>/f13");

            var icon = MakeIcon();
            icon.Action = action;

            Press(keyboard.spaceKey);
            yield return null;

            Assert.That(icon.GetComponent<Image>().sprite, Is.SameAs(_blankSprite));
            Assert.That(icon.GetComponent<Image>().enabled, Is.True, "a missing icon must not blank the prompt");

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator Icon_hides_itself_when_the_action_has_no_binding()
        {
            InputSystem.AddDevice<Keyboard>();

            var action = new InputAction("Unbound");
            var icon = MakeIcon();
            icon.Action = action;
            yield return null;

            Assert.That(icon.GetComponent<Image>().enabled, Is.False);

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator A_world_space_sprite_follows_the_device()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var action = new InputAction("Jump");
            action.AddBinding("<Keyboard>/space");
            action.AddBinding("<Gamepad>/buttonSouth");

            var go = new GameObject("World Prompt", typeof(SpriteRenderer), typeof(InputPromptSprite));
            _spawned.Add(go);
            var prompt = go.GetComponent<InputPromptSprite>();
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            prompt.Action = action;

            Press(keyboard.spaceKey);
            yield return null;
            Assert.That(spriteRenderer.sprite, Is.SameAs(_keyboardSprite));

            Press(gamepad.buttonSouth);
            yield return null;
            Assert.That(spriteRenderer.sprite, Is.SameAs(_gamepadSprite));

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator A_world_height_scales_the_sprite_to_that_size()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var action = new InputAction("Jump", binding: "<Keyboard>/space");

            var go = new GameObject("World Prompt", typeof(SpriteRenderer), typeof(InputPromptSprite));
            _spawned.Add(go);
            var prompt = go.GetComponent<InputPromptSprite>();
            prompt.Action = action;
            Press(keyboard.spaceKey);

            prompt.WorldHeight = 2f;
            yield return null;

            var rendered = go.GetComponent<SpriteRenderer>().bounds.size.y;
            Assert.That(rendered, Is.EqualTo(2f).Within(0.001f));

            action.Dispose();
        }

        [UnityTest]
        public IEnumerator Two_players_paired_to_different_devices_see_their_own_icons()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<XInputController>();

            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var jump = actions.AddActionMap("Player").AddAction("Jump", binding: "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            var prefab = new GameObject("PlayerPrefab");
            prefab.SetActive(false);
            prefab.AddComponent<PlayerInput>().actions = actions;
            prefab.AddComponent<InputPromptPlayer>();
            _spawned.Add(prefab);

            var one = PlayerInput.Instantiate(prefab, pairWithDevice: keyboard);
            var two = PlayerInput.Instantiate(prefab, pairWithDevice: gamepad);
            _spawned.Add(one.gameObject);
            _spawned.Add(two.gameObject);
            yield return null;

            Press(keyboard.spaceKey);
            Press(gamepad.buttonSouth);
            yield return null;

            var oneContext = one.GetComponent<InputPromptPlayer>().Context;
            var twoContext = two.GetComponent<InputPromptPlayer>().Context;

            Assert.That(oneContext.CurrentStyle, Is.EqualTo(InputDeviceStyle.KeyboardMouse));
            Assert.That(twoContext.CurrentStyle, Is.EqualTo(InputDeviceStyle.Xbox));
            Assert.That(oneContext.GetSprite(one.actions["Jump"]), Is.SameAs(_keyboardSprite));
            Assert.That(twoContext.GetSprite(two.actions["Jump"]), Is.SameAs(_gamepadSprite));

            Object.DestroyImmediate(actions);
        }

        #endregion


        #region Tools and Utilities

        private static int ActiveIcons(InputPromptGroup group)
        {
            var count = 0;
            foreach (Transform child in group.transform)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private InputPromptIcon MakeIcon()
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(InputPromptIcon));
            _spawned.Add(go);
            return go.GetComponent<InputPromptIcon>();
        }

        private InputPromptGroup MakeGroup(InputAction action)
        {
            var prefabObject = new GameObject("IconPrefab", typeof(RectTransform), typeof(Image),
                typeof(InputPromptIcon));
            prefabObject.SetActive(false);
            _spawned.Add(prefabObject);

            var groupObject = new GameObject("Group", typeof(RectTransform), typeof(InputPromptGroup));
            _spawned.Add(groupObject);

            var group = groupObject.GetComponent<InputPromptGroup>();
            SetPrivate(group, "_iconPrefab", prefabObject.GetComponent<InputPromptIcon>());
            group.Action = action;
            return group;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
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
