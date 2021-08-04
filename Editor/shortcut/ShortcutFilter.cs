using System;
using System.Collections.Generic;
using System.Text.Ex;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace mulova.shortcut
{
    public class ShortcutFilter
    {

        private string title;
        private bool asset;
        private bool[] show;
        private Predicate<ShortcutItem>[] predicates;
        private ToStr toString;

        /// <summary>
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="asset"><c>true</c> If target objects are asset types, false if scene objects</param>
        public ShortcutFilter(string title, bool asset, ToStr toString, params Predicate<ShortcutItem>[] predicates)
        {
            this.title = title;
            this.asset = asset;
            this.toString = toString != null ? toString : ObjToString.ScenePathToString;
            this.predicates = predicates;
            this.show = new bool[predicates.Length];
        }

        private static Dictionary<object, bool> bools = new Dictionary<object, bool>();
        private string filter;
        private FileType fileType = FileType.All;
        public AndPredicate<ShortcutItem> GetPredicate(List<ShortcutItem> list)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.cyan;
            AndPredicate<ShortcutItem> predicate = new AndPredicate<ShortcutItem>();
            EditorGUILayout.BeginHorizontal();
            bool selected = GUILayout.Button(title, EditorStyles.toolbarButton, GUILayout.MaxWidth(100));
            EditorGUILayoutEx.SearchField("", ref filter);
            if (!filter.IsEmpty())
            {
                predicate.AddPredicate(new ToStringFilter(toString, filter).Filter);
                //if (asset)
                //{
                //    predicate.AddPredicate(s=> new AssetFilter(filter).Filter(s.objRef?.reference));
                //} else
                //{
                //    predicate.AddPredicate(new ToStringFilter(toString, filter).Filter<ShortcutItem>);
                //}

            }
            if (asset)
            {
                EditorGUILayoutEx.PopupEnum(null, ref fileType);
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < predicates.Length; ++i)
            {
                Predicate<ShortcutItem> p = predicates[i];
                bool b = false;
                if (!bools.TryGetValue(p, out b))
                {
                    bools[p] = b;
                }
                show[i] = EditorGUILayout.Toggle(p.Method.Name, show[i]);
                if (bools[p])
                {
                    predicate.AddPredicate(p);
                }
            }
            predicate.AddPredicate(new FileTypeFilter(fileType).Filter);

            if (selected)
            {
                List<Object> filtered = new List<Object>();
                foreach (ShortcutItem o in list)
                {
                    if (predicate.Accept(o))
                    {
                        filtered.Add(o.reference);
                    }
                }
                Selection.objects = filtered.ToArray();
            }

            GUI.color = oldColor;

            return predicate;
        }
    }
}
