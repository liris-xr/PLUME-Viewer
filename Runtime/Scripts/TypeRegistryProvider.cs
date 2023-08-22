using Google.Protobuf.Reflection;
using UnityEngine;

namespace PLUME
{
    public abstract class TypeRegistryProvider : MonoBehaviour
    {
        public abstract TypeRegistry GetTypeRegistry();
    }
}