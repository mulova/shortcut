using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.Ex;
using mulova.commons;
using mulova.unicore;
using Rotorz.Games.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Ex;
using Object = UnityEngine.Object;
using System.Ex;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System;
using UnityEngine.SceneManagement;

namespace mulova.shortcut
{

    public class ShortcutTab : EditorTab
    {

        private ShortcutSection section;
        private ShortcutWindow win;
        private WindowToolbar toolbar = new WindowToolbar();

        public ShortcutTab(string dir, string name, ShortcutWindow window) : base(window)
        {
            this.win = window;
            section = new ShortcutSection("Shortcut", dir);
            section.name = name;
            section.LoadRefs(dir);
            Init();
        }

        public ShortcutTab(ShortcutSection sect, ShortcutWindow window) : base(window)
        {
            this.win = window;
            section = sect;
            Init();
        }

        private void Init()
        {
            toolbar.AddMenuButton(new WindowToolbar.MenuButton { content = new GUIContent("Record Prefab"), callback = ToggleRecordPrefabOpen, getSelected = () => section.recordPrefab },
                new WindowToolbar.MenuButton { content = new GUIContent("Record modified"), callback = ToggleRecordModified, getSelected = ()=> section.recordModified },
                new WindowToolbar.MenuButton { content = new GUIContent("Access First", "Sort access first"), callback = ToggleAccessFirst, getSelected = ()=> section.accessFirst },
                new WindowToolbar.MenuButton { content = new GUIContent("Apply Cam", "Apply camera setting on prefab enter"), callback = ToggleApplyCam, getSelected = ()=> section.applyCam },
                new WindowToolbar.MenuButton { content = new GUIContent("Vertical", "Show Vertically"), callback = ToggleVertical, getSelected = ()=> section.vertical },
                new WindowToolbar.MenuButton { content = new GUIContent("Remove Missing", "Remove missing references"), callback = RemoveMissing }
            );

            void ToggleRecordPrefabOpen()
            {
                section.recordPrefab = !section.recordPrefab;
                section.Save();
            }

            void ToggleRecordModified()
            {
                section.recordModified = !section.recordModified;
                section.Save();
            }

            void ToggleAccessFirst()
            {
                section.accessFirst = !section.accessFirst;
                section.Save();
            }

            void ToggleApplyCam()
            {
                section.applyCam = !section.applyCam;
                section.Save();
            }

            void ToggleVertical()
            {
                section.vertical = !section.vertical;
                section.Save();
            }

            void RemoveMissing()
            {
                section.RemoveMissing();
                section.Save();
            }

        }

        public string name
        {
            get
            {
                return section.name;
            }
            set
            {
                if (section.name != value)
                {
                    section.name = value;
                }
            }
        }

        public override string ToString()
        {
            return section.name;
        }

        private ShortcutFilter assetFilter;
        private ShortcutFilter sceneFilter;
        public override void OnEnable()
        {
            OnFocus(true);
            assetFilter = new ShortcutFilter("Assets", true, null);
            sceneFilter = new ShortcutFilter("Scene Objects", false, null);
            PrefabStageCallbackManager.instance.RegisterOpen(OnPrefabStageOpen, int.MaxValue);
            PrefabStageCallbackManager.instance.RegisterClose(OnPrefabStageClose, int.MinValue);
            Selection.selectionChanged += OnSelection;
            EditorSceneManager.sceneOpening += OnSceneOpening;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.update += OnUpdate;
        }

        public override void OnDisable()
        {
            PrefabStageCallbackManager.instance.DeregisterOpen(OnPrefabStageOpen);
            PrefabStageCallbackManager.instance.DeregisterClose(OnPrefabStageClose);
            Selection.selectionChanged -= OnSelection;
            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorApplication.update -= OnUpdate;
        }

        private bool registered;
        private void OnSelection()
        {
            registered = Selection.activeObject != null && section.assetRefs.Contains(Selection.activeObject);
        }

