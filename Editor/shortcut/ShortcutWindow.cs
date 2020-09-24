using System.Collections.Generic;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEngine;

namespace mulova.shortcut
{
    public class ShortcutWindow : TabbedEditorWindow {

        public const string dir = "Shortcut";

        protected override void CreateTabs()
        {
            LoadTabs(dir);
            LoadTabs("Library/"+dir);
            if (tabCount == 0)
            {
                AddTab();
            }
            SetClosable(true);
            titleContent.text = "Shortcut";
        }

		private void LoadTabs(string directory) {
            var files = AssetUtil.ListFiles(directory, "*" + ShortcutSection.SECT_EXT);
            if (files.Length > 0)
            {
                var sections = new List<ShortcutSection>();
                foreach (var f in files)
                {
                    var sect = JsonUtil.ReadJson<ShortcutSection>(f.FullName);
                    if (sect != null)
                    {
                        sect.LoadRefs(EditorAssetUtil.GetProjectRelativePath(f.Directory.FullName));
                        sections.Add(sect);
                    }
                }
                sections.Sort();
                foreach (var s in sections)
                {
                    AddTab(new ShortcutTab(s, this));
                }
            }
		}

        [MenuItem("Tools/Shortcut/Memorize Selection %#M")]
        public static void AddSelection()
        {
            var window = GetWindow<ShortcutWindow>();
            var tab = window.activeTab as ShortcutTab;
            tab.AddSelection();
        }

        [MenuItem("Tools/Shortcut/Shortcut Window")]
        public static ShortcutWindow Get()
        {
            // Get existing open window or if none, make a new one:
            ShortcutWindow window = GetWindow<ShortcutWindow>();
            window.titleContent = new GUIContent("Shortcut");
            window.Show();
            return window;
        }

        protected override void AddContextMenu()
        {
            // Now create the menu, add items and show it
            contextMenu.AddItem(new GUIContent("New Tab"), false, AddTab);
            contextMenu.AddItem(new GUIContent("Remove Tab"), false, RemoveCurrentTab);
        }

        private void AddTab()
        {
            var tab = new ShortcutTab(dir, "Shortcut" + (tabCount + 1), this);
            AddTab(tab);
            activeTab = tab;
        }

        private void RemoveCurrentTab()
        {
            var tab = activeTab as ShortcutTab;
            tab.Remove();
            RemoveTab(tab);
        }

        public static void SelectAsset(int index)
        {
            // Get existing open window or if none, make a new one:
            ShortcutWindow window = GetWindow<ShortcutWindow>();
            ShortcutTab tab = window.activeTab as ShortcutTab;
            tab.SelectAsset(index);
        }

        public static void SelectSceneObject(int index)
        {
            // Get existing open window or if none, make a new one:
            ShortcutWindow window = GetWindow<ShortcutWindow>();
            ShortcutTab tab = window.activeTab as ShortcutTab;
            tab.SelectSceneObject(index);
        }

        [MenuItem("Tools/Shortcut/Load Asset 1 %F1")]
        public static void LoadAsset1()
        {
            SelectAsset(0);
        }

        [MenuItem("Tools/Shortcut/Load Asset 2 %F2")]
        public static void LoadAsset2()
        {
            SelectAsset(1);
        }
        [MenuItem("Tools/Shortcut/Load Asset 3 %F3")]
        public static void LoadAsset3()
        {
            SelectAsset(2);
        }
        [MenuItem("Tools/Shortcut/Load Asset 4 %F4")]
        public static void LoadAsset4()
        {
            SelectAsset(3);
        }
        [MenuItem("Tools/Shortcut/Load Asset 5 %F5")]
        public static void LoadAsset5()
        {
            SelectAsset(4);
        }
        [MenuItem("Tools/Shortcut/Load Asset 6 %F6")]
        public static void LoadAsset6()
        {
            SelectAsset(5);
        }
        [MenuItem("Tools/Shortcut/Load Asset 7 %F7")]
        public static void LoadAsset7()
        {
            SelectAsset(6);
        }
        [MenuItem("Tools/Shortcut/Load Asset 8 %F8")]
        public static void LoadAsset8()
        {
            SelectAsset(7);
        }
        [MenuItem("Tools/Shortcut/Load Asset 9 %F9")]
        public static void LoadAsset9()
        {
            SelectAsset(8);
        }
        [MenuItem("Tools/Shortcut/Load Asset 10 %F10")]
        public static void LoadAsset10()
        {
            SelectAsset(9);
        }
        [MenuItem("Tools/Shortcut/Load Asset 11 %F11")]
        public static void LoadAsset11()
        {
            SelectAsset(10);
        }
        [MenuItem("Tools/Shortcut/Load Asset 12 %F12")]
        public static void LoadAsset12()
        {
            SelectAsset(11);
        }

        [MenuItem("Tools/Shortcut/Load SceneObject 1 %#F1")]
        public static void LoadSceneObject1()
        {
            SelectSceneObject(0);
        }

        [MenuItem("Tools/Shortcut/Load SceneObject 2 %#F2")]
        public static void LoadSceneObject2()
        {
            SelectSceneObject(1);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 3 %#F3")]
        public static void LoadSceneObject3()
        {
            SelectSceneObject(2);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 4 %#F4")]
        public static void LoadSceneObject4()
        {
            SelectSceneObject(3);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 5 %#F5")]
        public static void LoadSceneObject5()
        {
            SelectSceneObject(4);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 6 %#F6")]
        public static void LoadSceneObject6()
        {
            SelectSceneObject(5);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 7 %#F7")]
        public static void LoadSceneObject7()
        {
            SelectSceneObject(6);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 8 %#F8")]
        public static void LoadSceneObject8()
        {
            SelectSceneObject(7);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 9 %#F9")]
        public static void LoadSceneObject9()
        {
            SelectSceneObject(8);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 10 %#F10")]
        public static void LoadSceneObject10()
        {
            SelectSceneObject(9);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 11 %#F11")]
        public static void LoadSceneObject11()
        {
            SelectSceneObject(10);
        }
        [MenuItem("Tools/Shortcut/Load SceneObject 12 %#F12")]
        public static void LoadSceneObject12()
        {
            SelectSceneObject(11);
        }
    }
}
