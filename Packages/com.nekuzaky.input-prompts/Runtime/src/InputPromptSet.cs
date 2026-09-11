using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekuzaky.InputPrompts
{
    [CreateAssetMenu(menuName = "Input Prompts/Prompt Set", fileName = "SO_InputPromptSet")]
    public class InputPromptSet : ScriptableObject
    {
        #region Public

        [Header("Device")]
        [Tooltip("Device family this set represents.")]
        public InputDeviceStyle m_style = InputDeviceStyle.Generic;

        [Tooltip("Layouts this set is used for, e.g. \"XInputController\". Checked against InputDevice.layout, sub-layouts included.")]
        public string[] m_layouts = Array.Empty<string>();

        [Space(15), Header("Icons")]
        [Tooltip("Used when a key has no icon of its own, e.g. a blank key cap the control name is printed on.")]
        public Sprite m_blankSprite;

        [Tooltip("When a key is missing here, look it up in this set instead.")]
        public InputPromptSet m_fallback;

        [Tooltip("Generated sprite asset holding the same icons, for inline <sprite> tags in TextMeshPro.")]
        public TMP_SpriteAsset m_spriteAsset;

        #endregion


        #region Private and Protected

        private const int MaxFallbackDepth = 8;

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, Sprite> _lookup;

        #endregion


        #region Unity API

        private void OnValidate() => _lookup = null;

        #endregion


        #region Main API

        public IReadOnlyList<Entry> Entries => _entries;

        public Sprite Find(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var set = this;
            for (var depth = 0; set != null && depth < MaxFallbackDepth; depth++)
            {
                set.BuildLookup();
                if (set._lookup.TryGetValue(key, out var sprite) && sprite != null)
                    return sprite;

                set = set.m_fallback != set ? set.m_fallback : null;
            }

            return null;
        }

        public bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            BuildLookup();
            return _lookup.ContainsKey(key);
        }

        public void SetEntries(IEnumerable<Entry> entries)
        {
            _entries.Clear();
            _entries.AddRange(entries);
            _lookup = null;
        }

        public void Invalidate() => _lookup = null;

        #endregion


        #region Tools and Utilities

        private void BuildLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, Sprite>(_entries.Count, StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.m_key))
                    continue;
                _lookup[entry.m_key.ToLowerInvariant()] = entry.m_sprite;
            }
        }

        #endregion

        [Serializable]
        public struct Entry
        {
            [Tooltip("Control path without the device part, e.g. \"space\", \"buttonSouth\", \"leftStick/up\".")]
            public string m_key;

            public Sprite m_sprite;
        }
    }
}
