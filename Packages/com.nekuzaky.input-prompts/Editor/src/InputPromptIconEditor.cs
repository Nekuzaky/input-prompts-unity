using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.InputPrompts.Editor
{
    [CustomEditor(typeof(InputPromptIcon))]
    [CanEditMultipleObjects]
    public class InputPromptIconEditor : UnityEditor.Editor
    {
        #region Private and Protected

        private const string PreviewPref = "InputPrompts.PreviewStyle";
        private const string PreviewOffLabel = "Follow active device";

        private static readonly string[] PreviewOptions =
            new[] { PreviewOffLabel }.Concat(Enum.GetNames(typeof(InputDeviceStyle))).ToArray();

        #endregion


        #region Unity API

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor preview", EditorStyles.boldLabel);
            DrawPreviewPicker();

            var icon = (InputPromptIcon)target;
            var displayString = icon.DisplayString;
            EditorGUILayout.LabelField("Bound to", string.IsNullOrEmpty(displayString) ? "(nothing)" : displayString);

            if (!GUILayout.Button("Refresh"))
                return;

            foreach (var selected in targets.OfType<InputPromptIcon>())
                selected.Refresh();
        }

        #endregion


        #region Tools and Utilities

        private static void DrawPreviewPicker()
        {
            var current = Mathf.Clamp(EditorPrefs.GetInt(PreviewPref, 0), 0, PreviewOptions.Length - 1);
            var picked = EditorGUILayout.Popup("Show icons of", current, PreviewOptions);

            if (picked != current)
                EditorPrefs.SetInt(PreviewPref, picked);

            ApplyPreview(picked);
        }

        private static void ApplyPreview(int index)
        {
            var style = index <= 0 ? (InputDeviceStyle?)null : (InputDeviceStyle)(index - 1);
            if (InputPromptService.EditorPreviewStyle == style)
                return;

            InputPromptService.EditorPreviewStyle = style;
            InputPromptService.Refresh();
            SceneView.RepaintAll();
        }

        #endregion
    }
}