        private void SaveSceneCamera(Scene scene)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            var r = section.assetRefs.Find(sceneAsset);
            if (r != null && r.CollectCam())
            {
                section.Save();
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (singleSceneNowOpening == scene.path)
            {
                singleSceneNowOpening = null;
            }
            if (mode == OpenSceneMode.Single)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                var r = section.assetRefs.Find(sceneAsset);
                if (r != null)
                {
                    r.ApplyCam();
                }
            }
        }

        private string singleSceneNowOpening;
        private void OnSceneOpening(string path, OpenSceneMode mode)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (mode == OpenSceneMode.Single)
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (!section.assetRefs.Contains(scene))
                {
                    section.AddObject(scene);
                } else
                {
                    section[path].CollectCam();
                    section.Save();
                }
                singleSceneNowOpening = path;
            }
        }

        private void OnSceneClosing(Scene s, bool removing)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            var item = section[s.path];
            if (item != null)
            {
                item.CollectCam();
                section.Save();
            }
        }

        private void OnPrefabStageOpen(PrefabStage p)
        {
            SaveSceneCamera(SceneManager.GetActiveScene());
            if (section.recordPrefab)
            {
#if UNITY_2020_1_OR_NEWER
                var prefab = AssetDatabase.LoadAssetAtPath<Object>(p.assetPath);
                //section.LoadStageRefs();
                section.ApplyCam(p.assetPath);
#else
                var prefab = AssetDatabase.LoadAssetAtPath<Object>(p.prefabAssetPath);
                //section.LoadStageRefs();
                section.ApplyCam(p.prefabAssetPath);
#endif
                AddAsset(prefab);
            }
        }

        private void OnPrefabStageClose(PrefabStage p)
        {
            var cur = PrefabStageUtility.GetCurrentPrefabStage();
            if (cur != p)
            {
                return;
            }
            SavePrefabStageCam(p);
        }

        private void SavePrefabStageCam(PrefabStage p)
        {
#if UNITY_2020_1_OR_NEWER
            var e = section[p.assetPath];
#else
            var e = section[p.prefabAssetPath];
#endif
            if (e != null && section.applyCam)
            {
                if (e.CollectCam())
                {
                    section.Save();
                }
            }
        }

        public override void AddContextMenu()
        {
            contextMenu.AddItem(new GUIContent("Shared"), section.isShared, ToggleShared);
            contextMenu.AddItem(new GUIContent("Apply Cam"), section.applyCam, ToggleCam);

            void ToggleShared()
            {
                section.isShared = !section.isShared;
            }

            void ToggleCam()
            {
                section.applyCam = !section.applyCam;
                section.Save();
            }
        }

        public override void OnTabChange(bool sel) { }
        public override void OnFocus(bool focus) { }

        public override void OnChangePlayMode(PlayModeStateChange stateChange)
        {
            OnChangeScene(null);
        }

        public override void OnChangeScene(string sceneName)
        {
            section.ClearCache();
            Repaint();
        }

        private void OnUpdate()
        {
            if (registered)
            {
                return;
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                SavePrefabStageCam(stage);
            } else if (section.recordModified && Selection.activeObject != null)
            {
                if (IsDirty(Selection.activeObject))
                {
                    section.AddObject(Selection.activeObject);
                    registered = true;
                }
            }
        }

        private bool IsDirty(Object o)
        {
            if (EditorUtility.IsDirty(o))
            {
                return true;
            }
            if (o is GameObject)
            {
                foreach (var c in (o as GameObject).GetComponents<Component>())
                {
                    if (c != null && EditorUtility.IsDirty(c))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void Repaint()
        {
            base.Repaint();
            pathMap.Clear();
        }

        public void AddSelection()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                section.AddAsset(AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            foreach (var o in Selection.gameObjects)
            {
                if (stage?.IsPartOfPrefabContents(o) ?? false)
                {
                    section.AddPrefabObject(o);
                } else
                {
                    if (o.scene.IsValid() && !o.scene.path.IsEmpty())
                    {
                        section.AddSceneObject(o);
                    }

                }
            }
            section.Save();
        }

        internal void AddAsset(Object o)
        {
            section.AddAsset(o);
            section.Save();
        }

        public void SelectAsset(int index)
        {
            section.SelectAsset(index);
            section.Save();
        }

        public void SelectSceneObject(int index)
        {
            section.SelectSceneObject(index);
        }

        private bool DrawShortcutList(ShortcutList list, ShortcutFilter filter, bool allowSceneObject)
        {
            AndPredicate<ShortcutItem> predicate = filter.GetPredicate(list.entries);
            var drawer = new ShortcutListDrawer(list, true);
            drawer.flags = ReorderableListFlags.ShowIndices;
            drawer.allowSceneObject = allowSceneObject;
            drawer.Filter(predicate.Accept);
            return drawer.Draw();
        }

        private Dictionary<Object, string> pathMap = new Dictionary<Object, string>();
        private string GetObjectPath(Object o)
        {
            string path = pathMap.Get(o);
            if (path != null)
            {
                return path;
            }
            path = AssetDatabase.GetAssetPath(o);
            if (path.IsEmpty())
            {
                var go = o.GetGameObject();
                if (o != null)
                {
                    path = go.transform.GetScenePath();
                }
                else
                {
                    path = o.ToString();
                }
            }
            pathMap[o] = path;
            return path;
        }

        public override void Remove()
        {
            section.Delete();
        }

        public override void OnHeaderGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (editName)
            {
                if (EditorGUILayoutEx.TextField("", ref section.name))
                {
                    editName = false;
                    section.Save();
                }
            }
            else
            {
                EditorGUILayout.LabelField(section.name);
                if (GUILayout.Button("Edit", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    editName = true;
                }
            }
            toolbar.Draw();
            EditorGUILayout.EndHorizontal();
        }

        private bool editName;
        public override void OnInspectorGUI()
        {
            bool changed = false;
            GUI.enabled = true;
            IDisposable rootScope = section.vertical ? (IDisposable)new EditorGUILayout.VerticalScope() : (IDisposable)new EditorGUILayout.HorizontalScope();
            using (rootScope)
            {
                ShortcutList assetRefs = section.assetRefs;
                ShortcutList stageRefs = section.stageRefs;
                var height = section.vertical && stageRefs.Count > 0 ? GUILayout.MaxHeight(win.position.height * assetRefs.Count/(assetRefs.Count+stageRefs.Count)) : GUILayout.MaxHeight(window.position.height);
                using (var scope = new EditorGUILayout.ScrollViewScope(section.scroll1, height))
                {
                    changed |= DrawShortcutList(assetRefs, assetFilter, false);
                    section.scroll1 = scope.scrollPosition;
                }
                if (stageRefs.Count > 0)
                {
                    using (var scope = new EditorGUILayout.ScrollViewScope(section.scroll2))
                    {
                        changed |= DrawShortcutList(stageRefs, sceneFilter, true);
                        section.scroll2 = scope.scrollPosition;
                    }
                }

                Object[] drag = EditorGUIEx.DnD();
                if (drag != null)
                {
                    foreach (Object o in drag)
                    {
                        if (o == null)
                        {
                            continue;
                        }
                        if (AssetDatabase.IsMainAsset(o) || AssetDatabase.IsSubAsset(o))
                        {
                            if (!assetRefs.Contains(o))
                            {
                                assetRefs.Add(o);
                            }
                        }
                        else
                        {
                            if (!stageRefs.Contains(o))
                            {
                                stageRefs.Add(o);
                            }
                        }
                        changed = true;
                    }
                }
                GUI.enabled = true;

                if (changed)
                {
                    section.Sort();
                    section.Save();
                    Repaint();
                }
            }
        }

        public override void OnFooterGUI()
        {
        }
    }
}
