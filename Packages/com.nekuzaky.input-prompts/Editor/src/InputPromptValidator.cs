using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekuzaky.InputPrompts.Editor
{
    public static class InputPromptValidator
    {
        #region Public

        public enum IssueKind
        {
            MissingIcon = 0,
            NoFamily = 1,
            Unresolvable = 2,
        }

        public readonly struct Issue
        {
            public readonly IssueKind m_kind;
            public readonly string m_action;
            public readonly string m_part;
            public readonly string m_path;
            public readonly string m_key;
            public readonly InputDeviceStyle m_style;

            public Issue(IssueKind kind, string action, string part, string path, string key, InputDeviceStyle style)
            {
                m_kind = kind;
                m_action = action;
                m_part = part;
                m_path = path;
                m_key = key;
                m_style = style;
            }

            public override string ToString()
            {
                var target = string.IsNullOrEmpty(m_part) ? m_action : $"{m_action}#{m_part}";
                return m_kind switch
                {
                    IssueKind.MissingIcon => $"{target}: {m_path} has no icon in {m_style} (key \"{m_key}\")",
                    IssueKind.NoFamily => $"{target}: {m_path} matches no device family of the database",
                    _ => $"{target}: {m_path} cannot be turned into an icon key",
                };
            }
        }

        #endregion


        #region Main API

        public static List<Issue> Validate(InputActionAsset actions, InputPromptDatabase database)
        {
            var issues = new List<Issue>();
            if (actions == null || database == null)
                return issues;

            foreach (var map in actions.actionMaps)
            {
                foreach (var action in map.actions)
                    ValidateAction(action, database, issues);
            }

            return issues;
        }

        [MenuItem("Tools/Input Prompts/Validate Action Assets")]
        public static void ValidateProject()
        {
            var database = InputPromptService.Database;
            if (database == null)
            {
                Debug.LogWarning("[Input Prompts] No prompt database to validate against. Generate one first.");
                return;
            }

            var total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                var issues = Validate(actions, database);
                total += issues.Count;

                if (issues.Count == 0)
                {
                    Debug.Log($"[Input Prompts] {path}: every binding has an icon.", actions);
                    continue;
                }

                var report = new StringBuilder($"[Input Prompts] {path}: {issues.Count} issue(s)\n");
                foreach (var issue in issues)
                    report.AppendLine($"    {issue}");

                Debug.LogWarning(report.ToString(), actions);
            }

            if (total == 0)
                Debug.Log("[Input Prompts] Validation passed.");
        }

        #endregion


        #region Tools and Utilities

        private static void ValidateAction(InputAction action, InputPromptDatabase database, List<Issue> issues)
        {
            var name = action.actionMap != null ? $"{action.actionMap.name}/{action.name}" : action.name;
            var bindings = action.bindings;

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite)
                    continue;

                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path))
                    continue;

                var part = binding.isPartOfComposite ? binding.name : null;
                var key = ControlPath.ToKey(path);
                if (key == null)
                {
                    issues.Add(new Issue(IssueKind.Unresolvable, name, part, path, null, default));
                    continue;
                }

                var sets = database.FindSetsFor(path);
                if (sets.Count == 0)
                {
                    issues.Add(new Issue(IssueKind.NoFamily, name, part, path, key, default));
                    continue;
                }

                foreach (var set in sets)
                {
                    if (database.IsShadowedByFallback(set))
                        continue;

                    if (set.Find(key) == null)
                        issues.Add(new Issue(IssueKind.MissingIcon, name, part, path, key, set.m_style));
                }
            }
        }

        #endregion
    }
}
