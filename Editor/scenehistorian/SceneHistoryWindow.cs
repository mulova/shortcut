using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.IO;
using System.Text.Ex;
using mulova.unicore;
using Rotorz.Games.Collections;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace mulova.scenehistorian
{
    public class SceneHistoryWindow : EditorWindow
    {
        private SceneHistory sceneHistory;
        private const string PATH = "Library/scenehistorian/history";
        private bool changed;
        private string nameFilter;
        private ListDrawer<SceneHistoryItem> listDrawer;
        private const bool SHOW_SIZE = false;
        private SceneViewContextMenu contextMenu;

        private static readonly Color SORT_COLOR = Color.green;

        private bool valid
        {
            get
            {
                return !BuildPipeline.isBuildingPlayer;
            }
        }

        public static SceneHistoryWindow instance
        {
            get
            {
                return GetWindow<SceneHistoryWindow>("SceneHistorian");
            }
        }


        void OnEnable()
        {
            contextMenu = new SceneViewContextMenu();
            //showMenuIcon = new GUIContent("...", "Sort");
			var dir = Path.GetDirectoryName(PATH);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
            sceneHistory = SceneHistory.Load(PATH);
			OnSceneOpened(SceneManager.GetActiveScene(), OpenSceneMode.Single);
            #if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged += OnSceneObjChange;
            #else
            EditorApplication.hierarchyWindowChanged += OnSceneObjChange;
            #endif
			if (!BuildPipeline.isBuildingPlayer)
			{
				EditorApplication.pauseStateChanged += OnPauseStateChanged;
				EditorSceneManager.sceneOpening += OnSceneOpening;
				EditorSceneManager.sceneOpened += OnSceneOpened;
				EditorSceneManager.sceneClosing += OnSceneClosing;
				SceneManager.activeSceneChanged += OnActiveScene;
                SceneManager.sceneLoaded += OnSceneLoaded;
                PrefabStageCallbackManager.instance.RegisterOpen(OnPrefabStageOpen, 101);
                PrefabStageCallbackManager.instance.RegisterClose(OnPrefabStageClose, 102);
            }

            #if UNITY_2017_1_OR_NEWER
            EditorApplication.playModeStateChanged += ChangePlayMode;
            #else
            EditorApplication.playmodeStateChanged += ChangePlaymode;
            #endif
			var history = sceneHistory.items;
			if (history.Count > 0)
			{
				contextMenu.AddContextMenu(menu=> {
					if (history.Count >= 2)
					{
						menu.AddItem(new GUIContent("Previous: " + history[1].name), false, GoBack);
					}
					for (int i=2; i<history.Count; ++i)
					{
						if (history[i].starred)
						{
							menu.AddItem(new GUIContent("scenes/"+ history[i].name), false, OnSceneMenu, history[i]);
						}
					}
				}, 1);
			}
        }

        void OnDisable()
        {
            contextMenu.Dispose();
            // Enter play mode
            if (!Application.isPlaying)
			{
				SaveCam();
				sceneHistory.Save(PATH);
			}
            #if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged -= OnSceneObjChange;
            #else
            EditorApplication.hierarchyWindowChanged -= OnSceneObjChange;
            #endif
			EditorApplication.pauseStateChanged -= OnPauseStateChanged;
			EditorSceneManager.sceneOpening -= OnSceneOpening;
			EditorSceneManager.sceneOpened -= OnSceneOpened;
			EditorSceneManager.sceneClosing -= OnSceneClosing;
			SceneManager.activeSceneChanged -= OnActiveScene;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PrefabStageCallbackManager.instance.DeregisterOpen(OnPrefabStageOpen);
            PrefabStageCallbackManager.instance.DeregisterClose(OnPrefabStageClose);

#if UNITY_2017_1_OR_NEWER
            EditorApplication.playModeStateChanged += ChangePlayMode;
            #else
            EditorApplication.playmodeStateChanged += ChangePlaymode;
            #endif
        }

        private void OnActiveScene(Scene s1, Scene s2)
		{
			if (Application.isPlaying) {
				return;
			}
			if (sceneHistory.Count == 0)
			{
				return;
			}
			if (!sceneHistory[0].activeScene.path.EqualsIgnoreSeparator(s2.path)) {
				sceneHistory[0].SetActiveScene(s2.path);
				sceneHistory.Save(PATH);
			}
		}

		void OnPauseStateChanged(PauseState state)
		{
//			if (sceneHistory.Count >= 0)
//			{
//				sceneHistory[0].ApplyCam();
//			}
		}

        private void OnSceneObjChange()
        {
            changed = true;
        }

        private void ChangePlayMode(PlayModeStateChange stateChange)
        {
			if (BuildPipeline.isBuildingPlayer)
			{
				return;
			}
			if (stateChange == PlayModeStateChange.EnteredEditMode)
			{
				if (sceneHistory.Count >= 0 && sceneHistory.useCam)
				{
					sceneHistory[0].ApplyCam();
				}
			}
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //			if (mode != LoadSceneMode.Single)
            //			{
            //				return;
            //			}
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            ApplySceneCam(scene);
        }

        private void ApplySceneCam(Scene scene)
        {
            if (sceneHistory.useCam)
            {
                int index = sceneHistory.IndexOf(scene.path);
                if (index >= 0)
                {
                    sceneHistory[index].ApplyCam();
                }
            }
        }

        private void OnPrefabStageOpen(PrefabStage obj)
        {
            SaveCam();
        }

        private void OnPrefabStageClose(PrefabStage obj)
        {
            ApplySceneCam(SceneManager.GetActiveScene());
        }

        private void SaveCam()
        {
            if (sceneHistory.Count > 0)
            {
                var item = sceneHistory[0];
                item.SaveCam();
            }
        }

        private string singleSceneNowOpening;
        private void OnSceneOpening(string path,OpenSceneMode mode)
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
                SaveCam();
                singleSceneNowOpening = path;
    			sceneHistory.Save(PATH);
			}
        }

        private void OnSceneOpened(Scene s, OpenSceneMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            if (singleSceneNowOpening == s.path)
            {
                singleSceneNowOpening = null;
            }

            if (mode == OpenSceneMode.Single)
            {
                int index = sceneHistory.IndexOf(s.path);
                if (index >= 0)
                {
                    var item = sceneHistory[index];
                    sceneHistory.RemoveAt(index);
                    sceneHistory.Insert(0, item);
                    item.LoadAdditiveScenes();
                    if (sceneHistory.useCam)
                    {
                        item.ApplyCam();
                    }
                } else
                {
                    var sceneObj = AssetDatabase.LoadAssetAtPath<Object>(s.path);
                    var item = new SceneHistoryItem(sceneObj);
                    item.SaveCam();
                    sceneHistory.Insert(0, item);
                }
            } else
            {
                var item = sceneHistory[0];
                var sceneObj = AssetDatabase.LoadAssetAtPath<Object>(s.path);
                if (!item.Contains(sceneObj))
                {
                    item.AddScene(sceneObj);
                }
            }
            sceneHistory.Save(PATH);
            changed = false;
        }

        private void OnSceneClosing(Scene s, bool removing)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            var firstScenePath = sceneHistory[0]?.first?.path;
            if (firstScenePath == s.path)
            {
				SaveCam();
			} else
			{
				if (singleSceneNowOpening == firstScenePath) // feasible only when the main scene is not closing
				{
				    var firstScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScenePath);
				    int index = sceneHistory.IndexOf(firstScene);
				    SceneHistoryItem item = null;
				    if (index >= 0)
				    {
					    item = sceneHistory[index];
					    var closingScene = AssetDatabase.LoadAssetAtPath<Object>(s.path);
					    if (item.Contains(closingScene))
					    {
						    item.RemoveScene(closingScene);
					    }
				    } else
				    {
					    item = new SceneHistoryItem(firstScene);
					    sceneHistory.Insert(0, item);
				    }
				}
			}
            sceneHistory.Save(PATH);
            changed = false;
        }

        public void GoBack()
        {
            LoadScene(1);
        }

        public void LoadScene(int i)
        {
            if ((!changed || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) && sceneHistory.Count > i)
            {
                EditorSceneManager.OpenScene(sceneHistory[i].first.path);
            }
        }

        void OnInspectorUpdate() {
			if (!Application.isPlaying)
			{
				// activeSceneChanged event is not available in Editor mode
				var s = SceneManager.GetActiveScene();
				OnActiveScene(s, s);
			}
		}

        private  void OnSceneMenu(object h)
        {
            SceneHistoryItem hist = h as SceneHistoryItem;
            EditorSceneManager.OpenScene(hist.first.path);
        }

        private WindowToolbar toolbar;
        private List<ObjRef> allScenes = new List<ObjRef>();
        public void OnHeaderGUI()
        {
            if (toolbar == null)
            {
                toolbar = new WindowToolbar();
                toolbar.AddButton(new GUIContent("Back"), GoBack, 50,
                    isEnabled: () => sceneHistory.Count >= 2);
                toolbar.AddMenuButton(
                    new WindowToolbar.MenuButton
                    {
                        content = new GUIContent("Sort"),
                        callback = ToggleSort,
                        getSelected = () => sceneHistory.sort
                    },
                    new WindowToolbar.MenuButton
                    {
                        content = new GUIContent("Cam"),
                        callback = ToggleCam,
                        getSelected = () => sceneHistory.useCam
                    },
                    new WindowToolbar.MenuButton
                    {
                        content = new GUIContent("Clear"),
                        callback = ClearHistory
                    },
                    new WindowToolbar.MenuButton
                    {
                        content = new GUIContent("Remove missing"),
                        callback = RemoveMissing
                    }
                );

                void ToggleSort()
                {
                    sceneHistory.sort = !sceneHistory.sort;
                    sceneHistory.Save(PATH);
                }

                void ToggleCam()
                {
                    sceneHistory.useCam = !sceneHistory.useCam;
                    sceneHistory.Save(PATH);

                }
                void ClearHistory()
                {
                    if (EditorUtility.DisplayDialog("Warning", "Clear history?", "Ok", "Cancel"))
                    {
                        sceneHistory.Clear();
                        File.Delete(PATH);
                    }
                }
                void RemoveMissing()
                {
                    sceneHistory.RemoveMissing();
                    sceneHistory.Save(PATH);
                }
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (EditorGUILayoutEx.SearchField("", ref nameFilter))
                {
                    if (nameFilter.IsEmpty())
                    {
                        allScenes.Clear();
                        var guids = AssetDatabase.FindAssets("t:Scene");
                        foreach (var id in guids)
                        {
                            allScenes.Add(new ObjRef(AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(id))));
                        }
                    }
                }
                toolbar.Draw();
                //GUI.enabled = sceneHistory.Count >= 2;
                //if (GUILayout.Button("Back", EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(20)))
                //{
                //    GoBack();
                //}
                //var color = GUI.contentColor;
                //if (sceneHistory.sort)
                //{
                //    GUI.contentColor = SORT_COLOR;
                //}
                //if (GUILayout.Button(sortIcon, EditorStyles.toolbarButton, GUILayout.Width(30), GUILayout.Height(20)))
                //{
                //    ShowMenu();
                //}
                //GUI.contentColor = color;
            }
        }

        private ListDrawer<SceneHistoryItem> CreateDrawer(SceneHistory list)
        {
            var drawer = new ListDrawer<SceneHistoryItem>(list.items, new SceneHistoryItemDrawer(list.useCam));
            drawer.createDefaultValue = () => new SceneHistoryItem(Selection.activeObject);
            drawer.createItem = o=>new SceneHistoryItem(o);
            return drawer;
        }

        private Vector3 scrollPos;
        void OnGUI()
        {
            listDrawer = CreateDrawer(sceneHistory);
            OnHeaderGUI();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (!nameFilter.IsEmpty())
            {
                string[] filters = nameFilter.SplitEx(' ');
                Predicate<SceneHistoryItem> filter = h =>
                {
                    if (h.first == null || h.first.path.IsEmpty())
                    {
                        return false;
                    }
                    string itemName = h.name;
                    foreach (var n in filters)
                    {
                        if (itemName.IndexOfIgnoreCase(n) < 0)
                        {
                            return false;
                        }
                    }
                    return true;
                };
                listDrawer.Filter(filter);
            }
            try
            {
                try
                {
                    listDrawer.Draw(ReorderableListFlags.ShowIndices | ReorderableListFlags.HideAddButton | ReorderableListFlags.DisableContextMenu);
                } catch (Exception ex)
                {
                    Debug.LogException(ex);
                    //sceneHistory.Clear();
                }
                if (listDrawer.changed)
                {
                    sceneHistory.Save(PATH);
                    changed = false;
                }

                if (!nameFilter.IsEmpty())
                {
					EditorGUILayout.LabelField("Not in history", EditorStyles.miniBoldLabel);
                    string[] filters = nameFilter.SplitEx(' ');
                    var filteredScenes = new SceneHistory();
                    foreach (var s in allScenes)
                    {
                        string filename = Path.GetFileNameWithoutExtension(s.path);
                        bool match = true;
                        foreach (var f in filters)
                        {
                            if (filename.IndexOfIgnoreCase(f) < 0)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match && !sceneHistory.Contains(s.path))
                        {
                            filteredScenes.Add(s.reference);
                        }
                    }
                    listDrawer = CreateDrawer(filteredScenes);
                    listDrawer.Draw(ReorderableListFlags.HideAddButton | ReorderableListFlags.DisableContextMenu | ReorderableListFlags.DisableReordering | ReorderableListFlags.DisableDuplicateCommand);
                }
            } catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            EditorGUILayout.EndScrollView();
            OnFooterGUI();
        }

        void OnFooterGUI()
        {
            GUI.enabled = true;
#pragma warning disable 0162
            if (SHOW_SIZE)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (EditorGUIEx.IntField("Size", ref sceneHistory.maxSize))
                    {
                        if (sceneHistory.maxSize < 2)
                        {
                            sceneHistory.maxSize = 2;
                        }
                    }
                }
            }
#pragma warning restore 0162
        }
#if MENU_SHORTCUT
        [MenuItem("Tools/SceneHistorian/Scene1 %~F1")]
        public static void LoadScene1()
        {
            instance.LoadScene(1);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F2")]
        public static void LoadScene2()
        {
            instance.LoadScene(2);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F3")]
        public static void LoadScene3()
        {
            instance.LoadScene(3);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F4")]
        public static void LoadScene4()
        {
            instance.LoadScene(4);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F5")]
        public static void LoadScene5()
        {
            instance.LoadScene(5);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F6")]
        public static void LoadScene6()
        {
            instance.LoadScene(6);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F7")]
        public static void LoadScene7()
        {
            instance.LoadScene(7);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F8")]
        public static void LoadScene8()
        {
            instance.LoadScene(8);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F9")]
        public static void LoadScene9()
        {
            instance.LoadScene(9);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F10")]
        public static void LoadScene10()
        {
            instance.LoadScene(10);
        }
        [MenuItem("Tools/SceneHistorian/Scene1 %~F11")]
        public static void LoadScene11()
        {
            instance.LoadScene(11);
        }
#endif
    }
}

