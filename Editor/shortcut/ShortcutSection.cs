using System;
using System.IO;
using System.Text;
using System.Text.Ex;
using mulova.commons;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Ex;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace mulova.shortcut
{
    [Serializable]
    public class ShortcutSection : IComparable<ShortcutSection>
    {
        public const string SECT_EXT = ".sct";
        public const string ASSET_EXT = ".asc";
        public const string SCENE_EXT = ".ssc";

        public string id;
        public string name;
        public bool recordPrefab = true;
        public bool recordModified = false;
        public bool accessFirst = true;
        public bool applyCam = true;
        public int max = 50;
        public bool vertical = true;
        public bool activeStageOnly = true;
        [NonSerialized] private string dir;
        public ShortcutList assetRefs { get; private set; }
        public ShortcutList sceneRefs { get; private set; } // current scene's object refs
        [NonSerialized] private string cachedPath;
        [NonSerialized] public Vector2 scroll1 = new Vector2();
        [NonSerialized] public Vector2 scroll2 = new Vector2();

        public bool isShared
        {
            get
            {
                return !dir.StartsWith("Library/", StringComparison.Ordinal);
            }
            set
            {
                if (value == isShared)
                {
                    return;
                }
                var oldDir = dir;
                if (value)
                {
                    dir = dir.Substring("Library/".Length);
                }
                else
                {
                    dir = "Library/" + dir;
                }
                var files = AssetUtil.ListFiles(oldDir, id + "*");
                foreach (var f in files)
                {
                    string fileName = f.Name;
                    AssetUtil.Move(PathUtil.Combine(oldDir, fileName), PathUtil.Combine(dir, fileName));
                }
            }
        }

        public ShortcutItem this[string id] => assetRefs.Find(id) ?? sceneRefs.Find(id);

        public ShortcutSection() { }

        public ShortcutSection(string name, string dir)
        {
            this.id = Guid.NewGuid().ToString("N");
            this.name = name;
            this.dir = dir;
        }

        internal void Init(string dir)
        {
            this.dir = dir;
            assetRefs = new ShortcutList();
            sceneRefs = new ShortcutList(); // current scene's object refs
        }

        internal void RemoveMissing()
        {
            assetRefs.RemoveMissing();
            sceneRefs.RemoveMissing();
        }

        internal static string GetSceneGuid(Scene s)
        {
            return AssetDatabase.AssetPathToGUID(s.path);
        }

        internal static string GetPrefabGuid()
        {
            return AssetDatabase.AssetPathToGUID(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
        }

        internal ShortcutList GetCurrentSceneObjects()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                return GetSceneObjects(stage.prefabAssetPath);
            }
            else
            {
                return GetSceneObjects(SceneManager.GetActiveScene().path);
            }
        }

        internal ShortcutList GetSceneObjects(string assetPath)
        {
            if (cachedPath != assetPath)
            {
                cachedPath = assetPath;
                sceneRefs.Clear();
                sceneRefs = activeStageOnly? LoadSceneObjList(assetPath): LoadSceneObjList();
            }
            return sceneRefs;
        }

        internal ShortcutList LoadSceneObjList()
        {
            var d = new DirectoryInfo(dir);
            var files = d.GetFiles($"{id}*.{SCENE_EXT}");

            var list = new ShortcutList();
            foreach (var f in files)
            {
                var l = ShortcutList.Load(f.FullName);
                list.AddRange(l);
            }
            return list;
        }

        internal ShortcutList LoadSceneObjList(string assetPath)
        {
            var list = ShortcutList.Load(GetSceneStorePath(assetPath));
            return list;
        }

        internal void SelectAsset(int index)
        {
            if (index < assetRefs.Count)
            {
                var r = assetRefs[index];
                Selection.activeObject = r.reference;
                if (PrefabUtility.GetPrefabAssetType(r.reference) != PrefabAssetType.NotAPrefab)
                {
                    AssetDatabase.OpenAsset(r.reference);
                }
                else if (r.reference is SceneAsset)
                {
                    if (SceneManager.GetActiveScene().path != r.path)
                    {
                        EditorSceneManager.OpenScene(r.path);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Index out of range " + index);
            }
        }

        internal void ApplyCam(string id)
        {
            if (applyCam)
            {
                var r = this[id];
                if (r != null)
                {
                    r.ApplyCam();
                }
            }
        }

        internal void SelectSceneObject(int index)
        {
            if (index < sceneRefs.Count)
            {
                var r = sceneRefs[index];
                Selection.activeObject = r.reference;
            }
            else
            {
                Debug.LogWarning("Index out of range " + index);
            }
        }

        internal void ClearCache()
        {
            cachedPath = null;
            sceneRefs.Clear();
        }

        private void ForActiveStages(Action<string> action)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                action(stage.prefabAssetPath);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var s = SceneManager.GetSceneAt(i);
                    action(s.path);
                }
            }
        }

        internal void LoadRefs(string dir)
        {
            Init(dir);
            assetRefs = ShortcutList.Load(GetAssetStorePath());
            LoadStageRefs();
        }

        internal void LoadStageRefs()
        {
            sceneRefs = new ShortcutList();
            ForActiveStages(guid => sceneRefs.AddRange(LoadSceneObjList(guid)));
        }

        /// <summary>
        /// Save current scene references to file
        /// </summary>
        internal void Save()
        {
            SaveAssetStore();
            SaveSceneStore();
        }

        internal void SaveAssetStore()
        {
            if (name.IsEmpty())
            {
                return;
            }
            AssetUtil.WriteAllText(filePath, JsonUtility.ToJson(this), Encoding.UTF8);
            assetRefs.Save(GetAssetStorePath());
        }

        internal void SaveSceneStore()
        {
            if (name.IsEmpty())
            {
                return;
            }
            var stagePath = GetCurrentStagePath();
            var stageRefs = sceneRefs.GetStageRefs(stagePath);
            var storePath = GetSceneStorePath(stagePath);
            if (stageRefs.Count > 0)
            {
                stageRefs.Save(storePath);
            } else
            {
                if (File.Exists(storePath))
                {
                    File.Delete(storePath);
                }
            }
        }

        public static string GetCurrentStagePath()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.stageHandle.IsValid())
            {
                return stage.prefabAssetPath;
            }
            else
            {
                return SceneManager.GetActiveScene().path;
            }
        }

        internal void Sort()
        {
            assetRefs.Sort();
            sceneRefs.Sort();
        }


        internal void Delete()
        {
            File.Delete(filePath);
            string path = GetAssetStorePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileInfo[] files = AssetUtil.ListFiles(dir, string.Concat(id, "_*", SCENE_EXT));
            foreach (FileInfo f in files)
            {
                f.Delete();
            }
        }

        internal void AddPrefabObject(GameObject o)
        {
            AddInstance(o, PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
        }

        internal void AddSceneObject(Object o)
        {
            GameObject go = o.GetGameObject();
            if (go != null)
            {
                AddInstance(o, go.scene.path);
            }
        }

        private void AddInstance(Object o, string assetPath)
        {
            var list = GetSceneObjects(assetPath);

            var i = list.IndexOf(o);
            if (i >= 0)
            {
                if (accessFirst)
                {
                    list.Move(i, 0);
                }

                if (applyCam)
                {
                    list[i].SaveCam();
                }

            }
            else
            {
                i = 0;
                list.Insert(0, o);
            }
            if (accessFirst) // sort by star
            {
                list.Sort();
            }
            list[i].SaveCam();
        }

        internal void AddAsset(Object o)
        {
            var i = assetRefs.IndexOf(o);
            if (i >= 0)
            {
                if (accessFirst)
                {
                    assetRefs.Move(i, 0);
                }
            }
            else
            {
                i = 0;
                assetRefs.Insert(0, o);
            }
            if (accessFirst)
            {
                assetRefs.Sort();
            }
        }

        public void AddObject(Object o)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)))
            {
                AddSceneObject(o);
            }
            else
            {
                AddAsset(o);
            }
            Save();
        }

        internal string filePath
        {
            get
            {
                return PathUtil.Combine(dir, id + SECT_EXT);
            }
        }

        private string GetAssetStorePath()
        {
            return PathUtil.Combine(dir, id + ASSET_EXT);
        }

        private string GetSceneStorePath(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            return PathUtil.Combine(dir, string.Concat(id, "_", guid, SCENE_EXT));
        }

        public override string ToString()
        {
            return name;
        }

        public int CompareTo(ShortcutSection that)
        {
            int cmp = string.Compare(this.name, that.name, StringComparison.Ordinal);
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(this.id, that.id, StringComparison.Ordinal);
        }
    }
}
