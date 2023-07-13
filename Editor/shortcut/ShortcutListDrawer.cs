using System.Collections.Generic.Ex;
using System.Ex;
using System.Text.Ex;
using mulova.unicore;
using UnityEditor;
using Object = UnityEngine.Object;

namespace mulova.shortcut
{
    public class ShortcutListDrawer : ListDrawer<ShortcutItem>
    {
        public bool allowSceneObject = true;

        public ShortcutListDrawer(ShortcutList list, bool useCam) : base(list.entries, new ShortcutItemDrawer(useCam))
        {
            this.createDefaultValue = () => CreateItem(Selection.activeObject);
            this.createItem = CreateItem;
        }

        private ShortcutItem CreateItem(Object o)
        {
            if (allowSceneObject || !AssetDatabase.GetAssetPath(o).IsEmpty())
            {
                var s = new ShortcutItem(o);
                s.CollectCam();
                return s;
            }
            else
            {
                return new ShortcutItem(null);
            }
        }
    }
}
