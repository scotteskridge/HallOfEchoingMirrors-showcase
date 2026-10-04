using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Closes the open scenes before a git command that may change a scene file (merge, stash,
    /// checkout), and reopens them after. If git rewrites a scene Unity has open, Unity stops
    /// everything for a "modified externally" dialog until someone clicks it, and Claude's Unity
    /// tools hang meanwhile. A closed scene can't trigger it. Refuses to close a scene with unsaved
    /// changes, so nothing is lost. What was open is kept in SessionState, which lasts until this
    /// editor closes (through script reloads, which a merge often causes), and is per editor, so
    /// each lane's editor remembers its own.
    /// </summary>
    public static class SceneGitGuard
    {
        const string ParkedKey = "HallOfEchoingMirrors.SceneGitGuard.Parked";

        [Serializable]
        private class ParkedScene
        {
            public string path;
            public bool loaded;
            public bool active;
        }

        [Serializable]
        private class ParkedScenes
        {
            public List<ParkedScene> scenes = new List<ParkedScene>();
        }

        [MenuItem("Hall of Echoing Mirrors/Tools/Close Scenes Before Git", false, 40)]
        private static void CloseMenu()
        {
            bool closed = CloseForGit(out string message);
            if (closed)
                Debug.Log(message);
            else
                Debug.LogWarning(message);
        }

        [MenuItem("Hall of Echoing Mirrors/Tools/Reopen Scenes After Git", false, 41)]
        private static void ReopenMenu()
        {
            bool reopened = ReopenAfterGit(out string message);
            if (reopened)
                Debug.Log(message);
            else
                Debug.LogWarning(message);
        }

        /// <summary>True when scenes were closed by <see cref="CloseForGit"/> and not yet reopened.</summary>
        public static bool HasClosedScenes => !string.IsNullOrEmpty(SessionState.GetString(ParkedKey, ""));

        /// <summary>
        /// Remembers the open scenes and closes them (an empty, unsaved scene takes their place).
        /// False, with nothing closed, during Play or while a scene has unsaved changes.
        /// Calling it again before reopening changes nothing, so the first list is kept, unless
        /// scenes were opened by hand meanwhile: that list is stale, and the open ones are closed instead.
        /// </summary>
        public static bool CloseForGit(out string message)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Close Scenes Before Git: refused during Play. Stop Play first.";
                return false;
            }
            string saved = Encode(EditorSceneManager.GetSceneManagerSetup());
            if (HasClosedScenes && saved.Length == 0)
            {
                message = "Close Scenes Before Git: already closed; run Reopen Scenes After Git when git is done.";
                return true;
            }
            if (HasUnsavedWork("Close Scenes Before Git", out message))
                return false;

            if (saved.Length == 0)
            {
                message = "Close Scenes Before Git: no saved scene is open, so there is nothing to close.";
                return true;
            }
            SessionState.SetString(ParkedKey, saved);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            message = "Close Scenes Before Git: closed " + Describe(saved) + ". Run Reopen Scenes After Git when git is done.";
            return true;
        }

        /// <summary>
        /// Picks up what git changed, then reopens the scenes <see cref="CloseForGit"/> closed, as
        /// they now are on disk. A scene git deleted is skipped. False, keeping the list to try again,
        /// during Play or while something open has unsaved changes (reopening would replace it unasked).
        /// False if none were closed.
        /// </summary>
        public static bool ReopenAfterGit(out string message)
        {
            string saved = SessionState.GetString(ParkedKey, "");
            if (string.IsNullOrEmpty(saved))
            {
                message = "Reopen Scenes After Git: nothing was closed, so there is nothing to reopen.";
                return false;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Reopen Scenes After Git: refused during Play. Stop Play first.";
                return false;
            }
            if (HasUnsavedWork("Reopen Scenes After Git", out message))
                return false;

            AssetDatabase.Refresh();
            var setup = new List<SceneSetup>();
            var missing = new List<string>();
            foreach (var scene in Decode(saved))
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null)
                    setup.Add(scene);
                else
                    missing.Add(scene.path);
            }
            if (setup.Count == 0)
            {
                SessionState.EraseString(ParkedKey);
                message = "Reopen Scenes After Git: every closed scene is gone from disk: " + string.Join(", ", missing);
                return false;
            }
            // Unity needs exactly one active scene, and it must be loaded; the active one may have been the one deleted.
            if (!setup.Exists(s => s.isActive))
            {
                setup[0].isActive = true;
                setup[0].isLoaded = true;
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup.ToArray());
            // Only now: if the restore throws, the list is still there to try again.
            SessionState.EraseString(ParkedKey);
            message = "Reopen Scenes After Git: reopened " + Describe(Encode(setup.ToArray()))
                + (missing.Count > 0 ? ". Gone from disk, skipped: " + string.Join(", ", missing) : ".");
            return true;
        }

        // Closing or reopening replaces what's open without asking (a save prompt would freeze Claude's
        // Unity tools just like the git dialog), so refuse while an open scene or prefab has unsaved changes.
        private static bool HasUnsavedWork(string action, out string message)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    string name = string.IsNullOrEmpty(scene.name) ? "the untitled scene" : $"'{scene.name}'";
                    message = $"{action}: refused, {name} has unsaved changes. Save it (Ctrl+S) or discard them first, then try again.";
                    return true;
                }
            }
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene.isDirty)
            {
                message = $"{action}: refused, the prefab being edited ({prefabStage.assetPath}) has unsaved changes. Save it or discard them first.";
                return true;
            }
            message = null;
            return false;
        }

        /// <summary>The open scenes as text for SessionState. An unsaved scene (no path) can't be reopened, so it's left out.</summary>
        public static string Encode(SceneSetup[] setup)
        {
            var parked = new ParkedScenes();
            foreach (var scene in setup)
                if (!string.IsNullOrEmpty(scene.path))
                    parked.scenes.Add(new ParkedScene { path = scene.path, loaded = scene.isLoaded, active = scene.isActive });
            return parked.scenes.Count > 0 ? JsonUtility.ToJson(parked) : "";
        }

        /// <summary>The scenes <see cref="Encode"/> wrote, in the same order; none for empty text.</summary>
        public static SceneSetup[] Decode(string saved)
        {
            if (string.IsNullOrEmpty(saved))
                return new SceneSetup[0];
            var parked = JsonUtility.FromJson<ParkedScenes>(saved);
            return parked.scenes.ConvertAll(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray();
        }

        private static string Describe(string saved) =>
            string.Join(", ", Array.ConvertAll(Decode(saved), s => s.path));
    }
}
