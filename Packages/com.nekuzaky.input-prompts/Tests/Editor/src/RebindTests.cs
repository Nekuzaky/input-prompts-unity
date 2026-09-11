using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts.Tests
{
    public class RebindTests : InputTestFixture
    {
        #region Private and Protected

        private InputActionAsset _actions;
        private InputAction _jump;
        private InputAction _crouch;

        #endregion


        #region Unity API

        public override void Setup()
        {
            base.Setup();

            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = _actions.AddActionMap("Player");

            _jump = map.AddAction("Jump", binding: "<Keyboard>/space");
            _crouch = map.AddAction("Crouch", binding: "<Keyboard>/c");
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(_actions);
            base.TearDown();
        }

        #endregion


        #region Main API

        [Test]
        public void A_free_control_raises_no_conflict()
        {
            _jump.ApplyBindingOverride("<Keyboard>/j");

            Assert.That(RebindConflicts.Find(_jump, 0).IsValid, Is.False);
        }

        [Test]
        public void Binding_a_control_already_used_is_detected()
        {
            _jump.ApplyBindingOverride("<Keyboard>/c");

            var conflict = RebindConflicts.Find(_jump, 0);

            Assert.That(conflict.IsValid, Is.True);
            Assert.That(conflict.m_action, Is.SameAs(_crouch));
            Assert.That(conflict.m_bindingIndex, Is.EqualTo(0));
        }

        [Test]
        public void A_binding_never_conflicts_with_itself()
        {
            Assert.That(RebindConflicts.Find(_crouch, 0).IsValid, Is.False);
        }

        [Test]
        public void Swapping_gives_the_other_action_the_freed_control()
        {
            const string previous = "<Keyboard>/space";
            _jump.ApplyBindingOverride("<Keyboard>/c");

            var conflict = RebindConflicts.Find(_jump, 0);
            RebindConflicts.Swap(conflict, previous);

            Assert.That(_jump.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/c"));
            Assert.That(_crouch.bindings[0].effectivePath, Is.EqualTo(previous));
            Assert.That(RebindConflicts.Find(_jump, 0).IsValid, Is.False,
                "after a swap neither action should be left on a duplicate");
        }

        [Test]
        public void Conflicts_are_found_across_action_maps()
        {
            var ui = _actions.AddActionMap("UI");
            var submit = ui.AddAction("Submit", binding: "<Keyboard>/enter");

            _jump.ApplyBindingOverride("<Keyboard>/enter");

            var conflict = RebindConflicts.Find(_jump, 0);

            Assert.That(conflict.IsValid, Is.True);
            Assert.That(conflict.m_action, Is.SameAs(submit));
        }

        [Test]
        public void Overrides_survive_a_save_and_load_round_trip()
        {
            const string key = "InputPrompts.Tests.Bindings";
            _jump.ApplyBindingOverride("<Keyboard>/j");
            RebindStore.Save(_actions, key);

            _actions.RemoveAllBindingOverrides();
            Assert.That(_jump.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));

            var loaded = RebindStore.Load(_actions, key);

            Assert.That(loaded, Is.True);
            Assert.That(_jump.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/j"));

            PlayerPrefs.DeleteKey(key);
        }

        #endregion
    }
}
