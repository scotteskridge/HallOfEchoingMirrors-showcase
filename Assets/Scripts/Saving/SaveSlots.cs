using System;
using System.IO;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.Saving
{
    /// <summary>
    /// The save files on disk: one JSON file per slot, in Unity's standard per-user save folder
    /// (the folder Steam Cloud will sync later). Writes go to a temporary file first and then
    /// replace the real one, so a crash mid-save can't leave a half-written save behind.
    /// </summary>
    public static class SaveSlots
    {
        public const int SlotCount = 3;

        /// <summary>Where saves live. Tests point this at a temporary folder.</summary>
        public static string Folder { get; set; } = Path.Combine(Application.persistentDataPath, "saves");

        public static string PathFor(int slot) => Path.Combine(Folder, $"slot{slot + 1}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        public static bool Write(int slot, SaveData data, out string error)
        {
            error = null;
            string path = PathFor(slot);
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(temp, SaveSerializer.ToJson(data));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                TryDelete(temp); // don't leave a half-written file lying next to the save
                return false;
            }
        }

        /// <summary>The save in a slot, or null (with a reason) if it's empty or can't be read.</summary>
        public static SaveData Read(int slot, out string error)
        {
            error = null;
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                error = GameText.Get("menu.slot_empty");
                return null;
            }

            try
            {
                return SaveSerializer.FromJson(File.ReadAllText(path), out error);
            }
            catch (Exception e)
            {
                error = GameText.Get("menu.damaged", ("detail", e.Message));
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Saves: couldn't remove the unfinished file {path}: {e.Message}");
            }
        }

        /// <summary>Deletes a slot's file. Returns false, with the reason, if the file couldn't be removed (it's still there).</summary>
        public static bool Delete(int slot, out string error)
        {
            error = null;
            string path = PathFor(slot);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
