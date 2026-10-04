using HallOfEchoingMirrors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Editor helpers for the book pages: lay them out side by side so they don't overlap, frame
    /// one in the Scene view, or preview one in the Game view. Where pages sit in the editor makes
    /// no difference to the game: when Play starts, the Screen Manager positions them itself.
    /// </summary>
    public static class PageLayout
    {
        const float Gap = 200f;

        [MenuItem("Hall of Echoing Mirrors/UI/Lay Out Pages Side by Side", false, 20)]
        public static void LayOutSideBySideMenu()
        {
            var screens = Object.FindFirstObjectByType<ScreenManager>();
            if (screens == null)
            {
                Debug.LogWarning("Lay Out Pages: the open scene has no Screen Manager, so there are no pages to lay out.");
                return;
            }
            LayOutSideBySide(screens);
        }

        /// <summary>
        /// Puts every page in a row to the right of the view, so the view itself shows only the page
        /// background, and each page can be edited on its own.
        /// </summary>
        public static void LayOutSideBySide(ScreenManager screens)
        {
            Undo.SetCurrentGroupName("Lay Out Pages");
            int group = Undo.GetCurrentGroup();

            for (int i = 0; i < screens.Screens.Count; i++)
            {
                var rect = RectOf(screens.Screens[i]);
                if (rect != null)
                    Place(rect, (i + 1) * (WidthOf(rect) + Gap));
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(screens.gameObject.scene);
        }

        /// <summary>Moves one page into the view (for the Game view), with the rest laid out in a row.</summary>
        public static void Preview(ScreenManager screens, ScreenManager.ScreenEntry entry)
        {
            LayOutSideBySide(screens);
            var rect = RectOf(entry);
            if (rect != null)
                Place(rect, 0f);
            Selection.activeObject = rect != null ? rect.gameObject : null;
        }

        /// <summary>Selects a page and zooms the Scene view to it.</summary>
        public static void Frame(ScreenManager.ScreenEntry entry)
        {
            var rect = RectOf(entry);
            if (rect == null)
                return;
            Selection.activeGameObject = rect.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static RectTransform RectOf(ScreenManager.ScreenEntry entry) =>
            entry?.group != null ? entry.group.transform as RectTransform : null;

        private static float WidthOf(RectTransform rect) =>
            rect.parent is RectTransform parent ? parent.rect.width : 1920f;

        private static void Place(RectTransform rect, float x)
        {
            Undo.RecordObject(rect, "Lay Out Pages");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, 0f);
        }
    }
}
