using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCP.Editor
{
    internal static class MCPUnityCompat
    {
        public static T[] FindObjects<T>(bool includeInactive = true)
            where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(obj => IsValidObject(obj, includeInactive))
                .ToArray();
#endif
        }

        public static UnityEngine.Object[] FindObjects(Type type, bool includeInactive = true)
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType(
                type,
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
            return Resources.FindObjectsOfTypeAll(type)
                .Cast<UnityEngine.Object>()
                .Where(obj => IsValidObject(obj, includeInactive))
                .ToArray();
#endif
        }

        public static T FindFirstObject<T>(bool includeInactive)
            where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            return FindObjects<T>(includeInactive).FirstOrDefault();
#endif
        }

#if !UNITY_2022_2_OR_NEWER
        private static bool IsValidObject(UnityEngine.Object obj, bool includeInactive)
        {
            if (obj == null)
                return false;

            if (obj is GameObject go)
                return IsValidGameObject(go, includeInactive);

            if (obj is Component component)
                return IsValidGameObject(component.gameObject, includeInactive);

            return true;
        }

        private static bool IsValidGameObject(GameObject go, bool includeInactive)
        {
            if (go == null)
                return false;

            if (EditorUtility.IsPersistent(go))
                return false;

            Scene scene = go.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            if (!includeInactive && !go.activeInHierarchy)
                return false;

            return (go.hideFlags & HideFlags.HideAndDontSave) == 0;
        }
#endif
    }
}
