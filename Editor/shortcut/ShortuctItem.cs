using System;
using mulova.unicore;
using Object = UnityEngine.Object;

namespace mulova.shortcut
{
    [Serializable]
    public class ShortcutItem : IComparable<ShortcutItem>
    {
        internal int index; // just for sorting
        public readonly string name;
        public ObjRef objRef;
        public bool starred;
        public SceneCamProperty cam;

        public Object reference
        {
            get
            {
                 return objRef.reference;
            }
            set
            {
                objRef.reference = value;
            }
        }
        public string id => objRef.assetGuid;
        public string path => objRef.path;

        public ShortcutItem(Object o)
        {
            objRef = new ObjRef(o);
            name = ToString();
        }

        public bool CollectCam()
        {
            if (EditorUtil.sceneView == null)
            {
                return false;
            }
            if (cam == null)
            {
                cam = new SceneCamProperty();
            }
            return cam.Collect();
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

        public override string ToString()
        {
            string path = objRef.path;
            if (path != null)
            {
                return path;
            }
            else
            {
                return string.Empty;
            }
        }

        public int CompareTo(ShortcutItem that)
        {
            if (this.starred ^ that.starred)
            {
                return this.starred ? -1 : 1;
            }
            else
            {
                return index - that.index;
            }
        }
    }

}
