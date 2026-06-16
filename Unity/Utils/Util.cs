using System;
using System.Security.Cryptography;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using Component = UnityEngine.Component;
#if UNITY_EDITOR
using DebugManager = Hakjang.DebugManager;
#endif

namespace Utils
{
    public static class Util
    {
        #region GetComponent
        // GetComponent
        public static T GetComponent<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponent<T>(mono);
#endif
            return mono.GetComponent<T>();
        }

        // GameObject overloads
        public static T GetComponent<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponent<T>(go);
#endif
            return go.GetComponent<T>();
        }

        public static T[] GetComponents<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponents<T>(mono);
#endif
            return mono.GetComponents<T>();
        }

        public static T[] GetComponents<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponents<T>(go);
#endif
            return go.GetComponents<T>();
        }

        public static Component GetComponent(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponent(mono, type);
#endif
            return mono.GetComponent(type);
        }

        public static Component GetComponent(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponent(go, type);
#endif
            return go.GetComponent(type);
        }

        public static Component[] GetComponents(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponents(mono, type);
#endif
            return mono.GetComponents(type);
        }

        public static Component[] GetComponents(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponents(go, type);
#endif
            return go.GetComponents(type);
        }

        // InChildren
        public static T GetComponentInChildren<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren<T>(mono);
#endif
            return mono.GetComponentInChildren<T>();
        }

        public static T GetComponentInChildren<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren<T>(go);
#endif
            return go.GetComponentInChildren<T>();
        }

        public static T GetComponentInChildren<T>(MonoBehaviour mono, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren<T>(mono, include_inactive);
#endif
            return mono.GetComponentInChildren<T>(include_inactive);
        }

        public static T GetComponentInChildren<T>(GameObject go, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren<T>(go, include_inactive);
#endif
            return go.GetComponentInChildren<T>(include_inactive);
        }

        public static Component GetComponentInChildren(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren(mono, type);
#endif
            return mono.GetComponentInChildren(type);
        }

        public static Component GetComponentInChildren(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren(go, type);
#endif
            return go.GetComponentInChildren(type);
        }

        public static Component GetComponentInChildren(MonoBehaviour mono, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren(mono, type, include_inactive);
#endif
            return mono.GetComponentInChildren(type, include_inactive);
        }

        public static Component GetComponentInChildren(GameObject go, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInChildren(go, type, include_inactive);
#endif
            return go.GetComponentInChildren(type, include_inactive);
        }

        public static T[] GetComponentsInChildren<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren<T>(mono);
#endif
            return mono.GetComponentsInChildren<T>();
        }

        public static T[] GetComponentsInChildren<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren<T>(go);
#endif
            return go.GetComponentsInChildren<T>();
        }

        public static T[] GetComponentsInChildren<T>(MonoBehaviour mono, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren<T>(mono, include_inactive);
#endif
            return mono.GetComponentsInChildren<T>(include_inactive);
        }

        public static T[] GetComponentsInChildren<T>(GameObject go, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren<T>(go, include_inactive);
#endif
            return go.GetComponentsInChildren<T>(include_inactive);
        }

        public static Component[] GetComponentsInChildren(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren(mono, type);
#endif
            return mono.GetComponentsInChildren(type);
        }

        public static Component[] GetComponentsInChildren(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren(go, type);
#endif
            return go.GetComponentsInChildren(type);
        }

        public static Component[] GetComponentsInChildren(MonoBehaviour mono, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren(mono, type, include_inactive);
#endif
            return mono.GetComponentsInChildren(type, include_inactive);
        }

        public static Component[] GetComponentsInChildren(GameObject go, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInChildren(go, type, include_inactive);
#endif
            return go.GetComponentsInChildren(type, include_inactive);
        }

        // InParent
        public static T GetComponentInParent<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInParent<T>(mono);
#endif
            return mono.GetComponentInParent<T>();
        }

        public static T GetComponentInParent<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInParent<T>(go);
#endif
            return go.GetComponentInParent<T>();
        }

        public static Component GetComponentInParent(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInParent(mono, type);
#endif
            return mono.GetComponentInParent(type);
        }

        public static Component GetComponentInParent(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentInParent(go, type);
#endif
            return go.GetComponentInParent(type);
        }

        public static T[] GetComponentsInParent<T>(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent<T>(mono);
#endif
            return mono.GetComponentsInParent<T>();
        }

        public static T[] GetComponentsInParent<T>(GameObject go)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent<T>(go);
#endif
            return go.GetComponentsInParent<T>();
        }

        public static T[] GetComponentsInParent<T>(MonoBehaviour mono, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent<T>(mono, include_inactive);
#endif
            return mono.GetComponentsInParent<T>(include_inactive);
        }

        public static T[] GetComponentsInParent<T>(GameObject go, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent<T>(go, include_inactive);
#endif
            return go.GetComponentsInParent<T>(include_inactive);
        }

        public static Component[] GetComponentsInParent(MonoBehaviour mono, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent(mono, type);
#endif
            return mono.GetComponentsInParent(type);
        }

        public static Component[] GetComponentsInParent(GameObject go, Type type)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent(go, type);
#endif
            return go.GetComponentsInParent(type);
        }

        public static Component[] GetComponentsInParent(MonoBehaviour mono, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent(mono, type, include_inactive);
#endif
            return mono.GetComponentsInParent(type, include_inactive);
        }

        public static Component[] GetComponentsInParent(GameObject go, Type type, bool include_inactive)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.GetComponentsInParent(go, type, include_inactive);
#endif
            return go.GetComponentsInParent(type, include_inactive);
        }

        // TryGetComponent
        public static bool TryGetComponent<T>(MonoBehaviour mono, out T component)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.TryGetComponent(mono, out component);
#endif
            return mono.TryGetComponent(out component);
        }

        public static bool TryGetComponent<T>(GameObject go, out T component)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.TryGetComponent(go, out component);
#endif
            return go.TryGetComponent(out component);
        }

        public static bool TryGetComponent(MonoBehaviour mono, Type type, out Component component)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.TryGetComponent(mono, type, out component);
#endif
            return mono.TryGetComponent(type, out component);
        }

        public static bool TryGetComponent(GameObject go, Type type, out Component component)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return DebugManager.TryGetComponent(go, type, out component);
#endif
            return go.TryGetComponent(type, out component);
        }

        public static int GetFixedHash(this string str)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
                return BitConverter.ToInt32(bytes, 0);
            }
        }
        #endregion
    }
}