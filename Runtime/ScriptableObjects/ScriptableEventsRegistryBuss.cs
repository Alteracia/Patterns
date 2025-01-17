﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alteracia.Patterns.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ScriptableEventsRegistryBuss", menuName = "AltEvents/ScriptableEventsRegistryBuss", order = 0)]
    public class ScriptableEventsRegistryBuss : RootScriptableObject
    {
        [SerializeField] private List<ScriptableEventsRegistry> registries = new List<ScriptableEventsRegistry>();
        
        // Called after runtime starts
        public void OnEnable()
        {
            _instance = this;
            BindEventsRegistryAndBuss();
        }
        
        // Subscribe all on Start
        private void BindEventsRegistryAndBuss()
        {
            foreach (var soEvent in registries.SelectMany(registry => registry.Nested.OfType<ISubscribableEvent>()))
            {
                foreach (var cur in this.Nested.OfType<ISubscribableEvent>())
                {
                    if (!cur.Equals(soEvent)) continue;

                    cur.SubscribeTo(soEvent);
                    soEvent.SubscribeTo(cur);
                    break;
                }
            }
        }
        
        public void UpdateEvents(ScriptableEventsRegistry registry)
        {
            foreach (var soEvent in registry.Nested.OfType<ISubscribableEvent>())
            {
                foreach (var cur in this.Nested.OfType<ISubscribableEvent>())
                {
                    if (!cur.Equals(soEvent)) continue;
                    soEvent.CopyFrom(cur);
                    
                    cur.SubscribeTo(soEvent);
                    soEvent.SubscribeTo(cur);
                    break;
                }
            }
        }

        public static readonly List<ScriptableEventsRegistry> Registries = new List<ScriptableEventsRegistry>();
        private static ScriptableEventsRegistryBuss _instance;
        
        public static ScriptableEventsRegistryBuss Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("Scriptable Events Registry Buss asset not initialized or doesn't exists in project");
                }

                return _instance;
            }
        }
        
#if UNITY_EDITOR
        public void AddRegistry(ScriptableEventsRegistry registry)
        {
            if (!registries.Contains(registry))
                registries.Add(registry);

            foreach (var soEvent in registry.Nested.OfType<ISubscribableEvent>())
            {
                bool equal = false;
                foreach (var cur in this.Nested.OfType<ISubscribableEvent>())
                {
                    if (cur.Equals(soEvent))
                    {
                        equal = true;
                        break;
                    }
                    
                }

                if (equal) continue;
                AddNested((NestedScriptableObject)soEvent);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }

        // Called before quit, recompilation and after press play button in editor
        public void OnDisable()
        {
            SaveThis();
        }

        // Awake called after application starts - for editor start editor!
        void Awake()
        {
            if (_instance == null) _instance = this;
            foreach (var registry in Registries.Where(registry => registry))
            {
                this.AddRegistry(registry);
            }
        }

        [ContextMenu("Save")]
        private void SaveThis()
        {
            if (registries != null) registries.RemoveAll(r => r == null);
            
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }
        
        [ContextMenu("Clear")]
        private void ClearEvents()
        {
            this.ClearNested();
            this.ClearNested(n => !(n is ISubscribableEvent));
            
            if (registries != null) registries.RemoveAll(r => r == null);
            
            var list = this.Nested.OfType<ISubscribableEvent>().ToList();
            
            for (int i = list.Count; i-- > 0;)
            {
                var tmp = list[i];
                bool duplicate = false;
                for (int j = 0; j < i; j++)
                {
                    if (!list[i].Equals(list[j])) continue;
                    duplicate = true;
                    break;
                }
                if (!duplicate)
                {
                    bool found = registries.SelectMany(registry =>
                        registry.Nested.OfType<ISubscribableEvent>()).Any(soEvent => tmp.Equals(soEvent));
                    if (found) continue;
                }
                this.Nested.Remove((NestedScriptableObject) tmp);
                Undo.DestroyObjectImmediate((NestedScriptableObject) tmp);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(this);
        }
#endif
    }

}