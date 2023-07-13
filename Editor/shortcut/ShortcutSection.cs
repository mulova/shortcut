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
        [NonSerialized] private string dir;
        public ShortcutList assetRefs { get; private set; }
        private ShortcutList _stageRefs; // current scene's object refs
        [NonSerialized] private string cachedPath;
        [NonSerialized] public Vector2 scroll1 = new Vector2();
        [NonSerialized] public Vector2 scroll2 = new Vector2();

        public ShortcutList stageRefs
        {
            get
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
#if UNITY_2020_1_OR_NEWER
                var stagePath = stage != null ? stage.assetPath : SceneManager.GetActiveScene().path;
#else
                var stagePath = stage != null ? stage.prefabAssetPath : SceneManager.GetActiveScene().path;
#endif
                if (stagePath != cachedPath)
                {
                    cachedPath = stagePath;
                    _stageRefs = new ShortcutList();
                    ForEachStage(guid => _stageRefs.AddRange(LoadStageObjList(guid)));
                }
                return _stageRefs;
            }
        }

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

        public ShortcutItem this[string id] => assetRefs.Find(id) ?? _stageRefs.Find(id);

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
            _stageRefs = new ShortcutList(); // current scene's object refs
        }

        internal void RemoveMissing()
        {
            assetRefs.RemoveMissing();
            _stageRefs.RemoveMissing();
        }

        internal static string GetSceneGuid(Scene s)
        {
            return AssetDatabase.AssetPathToGUID(s.path);
        }

        internal static string GetPrefabGuid()
        {
#if UNITY_2020_1_OR_NEWER
            return AssetDatabase.AssetPathToGUID(PrefabStageUtility.GetCurrentPrefabStage().assetPath);
#else
            return AssetDatabase.AssetPathToGUID(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
#endif
        }

        internal ShortcutList LoadStageObjList(string assetPath)
        {
            var list = ShortcutList.Load(GetStageStorePath(assetPath));
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
            if (index < _stageRefs.Count)
            {
                var r = _stageRefs[index];
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
            _stageRefs.Clear();
        }

        internal void LoadRefs(string dir)
        {
            Init(dir);
            assetRefs = ShortcutList.Load(GetAssetStorePath());
        }

        private void ForEachStage(Action<string> action)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
#if UNITY_2020_1_OR_NEWER
                action(stage.assetPath);
#else
                action(stage.prefabAssetPath);
#endif
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

        /// <summary>
        /// Save current scene references to file
        /// </summary>
        internal void Save()
        {
            SaveAssetStore();
            SaveStageStore();
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

        internal void SaveStageStore()
        {
            if (name.IsEmpty())
            {
                return;
            }
            var categorized = stageRefs.Categorize();
            ForEachStage(path =>
            {
                var storePath = GetStageStorePath(path);
                if (categorized.TryGetValue(path, out var s))
                {
                    s.Save(storePath);
                } else
                {
                    if (File.Exists(storePath))
                    {
                        File.Delete(storePath);
                    }
                }
            });
        }

        public static string GetCurrentStagePath()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.stageHandle.IsValid())
            {
#if UNITY_2020_1_OR_NEWER
                return stage.assetPath;
#else
                return stage.prefabAssetPath;
#endif
            }
            else
            {
                return null;
            }
        }

        internal void Sort()
        {
            assetRefs.Sort();
            _stageRefs.Sort();
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
#if UNITY_2020_1_OR_NEWER
            AddInstance(o, PrefabStageUtility.GetCurrentPrefabStage().assetPath);
#else
            AddInstance(o, PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
#endif
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
            var list = stageRefs;

            var i = list.IndexOf(o);
            if (i >= 0)
            {
                if (accessFirst)
                {
                    list.Move(i, 0);
                }

                if (applyCam)
                {
                    list[i].CollectCam();
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
            list[i].CollectCam();
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

        private string GetStageStorePath(string assetPath)
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
