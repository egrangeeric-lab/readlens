using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ReadLens.App
{
    // The captured pages (text only, never images), kept on the headset so they survive the app closing.
    [Serializable]
    public class SavedDocument
    {
        public List<string> pages = new List<string>();
        public int currentPage;

        public static SavedDocument Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var doc = JsonUtility.FromJson<SavedDocument>(File.ReadAllText(path));
                    if (doc?.pages != null)
                    {
                        doc.currentPage = Mathf.Clamp(doc.currentPage, 0, Mathf.Max(0, doc.pages.Count - 1));
                        return doc;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReadLens] Document unreadable, starting empty: " + e.Message);
            }
            return new SavedDocument();
        }

        public void Save(string path)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(this));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReadLens] Could not save the document: " + e.Message);
            }
        }
    }
}
