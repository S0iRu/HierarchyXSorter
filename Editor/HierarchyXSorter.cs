using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace S0iRu.HierarchyXSorter.Editor
{
    internal static class HierarchyXSorter
    {
        const string GameObjectMenuPath = "GameObject/S0iRu/Set Position X by Hierarchy";
        const string ToolsMenuPath = "Tools/S0iRu/Set Position X by Hierarchy";

        // GameObject/ menu fires once per selected object; guard against that.
        static bool _called;

        [MenuItem(GameObjectMenuPath, false, 0)]
        static void ExecuteFromGameObjectMenu()
        {
            if (_called) return;
            _called = true;
            EditorApplication.delayCall += () => _called = false;
            Execute();
        }

        [MenuItem(GameObjectMenuPath, true)]
        static bool ValidateGameObjectMenu() => HasTwoOrMoreSceneObjects();

        [MenuItem(ToolsMenuPath, false)]
        static void ExecuteFromToolsMenu() => Execute();

        [MenuItem(ToolsMenuPath, true)]
        static bool ValidateToolsMenu() => HasTwoOrMoreSceneObjects();

        static bool HasTwoOrMoreSceneObjects()
        {
            return Selection.gameObjects.Count(go => !EditorUtility.IsPersistent(go)) >= 2;
        }

        static void Execute()
        {
            var sceneObjects = Selection.gameObjects
                .Where(go => !EditorUtility.IsPersistent(go))
                .ToArray();

            if (sceneObjects.Length < 2) return;

            // Sort by hierarchy display order (top-to-bottom), then reverse so bottom comes first.
            Array.Sort(sceneObjects, (a, b) => CompareHierarchyOrder(a.transform, b.transform));
            Array.Reverse(sceneObjects);

            var transforms = sceneObjects.Select(go => go.transform).ToArray();

            Undo.RecordObjects(transforms.Cast<UnityEngine.Object>().ToArray(), "Set Position X by Hierarchy");

            for (int i = 0; i < transforms.Length; i++)
            {
                var pos = transforms[i].localPosition;
                pos.x = i + 1;
                transforms[i].localPosition = pos;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>
        /// Compares two Transforms by their visual hierarchy order (top-to-bottom).
        /// Builds a path of sibling indices from root to each transform and compares lexicographically.
        /// </summary>
        static int CompareHierarchyOrder(Transform a, Transform b)
        {
            var pathA = GetSiblingPath(a);
            var pathB = GetSiblingPath(b);

            int len = Math.Min(pathA.Count, pathB.Count);
            for (int i = 0; i < len; i++)
            {
                int cmp = pathA[i].CompareTo(pathB[i]);
                if (cmp != 0) return cmp;
            }

            return pathA.Count.CompareTo(pathB.Count);
        }

        static List<int> GetSiblingPath(Transform t)
        {
            var path = new List<int>();
            var current = t;
            while (current != null)
            {
                path.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            path.Reverse();
            return path;
        }
    }
}
