using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Ex;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace mulova.shortcut
{
    [Serializable]
    public class ShortcutList
    {
        public List<ShortcutItem> entries = new List<ShortcutItem>();

        public ShortcutItem this[int i] => entries[i];

        public int Count => entries.Count;

        public void Add(Object obj)
        {
            var e = new ShortcutItem(obj);
            entries.Add(e);
        }

        public void Add(ShortcutItem i)
        {
            entries.Add(i);
        }

        public ShortcutItem Find(Object obj) => entries.Find(e => e.reference == obj);

        public ShortcutItem Find(string id) => entries.Find(e => e.id == id);

        public int IndexOf(Object obj) => entries.FindIndex(e => e.reference == obj);

        public int IndexOf(string id) => entries.FindIndex(e => e.id == id);

        public void Insert(int index, Object obj)
        {
            var s = new ShortcutItem(obj);
            entries.Insert(index, s);
        }

        public void Remove(Object obj)
        {
            //int index = FindIndex(o => o.reference == obj);
            int index = entries.FindIndex(o =>
            {
                var o1 = o.reference;
                return o1.UnityEquals(obj);
            });

            if (index >= 0)
            {
                Debug.LogWarningFormat("Remove {0}", entries[index].ToString());
                entries.RemoveAt(index);
            }
        }

        public void Remove(string guid)
        {
            int index = entries.FindIndex(o => o.id == guid);
            if (index >= 0)
            {
                entries.RemoveAt(index);
            }
        }

        public bool Contains(Object obj)
        {
            return IndexOf(obj) >= 0;
        }
        public static ShortcutList Load(string path)
        {
            if (File.Exists(path))
            {
                ShortcutList list = null;
                try
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    list = JsonUtility.FromJson<ShortcutList>(json);
                } catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                if (list == null)
                {
                    list = new ShortcutList();
                }
                return list;
            }
            else
            {
                return new ShortcutList();
            }
        }

        internal void RemoveMissing()
        {
            var valids = new List<ShortcutItem>();
            foreach (var e in entries)
            {
                if (e.reference != null)
                {
                    valids.Add(e);
                }
            }
            entries.Clear();
            entries.AddRange(valids);
    }

        public void Save(string path)
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        internal void Clear()
        {
            entries.Clear();
        }

        internal void Move(int i, int inserted)
        {
            entries.Move(i, inserted);
        }

        internal void Sort()
        {
            entries.ForEachIndex((e, i) => e.index = i);
            entries.Sort();
        }

        internal void AddRange(ShortcutList l)
        {
            entries.AddRange(l.entries);
        }

        internal ShortcutList GetStageRefs(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var list = new ShortcutList();
            foreach (var e in entries)
            {
                if (e.objRef.assetGuid == guid)
                {
                    list.Add(e);
                }
            }
            return list;
        }
    }
}
