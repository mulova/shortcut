using mulova.unicore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Ex;
using UnityEditor.SceneManagement;

namespace mulova.shortcut
{
    public class ShortcutItemDrawer : ItemDrawer<ShortcutItem>
    {
        private bool useCam;
        private GUIContent favoriteIcon;
        private GUIContent loadCamIcon;
        private GUIContent saveCamIcon;

        public ShortcutItemDrawer(bool useCam)
        {
            this.useCam = useCam;
            favoriteIcon = new GUIContent(EditorBuiltInIcon.FavoriteIcon, "Save search");
            loadCamIcon = new GUIContent(EditorBuiltInIcon.VisibilityOn);
            saveCamIcon = new GUIContent(EditorBuiltInIcon.SceneSave);
        }

        public override bool DrawItem(Rect bound, int index, ShortcutItem item, out ShortcutItem newItem)
        {
            var isInstance = item.objRef.category == ObjCategory.SceneInstance || item.objRef.category == ObjCategory.PrefabInstance;
            var showCam = useCam && (item.cam?.valid?? false);
            var rightWidth = showCam ? (isInstance? 90:60) : 30;
            newItem = item;
            Rect[] refArea = bound.SplitByWidths((int)bound.width - rightWidth);
            Object obj = item.reference as Object;
            try
            {
                var content = obj != null? EditorGUIUtility.ObjectContent(obj, obj.GetType()): new GUIContent("Null");
                if (obj != null && item.objRef.category != ObjCategory.Asset)
                {
                    content.text = item.objRef.ToString();
                    EditorGUI.LabelField(refArea[0], content, EditorStyles.objectField);
                    int clickCount = EditorGUIEx.GetClick();
                    if (clickCount == 1)
                    {
                        EditorGUIUtility.PingObject(obj);
                    } else if (clickCount == 2)
                    {
                        if (item.objRef.category == ObjCategory.SceneInstance)
                        {
                            Selection.activeObject = obj;
                        }
                        else if (item.objRef.category == ObjCategory.PrefabInstance)
                        {
                            AssetDatabase.OpenAsset(obj);
                        } else if (obj is SceneAsset)
                        {
                            EditorSceneManager.OpenScene(item.objRef.assetPath);
                        }
                    }
                } else
                {
                    var newObj = EditorGUI.ObjectField(refArea[0], obj, typeof(Object), false);
                    if (newObj != obj)
                    {
                        item.reference = newObj;
                    }
                }
                Rect starredRect = refArea[1];
                if (showCam)
                {
                    Rect[] loadCamArea = refArea[1].SplitByWidths(30);
                    if (GUI.Button(loadCamArea[0], loadCamIcon))
                    {
                        item.cam.Apply();
                    }
                    if (isInstance)
                    {
                        var saveCamArea = loadCamArea[1].SplitByWidths(30);
                        if (GUI.Button(saveCamArea[0], saveCamIcon))
                        {
                            item.cam.Collect();
                        }
                        starredRect = saveCamArea[1];
                    } else
                    {
                        starredRect = loadCamArea[1];
                    }
                }
                bool starred = item.starred;
                using (new ContentColorScope(starred ? Color.cyan : Color.black))
                {
                    if (GUI.Button(starredRect, favoriteIcon))
                    {
                        item.starred = !item.starred;
                    }
                }
                return starred != item.starred || obj != item.reference;
            }
#pragma warning disable 0168
            catch (ExitGUIException ex)
#pragma warning restore 0168
            {
                return false;
            }
        }

        public override float GetItemHeight(int index, ShortcutItem obj)
        {
            return 20;
        }
    }
}

