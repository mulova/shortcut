﻿using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.IO;
using UnityEditor;

namespace mulova.scenehistorian
{
	[System.Serializable]
	public class SceneHistory
	{
		[SerializeField] private List<SceneHistoryItem> _items = new List<SceneHistoryItem>();
		[SerializeField] internal int maxSize = 100;
        [SerializeField] internal bool sort;
		[SerializeField] internal bool useCam = true;

		private class StarredSorter : IComparer<SceneHistoryItem>
		{
            private List<SceneHistoryItem> items;

            public StarredSorter(List<SceneHistoryItem> items)
            {
				this.items = new List<SceneHistoryItem>(items);
            }

			public int Compare(SceneHistoryItem item1,SceneHistoryItem item2)
			{
				int i1 = items.IndexOf(item1);
				int i2 = items.IndexOf(item2);
				if (i1 < 2 || i2 < 2)
                {
					return i1 - i2;
                } else if (i2 < 2)
                {
					return 1;
                }
				if (item1.starred ^ item2.starred)
                {
					return item1.starred ? -1 : 1;
                }
				return i1-i2;
			}
		}

		public List<SceneHistoryItem> items => _items;


		public int Count => items.Count;

		public SceneHistoryItem this[int i]
		{
			get
			{
                if (i < items.Count)
                {
				    return items[i];
                } else
                {
                    return null;
                }
			}
		}

		public void Add(SceneHistoryItem item)
		{
			items.Add(item);
			Resize();
		}

		public void RemoveAt(int i)
		{
			items.RemoveAt(i);
		}

		public void Insert(int i, SceneHistoryItem item)
		{
			items.Insert(i, item);
			Resize();
		}

		public void Clear()
		{
			items.Clear();
		}

		private void Resize()
		{
			int i = Count-1;
			while (Count > maxSize && i >= 0)
			{
				if (!items[i].starred)
				{
					items.RemoveAt(i);
				}
				i--;
			}
		}

		public void Add(Object obj)
		{
			Add(new SceneHistoryItem(obj));
		}

		public int IndexOf(SceneAsset s)
		{
			for (int i=0; i<Count; ++i)
			{
                var r = this[i].list[0].reference;
                if (this[i].list.Count > 0 && (r == s || (r != null && r.Equals(s))))
				{
					return i;
				}
			}
			return -1;
		}

		public int IndexOf(string path)
		{
			for (int i=0; i<Count; ++i)
			{
				if (this[i].list.Count > 0 &&  this[i].list[0].path == path)
				{
					return i;
				}
			}
			return -1;
		}

		public void Insert(int index, Object obj)
		{
			Insert(index, new SceneHistoryItem(obj));
		}

		public void Remove(Object obj)
		{
			int index = items.FindIndex(o => o.firstRef == obj);
			if (index >= 0)
			{
				items.RemoveAt(index);
			}
		}

		public void Remove(string guid)
		{
			int index = items.FindIndex(o => o.first != null && o.first.assetGuid == guid);
			if (index >= 0)
			{
				items.RemoveAt(index);
			}
		}

		public bool Contains(SceneAsset obj)
		{
			return IndexOf(obj) >= 0;
		}

        public bool Contains(string path)
        {
            return IndexOf(path) >= 0;
        }

		public void Sort()
        {
			_items.Sort(new StarredSorter(_items));
        }

        public static SceneHistory Load(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					var text = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(text))
					{
						var h =JsonUtility.FromJson<SceneHistory>(text);
						if (h.sort)
                        {
							h.Sort();
                        }
						return h;
					}
				}
			} catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
			}
			return new SceneHistory();
		}

		public void Save(string path)
		{
			try
			{
				if (sort)
				{
					Sort();
				}
				var json = JsonUtility.ToJson(this);
				File.WriteAllText(path, json);
			} catch(Exception ex)
			{
				Debug.LogError(ex.ToString());
			}
		}

        internal void RemoveMissing()
        {
            var valids = new List<SceneHistoryItem>();
            foreach (var i in _items)
            {
                if (i.firstRef != null)
                {
                    valids.Add(i);
                }
            }
            _items.Clear();
            _items.AddRange(valids);
        }
    }
}
