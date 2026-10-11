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
        const string MenuRoot = "ヒエラルキー順に Position X を設定";
        const string BottomFirstLabel = "下から";
        const string TopFirstLabel = "上から";
        const string GameObjectMenuRoot = "GameObject/S0iRu/" + MenuRoot;
        const string ToolsMenuRoot = "Tools/S0iRu/" + MenuRoot;

        // GameObject/ menu fires once per selected object; guard against that.
        static bool _called;

        [MenuItem(GameObjectMenuRoot + "/" + BottomFirstLabel, false, 0)]
        static void ExecuteGameObjectBottomFirst() => ExecuteFromGameObjectMenu(bottomFirst: true);

        [MenuItem(GameObjectMenuRoot + "/" + BottomFirstLabel, true)]
        static bool ValidateGameObjectBottomFirst() => HasTwoOrMoreSceneObjects();

        [MenuItem(GameObjectMenuRoot + "/" + TopFirstLabel, false, 1)]
        static void ExecuteGameObjectTopFirst() => ExecuteFromGameObjectMenu(bottomFirst: false);

        [MenuItem(GameObjectMenuRoot + "/" + TopFirstLabel, true)]
        static bool ValidateGameObjectTopFirst() => HasTwoOrMoreSceneObjects();

        [MenuItem(ToolsMenuRoot + "/" + BottomFirstLabel, false, 0)]
        static void ExecuteToolsBottomFirst() => Execute(bottomFirst: true);

        [MenuItem(ToolsMenuRoot + "/" + BottomFirstLabel, true)]
        static bool ValidateToolsBottomFirst() => HasTwoOrMoreSceneObjects();

        [MenuItem(ToolsMenuRoot + "/" + TopFirstLabel, false, 1)]
        static void ExecuteToolsTopFirst() => Execute(bottomFirst: false);

        [MenuItem(ToolsMenuRoot + "/" + TopFirstLabel, true)]
        static bool ValidateToolsTopFirst() => HasTwoOrMoreSceneObjects();

        static void ExecuteFromGameObjectMenu(bool bottomFirst)
        {
            if (_called) return;
            _called = true;
            EditorApplication.delayCall += () => _called = false;
            Execute(bottomFirst);
        }

        static bool HasTwoOrMoreSceneObjects()
        {
            return Selection.gameObjects.Count(go => !EditorUtility.IsPersistent(go)) >= 2;
        }

        static void Execute(bool bottomFirst)
        {
            var sceneObjects = Selection.gameObjects
                .Where(go => !EditorUtility.IsPersistent(go))
                .ToArray();

            if (sceneObjects.Length < 2) return;

            // Hierarchy display order is top-to-bottom. Reverse it when numbering from the bottom.
            Array.Sort(sceneObjects, (a, b) => CompareHierarchyOrder(a.transform, b.transform));
            if (bottomFirst)
                Array.Reverse(sceneObjects);

            var transforms = sceneObjects.Select(go => go.transform).ToArray();
            var undoName = bottomFirst
                ? MenuRoot + "（" + BottomFirstLabel + "）"
                : MenuRoot + "（" + TopFirstLabel + "）";

            Undo.RecordObjects(transforms.Cast<UnityEngine.Object>().ToArray(), undoName);

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
