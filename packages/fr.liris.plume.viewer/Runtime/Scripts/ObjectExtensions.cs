using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PLUME
{
    public static class ObjectExtensions
    {
        private static readonly MethodInfo FindObjectFromInstanceIDMethod =
            typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly Dictionary<int, Object> CachedObjectFromInstanceId = new();

        private static bool _loggedMissingFindObjectFromInstanceID;

        // TODO: this can be moved inside PlayerContext and optimized using a cache updated when a new identifier correspondence is registered
        public static Object FindObjectFromInstanceID(int instanceId)
        {
            var found = CachedObjectFromInstanceId.TryGetValue(instanceId, out var obj);

            if (found)
            {
                if (obj == null)
                {
                    CachedObjectFromInstanceId.Remove(instanceId);
                }
                else
                {
                    return obj;
                }
            }

            // The method is internal, so it can disappear on a Unity upgrade.
            if (FindObjectFromInstanceIDMethod == null)
            {
                if (!_loggedMissingFindObjectFromInstanceID)
                {
                    _loggedMissingFindObjectFromInstanceID = true;
                    Debug.LogError(
                        "UnityEngine.Object.FindObjectFromInstanceID is not available in Unity " +
                        Application.unityVersion +
                        ". Objects that are not tracked by the player context cannot be resolved.");
                }

                return null;
            }

            obj = (Object)FindObjectFromInstanceIDMethod.Invoke(null, new object[] { instanceId });
            CachedObjectFromInstanceId.Add(instanceId, obj);

            //TODO : Manage when obj is null (give safe handle)

            return obj;
        }

        public static List<GameObject> GetObjectsInLayer(LayerMask layerMask, bool includeInactive = false)
        {
            var ret = new List<GameObject>();
            foreach (var t in Object.FindObjectsByType<Transform>(
                         includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                var isIncludedInLayerMask = layerMask.value == (layerMask.value | (1 << t.gameObject.layer));

                if (isIncludedInLayerMask)
                {
                    ret.Add(t.gameObject);
                }
            }

            return ret;
        }
    }
}