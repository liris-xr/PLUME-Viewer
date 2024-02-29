using System;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using UnityEngine;

namespace PLUME
{
    public class TypeRegistryProviderAssembliesLookup : TypeRegistryProvider
    {
        [Tooltip("Assemblies where the module will look for MessageDescriptors, includes Assembly-CSharp by default.")]
        public string[] assembliesNames =
            { "Assembly-CSharp", typeof(TypeRegistryProviderAssembliesLookup).Assembly.GetName().Name };

        private TypeRegistry _registry;

        public override TypeRegistry GetTypeRegistry()
        {
            if (_registry != null) return _registry;

            var messageDescriptors = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => assembliesNames.Contains(a.GetName().Name))
                .SelectMany(assembly =>
                    assembly.GetTypes()
                        .Where(t => typeof(IMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .Select(t =>
                        {
                            var descriptorProperty =
                                t.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                            var value = descriptorProperty!.GetValue(null);
                            return (MessageDescriptor)value;
                        }));

            _registry = TypeRegistry.FromMessages(messageDescriptors);

            return _registry;
        }
    }
}