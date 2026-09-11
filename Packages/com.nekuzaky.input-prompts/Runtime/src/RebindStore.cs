using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts
{
    public static class RebindStore
    {
        #region Public

        public const string DefaultKey = "InputPrompts.Bindings";

        #endregion


        #region Main API

        public static void Save(InputActionAsset actions, string key = DefaultKey)
        {
            if (actions == null || string.IsNullOrEmpty(key))
                return;

            PlayerPrefs.SetString(key, actions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static bool Load(InputActionAsset actions, string key = DefaultKey)
        {
            if (actions == null || string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
                return false;

            var json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json))
                return false;

            actions.LoadBindingOverridesFromJson(json);
            InputPromptService.Refresh();
            return true;
        }

        public static void Clear(InputActionAsset actions, string key = DefaultKey)
        {
            if (actions != null)
                actions.RemoveAllBindingOverrides();

            if (!string.IsNullOrEmpty(key))
                PlayerPrefs.DeleteKey(key);

            InputPromptService.Refresh();
        }

        #endregion
    }
}
