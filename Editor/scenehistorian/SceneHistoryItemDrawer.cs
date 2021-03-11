using UnityEngine;
using mulova.unicore;
using UnityEditor;
using UnityEngine.Ex;

namespace mulova.scenehistorian
{
    public class SceneHistoryItemDrawer : ItemDrawer<SceneHistoryItem>
    {
        private bool useCam;
        private GUIContent favoriteIcon;
        private GUIContent camIcon;

		public SceneHistoryItemDrawer(bool useCam)
		{
            this.useCam = useCam;
            favoriteIcon = new GUIContent(EditorBuiltInIcon.Favorite);
            camIcon = new GUIContent(EditorBuiltInIcon.VisibilityOn);
        }

        public override bool DrawItem(Rect rect, int index, SceneHistoryItem item, out SceneHistoryItem newItem)
        {
            var showCam = useCam && (item.cam?.valid ?? false);
            var rightWidth = showCam? 60 : 30;
            newItem = item;
            Object obj = item.first.reference as Object;

            try
            {
                var area1 = rect.SplitByWidths((int)rect.width - rightWidth);
                if (obj != null)
                {
                    var newObj = EditorGUI.ObjectField(area1[0], obj, typeof(Object), false);
                    if (newObj != obj)
                    {
                        item.first.reference = newObj;
                    }
                    Rect starredRect = area1[1];
                    if (showCam)
                    {
                        var area2 = area1[1].SplitByWidths(30);
                        if (showCam && GUI.Button(area2[0], camIcon))
                        {
                            item.cam.Apply();
                        }
                        starredRect = area2[1];
                    }
                    bool starred = item.starred;
                    Color cc = GUI.contentColor;
                    GUI.contentColor = starred? Color.cyan: Color.black;

                    if (GUI.Button(starredRect, favoriteIcon))
                    {
                        item.starred = !item.starred;
                    }
                    GUI.contentColor = cc;
                    return starred != item.starred || obj != item.first.reference;
                } else
                {
                    EditorGUI.LabelField(rect, item.name);
                    return false;
                }
            }
#pragma warning disable 0168
            catch (ExitGUIException ex)
#pragma warning restore 0168
            {
                return false;
            }
        }

        public override float GetItemHeight(int index, SceneHistoryItem obj)
        {
            return 20;
        }
    }
}