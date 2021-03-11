using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Ex;
using System.IO;
using System.Text.Ex;
using mulova.unicore;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace mulova.scenehistorian
{
    [Serializable]
    public class SceneHistoryItem : IComparable<SceneHistoryItem>
    {
        public readonly string name;
        public List<ObjRef> list;
        public bool starred;
		public int activeIndex;

        public SceneCamProperty cam;

		public ObjRef activeScene
		{
			get
			{
                if (list.Count <= activeIndex)
                {
                    return list[0];
                }
				return list[activeIndex];
			}
		}

        public SceneHistoryItem(Object o)
        {
            list = new List<ObjRef>();
            list.Add(new ObjRef(o));
            name = ToString();
        }

        public void SaveCam()
        {
			if (EditorUtil.sceneView == null)
			{
				return;
			}
			if (cam == null)
			{
				cam = new SceneCamProperty();
			}
			cam.Collect();
        }

		public void ApplyCam()
		{
            if (EditorUtil.sceneView == null)
			{
				return;
			}
			if (cam != null)
			{
				cam.Apply();
			}
		}

        public ObjRef first
        {
            get
            {
                return list.Get(0);
            }
        }

        public Object firstRef
        {
            get
            {
                var o = first;
                if (o != null)
                {
                    return o.reference;
                } else
                {
                    return null;
                }
            }
        }

        public void AddScene(Object sceneObj)
        {
            list.Add(new ObjRef(sceneObj));
        }

        public void RemoveScene(Object sceneObj)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].reference == sceneObj)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        public bool Contains(Object sceneObj)
        {
            foreach (var o in list)
            {
                if (o.reference == sceneObj)
                {
                    return true;
                }
            }
            return false;
        }

        public void LoadAdditiveScenes()
        {
            for (int i = 1; i < list.Count; ++i)
            {
                if (!list[i].path.IsEmpty())
                {
                    var s = EditorSceneManager.OpenScene(list[i].path, OpenSceneMode.Additive);
				    if (activeIndex == i)
				    {
					    EditorSceneManager.SetActiveScene(s);
				    }
                }
            }
        }

		public override string ToString()
		{
			string path = list[0].path;
			if (path != null) {
                return path;
			} else {
				return string.Empty;
			}
		}

        public int CompareTo(SceneHistoryItem that)
        {
            if (this.starred^that.starred)
            {
                return this.starred? -1 : 1;
            } else
            {
                var str1 = this.name;
                var str2 = that.name;
                if (str1 != null)
                {
                    if (str2 != null)
                    {
                        return str1.CompareTo(str2);
                    } else
                    {
                        return -1;
                    }
                } else
                {
                    if (str2 != null)
                    {
                        return 1;
                    } else
                    {
                        return 0;
                    }
                }
            }
        }

		public void SetActiveScene(string path)
		{
			int index = list.FindIndex(id => id.path.EqualsIgnoreSeparator(path));
			if (index >= 0)
			{
				activeIndex = index;
			}
		}
    }

}
