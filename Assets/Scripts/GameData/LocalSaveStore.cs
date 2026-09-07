using System;
using System.IO;
using System.Text;
using MapleClient.GameLogic.Core;
using UnityEngine;

namespace MapleClient.GameData
{
    public sealed class LocalSaveStore
    {
        public string FilePath { get; }
        public LocalSaveStore(string path) { FilePath = Path.GetFullPath(path); }
        public bool Exists => File.Exists(FilePath);
        public bool TryWrite(LocalProgress progress, out string message)
        {
            if (progress == null) { message = "Enter a local map before saving."; return false; }
            string temporary = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(progress, true));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak", true);
                else File.Move(temporary, FilePath);
                message = "Progress saved."; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { message = "Could not write the save. The previous save is preserved."; return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch (IOException) {} catch (UnauthorizedAccessException) {} }
        }
        public bool TryRead(out LocalProgress progress, out string message)
        {
            progress = null;
            if (!Exists) { message = "No local save yet. Use Save progress first."; return false; }
            try
            {
                if (new FileInfo(FilePath).Length > 1024 * 1024) { message = "The save is too large to read."; return false; }
                progress = JsonUtility.FromJson<LocalProgress>(File.ReadAllText(FilePath, Encoding.UTF8));
                if (progress == null || progress.Version < 1 || progress.Version > 5 || progress.Player == null)
                { progress = null; message = "This save is not a supported character save."; return false; }
                message = "Save read."; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { message = "Could not read the save. Your current session is unchanged."; return false; }
        }
    }
}
