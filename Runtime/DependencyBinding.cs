using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Antoine.Systems {
    [CreateAssetMenu(menuName = "Dependency Binding")]
    public class DependencyBinding : ScriptableObject {
        [TypeFilter(typeof(IBindable))]
        [SerializeField] SerializableType type;
        
        IBindable dependency;
        Dictionary<Type, List<Action<IBindable>>> asyncBindings = new();
        
        public void Provide<T>(T bindable) where T : IBindable {
            if (dependency != null) {
                Debug.LogWarning($"[DependencyBinding.Provide]: Trying to provide an already satisfied binding of type {type}.");
                return;
            }
            
            var bindableType = bindable.GetType();
            if(bindableType != type.Type && !bindableType.IsSubclassOf(type.Type)) {
                Debug.LogWarning($"[DependencyBinding.Provide]: Trying to provide a binding of type {type} with incompatible type {bindableType}.");
                return;
            }
            
            if (asyncBindings.TryGetValue(bindableType, out var callbacks)) {
                foreach (var callback in callbacks) {
                    callback.Invoke(bindable);
                }
                asyncBindings.Remove(bindableType);
            }
            
            dependency = bindable;
        }

        public void BindAsync(Action<IBindable> callback) {
            if (callback == null) {
                Debug.LogWarning($"[DependencyBinding.BindAsync]: Trying to bind with a null callback for type {type}.");
                return;
            }

            // Already satisfied
            if (dependency != null) {
                callback.Invoke(dependency);
                return;
            }

            if (!asyncBindings.ContainsKey(type.Type)) {
                asyncBindings.Add(type.Type, new List<Action<IBindable>>());
            }
            
            asyncBindings[type.Type].Add(callback);
        } 
        
#if UNITY_EDITOR
        void OnPlayModeStateChanged(PlayModeStateChange state) {
            if(state == PlayModeStateChange.ExitingPlayMode) {
                dependency = default;
                asyncBindings.Clear();
            }
        }
        
        void OnEnable() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
#endif
    }
}
