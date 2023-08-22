using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Runtime;

namespace PLUME
{
    public static class ObjectExtensions
    {
        private static readonly ObjectHasher ObjectHasher = new();

        private static readonly MethodInfo FindObjectFromInstanceIDMethod =
            typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly Dictionary<int, Object> CachedObjectFromInstanceId = new();

        /**
         * Return a hash used to discriminate assets from one another in the record.
         */
        public static int GetRecorderHash(this Object obj)
        {
            return ObjectHasher.Hash(obj);
        }

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

            obj = (Object) FindObjectFromInstanceIDMethod.Invoke(null, new object[] {instanceId});
            CachedObjectFromInstanceId.Add(instanceId, obj);
            
            //TODO : Manage when obj is null (give safe handle)
            
            return obj;
        }
        
        public static List<GameObject> GetObjectsInLayer(LayerMask layerMask, bool includeInactive = false)
        {
            var ret = new List<GameObject>();
            foreach (var t in Object.FindObjectsOfType<Transform>(includeInactive))
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