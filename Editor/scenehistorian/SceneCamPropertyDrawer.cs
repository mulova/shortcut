//#define CONFIRM
using UnityEngine;
using mulova.unicore;
using UnityEditor;
using System;
using UnityEngine.Ex;

namespace mulova.scenehistorian
{
    public class SceneCamPropertyDrawer : ItemDrawer<SceneCamProperty>
    {
        public const int CONFIRM_PERIOD = 2;
        private GUIContent saveIcon;
        private GUIContent loadIcon;

		public SceneCamPropertyDrawer()
		{
            loadIcon = new GUIContent(EditorBuiltInIcon.VisibilityOn, "Load");
            saveIcon = new GUIContent(EditorBuiltInIcon.SceneSave, "Save");
		}

        private static DateTime time;
        private static SceneCamProperty toSave;
        public override bool DrawItem(Rect rect, int index, SceneCamProperty item, out SceneCamProperty newItem)
        {
            bool changed = false;
            Rect[] r = rect.SplitByWidths((int)rect.size.x-90,  60);
            if (item == null)
            {
                item = new SceneCamProperty();
                item.Collect();
            }
            string name = EditorGUI.TextField(r[0], item.id);
            if (GUI.Button(r[1], loadIcon))
            {
                item.Apply();
            }
            Color bg = GUI.backgroundColor;
            #if CONFIRM
            TimeSpan diff = System.DateTime.Now-time;
            if (diff.TotalSeconds < CONFIRM_PERIOD)
            {
                if (toSave == item)
                {
                    GUI.backgroundColor = Color.red;
                }
            } else
            {
                toSave = null;
            }
            #endif
            if (GUI.Button(r[2], saveIcon) && EditorUtility.DisplayDialog("Save", "Save Cam?", "Ok", "Cancel"))
            {
                #if CONFIRM
                if (toSave == item)
                {
                    item.Collect();
                    toSave = null;
                } else
                {
                    toSave = item;
                }
                time = System.DateTime.Now;
                #else
                item.Collect();
                changed = true;
                #endif
            }
            GUI.backgroundColor = bg;
            newItem = item;
            if (name != item.id)
            {
                item.id = name;
                return true;
            } else
            {
                return changed;
            }
        }

        public override float GetItemHeight(int index, SceneCamProperty p)
        {
            return 20;
        }
    }
}
