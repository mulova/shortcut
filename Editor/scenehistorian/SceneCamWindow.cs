using System;
using mulova.unicore;
using UnityEditor;
using UnityEngine;

namespace mulova.scenehistorian
{

    public class SceneCamWindow : EditorWindow
    {
        private const string PATH = "Library/scenehistorian/cam";
        private SceneCamHistory history;
        private SceneViewContextMenu contextMenu;

        private ListDrawer<SceneCamProperty> listDrawer;

        public static SceneCamWindow instance
        {
            get
            {
                return EditorWindow.GetWindow<SceneCamWindow>("Scene Camera");
            }
        }

        [MenuItem("Tools/SceneHistorian/Scene Cam")]
        private static void OpenWindow()
        {
            instance.Show();
        }

        void OnEnable()
        {
            contextMenu = new SceneViewContextMenu();
            history = SceneCamHistory.Load(PATH);
            contextMenu.AddContextMenu(menu=> {
                foreach (var h in instance.history.items)
                {
                    menu.AddItem(new GUIContent("cam/"+h.id), false, OnCamMenu, h);
                }
            }, 10);
            listDrawer = new ListDrawer<SceneCamProperty>(history.items, new SceneCamPropertyDrawer());
            listDrawer.addSelected = false;
            listDrawer.createDefaultValue = () => new SceneCamProperty();
            listDrawer.onDuplicate += AddItem;
            listDrawer.onInsert += AddItem;

            void AddItem(int i, SceneCamProperty item)
            {
                int no = i;
                while (history["cam"+no] != null)
                {
                    ++no;
                }
                item.id = "cam" + no;
                item.Collect();
            }
        }


        void OnDisable()
        {
            contextMenu.Dispose();
            // Enter play mode
            if (!Application.isPlaying)
            {
                history.Save(PATH);
            }
        }

        private void OnCamMenu(object h)
        {
            SceneCamProperty p = h as SceneCamProperty;
            p.Apply();
        }

        void OnHeaderGUI()
        {
        }

        void OnFooterGUI()
        {
        }

        private Vector3 scrollPos;
        void OnGUI()
        {
            OnHeaderGUI();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            try
            {
				if (listDrawer.Draw())
				{
					history.Save(PATH);
				}
            } catch (ExitGUIException ex)
            {
                //if (!(ex.GetBaseException() is ExitGUIException))
                //{
                //    throw ex.GetBaseException();
                //}
            }
            EditorGUILayout.EndScrollView();
            OnFooterGUI();
        }
    }
}
