using PLUME.Sample.Unity;

namespace PLUME.Viewer
{
    public interface IHierarchyUpdateEvent
    {
    }

    public class HierarchyForceRebuild : IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateGameObjectNameEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly string name;

        public HierarchyUpdateGameObjectNameEvent(GameObjectIdentifier gameObjectIdentifier, string name)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.name = name;
        }
    }

    public class HierarchyUpdateGameObjectSiblingIndexEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly int siblingIndex;

        public HierarchyUpdateGameObjectSiblingIndexEvent(GameObjectIdentifier gameObjectIdentifier, int siblingIndex)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.siblingIndex = siblingIndex;
        }
    }

    public class HierarchyUpdateGameObjectParentEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly GameObjectIdentifier parentIdentifier;
        public readonly int siblingIdx;

        public HierarchyUpdateGameObjectParentEvent(GameObjectIdentifier gameObjectIdentifier,
            GameObjectIdentifier parentIdentifier, int siblingIdx)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.parentIdentifier = parentIdentifier;
            this.siblingIdx = siblingIdx;
        }
    }

    public class HierarchyUpdateGameObjectEnabledEvent : IHierarchyUpdateEvent
    {
        public readonly bool enabled;
        public readonly GameObjectIdentifier gameObjectIdentifier;

        public HierarchyUpdateGameObjectEnabledEvent(GameObjectIdentifier gameObjectIdentifier, bool enabled)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.enabled = enabled;
        }
    }

    public class HierarchyCreateGameObjectEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;

        public HierarchyCreateGameObjectEvent(GameObjectIdentifier gameObjectIdentifier)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
        }
    }

    public class HierarchyDestroyGameObjectEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;

        public HierarchyDestroyGameObjectEvent(GameObjectIdentifier gameObjectIdentifier)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
        }
    }
}