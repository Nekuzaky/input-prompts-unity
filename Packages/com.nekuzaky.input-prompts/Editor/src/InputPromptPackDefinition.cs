using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Editor
{
    [CreateAssetMenu(menuName = "Input Prompts/Pack Definition", fileName = "SO_InputPromptPack")]
    public class InputPromptPackDefinition : ScriptableObject
    {
        #region Public

        [Header("Files")]
        [Tooltip("Each family folder holds Default and Double sub-folders, as in the Kenney pack.")]
        public bool m_usesVariantFolders = true;

        [Tooltip("File extension of the icons, dot included.")]
        public string m_extension = ".png";

        [Tooltip("Appended to a file name to find its outline variant.")]
        public string m_outlineSuffix = "_outline";

        [Space(15), Header("Families")]
        public List<Family> m_families = new();

        [Serializable]
        public class Family
        {
            public InputDeviceStyle m_style;

            [Tooltip("Folder of this family inside the pack.")]
            public string m_folder;

            [Tooltip("Input System layouts this family answers for, e.g. \"DualShockGamepad\".")]
            public string[] m_layouts = Array.Empty<string>();

            [Tooltip("File drawn behind a control that has no icon of its own.")]
            public string m_blankIcon;

            [Tooltip("Look missing keys up in the Generic family.")]
            public bool m_fallsBackToGeneric = true;

            public List<Mapping> m_mappings = new();
        }

        [Serializable]
        public struct Mapping
        {
            [Tooltip("Control key, e.g. \"buttonsouth\" or \"leftstick/up\".")]
            public string m_key;

            [Tooltip("Icon file name, without extension or outline suffix.")]
            public string m_file;
        }

        #endregion


        #region Main API

        public Family Find(InputDeviceStyle style)
        {
            foreach (var family in m_families)
            {
                if (family != null && family.m_style == style)
                    return family;
            }

            return null;
        }

        #endregion
    }
}
