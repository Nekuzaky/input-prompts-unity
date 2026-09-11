using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nekuzaky.InputPrompts.Editor
{
    /// <summary>Menu entries that create prompt objects, so nothing has to be wired by hand.</summary>
    public static class InputPromptMenu
    {
        #region Private and Protected

        private const float IconSize = 48f;

        #endregion


        #region Main API

        [MenuItem("GameObject/UI/Input Prompt Icon", false, 2100)]
        public static void CreateIcon(MenuCommand command)
        {
            var parent = (command.context as GameObject)?.transform ?? FindCanvas()?.transform;
            var icon = CreateIconObject("Input Prompt Icon", parent);

            Undo.RegisterCreatedObjectUndo(icon.gameObject, "Create Input Prompt Icon");
            Selection.activeGameObject = icon.gameObject;
        }

        [MenuItem("Tools/Input Prompts/Create Demo Canvas")]
        public static void CreateDemo()
        {
            var actions = Selection.activeObject as InputActionAsset ?? FindFirstActionAsset();
            if (actions == null)
            {
                EditorUtility.DisplayDialog("Input Prompts",
                    "No Input Action Asset found in this project. Create one, or select it in the Project "
                    + "window before running this.", "OK");
                return;
            }

            var canvas = FindCanvas() ?? CreateCanvas();
            var root = CreateDemoRoot(canvas);
            var prefab = LoadOrCreateIconPrefab();

            foreach (var map in actions.actionMaps)
            {
                foreach (var action in map.actions)
                    CreateRow(root.transform, actions, map.name, action.name, prefab);
            }

            Selection.activeGameObject = root;
            Debug.Log("[Input Prompts] Demo canvas created. Enter play mode and switch between keyboard and gamepad.");
        }

        #endregion


        #region Tools and Utilities

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Demo Canvas");
            return canvas;
        }

        private static GameObject CreateDemoRoot(Canvas canvas)
        {
            var root = new GameObject("Input Prompt Demo", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(root, "Create Input Prompt Demo");
            root.transform.SetParent(canvas.transform, worldPositionStays: false);

            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(40f, -40f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            return root;
        }

        private static void CreateRow(Transform parent, InputActionAsset actions, string mapName, string actionName,
            InputPromptIcon prefab)
        {
            var row = new GameObject(actionName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, worldPositionStays: false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(520f, IconSize);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            CreateGroup(row.transform, actions, mapName, actionName, prefab);
            CreateLabel(row.transform, $"{mapName}/{actionName}");
        }

        private static void CreateGroup(Transform parent, InputActionAsset actions, string mapName, string actionName,
            InputPromptIcon prefab)
        {
            var group = new GameObject("Icons",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(InputPromptGroup));
            group.transform.SetParent(parent, worldPositionStays: false);
            ((RectTransform)group.transform).sizeDelta = new Vector2(4f * IconSize, IconSize);

            var layout = group.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var serialized = new SerializedObject(group.GetComponent<InputPromptGroup>());
            serialized.FindProperty("_action").objectReferenceValue = FindReference(actions, mapName, actionName);
            serialized.FindProperty("_iconPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLabel(Transform parent, string content)
        {
            // TextMeshPro needs its essentials imported before it can draw anything.
            if (TMP_Settings.defaultFontAsset == null)
                return;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(parent, worldPositionStays: false);
            ((RectTransform)label.transform).sizeDelta = new Vector2(240f, IconSize);

            var text = label.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        /// <summary>
        /// The action reference the demo binds to. Input Action Assets keep one sub-asset per action.
        /// </summary>
        private static InputActionReference FindReference(InputActionAsset actions, string mapName, string actionName)
        {
            var path = AssetDatabase.GetAssetPath(actions);

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is InputActionReference reference &&
                    reference.action != null &&
                    reference.action.name == actionName &&
                    reference.action.actionMap?.name == mapName)
                    return reference;
            }

            return null;
        }

        /// <summary>Any action asset of the project, so the demo works without a fixed file name.</summary>
        private static InputActionAsset FindFirstActionAsset()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }

            return null;
        }

        private static InputPromptIcon LoadOrCreateIconPrefab()
        {
            // The folder comes from the dashboard settings: the package must not impose a layout on
            // the project that uses it.
            var settings = InputPromptSettings.instance;
            var prefabPath = settings.PrefabPath;

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
                return existing.GetComponent<InputPromptIcon>();

            CreateFolders(settings.m_prefabFolder);

            var temp = CreateIconObject("P_InputPromptIcon", null);
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp.gameObject, prefabPath);
            Object.DestroyImmediate(temp.gameObject);
            return prefab.GetComponent<InputPromptIcon>();
        }

        private static InputPromptIcon CreateIconObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputPromptIcon));
            if (parent != null)
                go.transform.SetParent(parent, worldPositionStays: false);

            ((RectTransform)go.transform).sizeDelta = new Vector2(IconSize, IconSize);
            go.GetComponent<Image>().preserveAspect = true;
            return go.GetComponent<InputPromptIcon>();
        }

        private static void CreateFolders(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Canvas FindCanvas() => Object.FindFirstObjectByType<Canvas>();

        #endregion
    }
}
