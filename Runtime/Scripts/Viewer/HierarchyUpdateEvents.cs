using PLUME.Sample.Unity;

namespace PLUME.Viewer
{
    public interface IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateResetEvent : IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateSiblingIndexEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly int siblingIndex;

        public HierarchyUpdateSiblingIndexEvent(GameObjectIdentifier gameObjectIdentifier, int siblingIndex)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.siblingIndex = siblingIndex;
        }
    }

    public class HierarchyUpdateParentEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly GameObjectIdentifier parentIdentifier;
        public readonly int siblingIdx;

        public HierarchyUpdateParentEvent(GameObjectIdentifier gameObjectIdentifier,
            GameObjectIdentifier parentIdentifier, int siblingIdx)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.parentIdentifier = parentIdentifier;
            this.siblingIdx = siblingIdx;
        }
    }

    public class HierarchyUpdateEnabledEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;
        public readonly bool enabled;

        public HierarchyUpdateEnabledEvent(GameObjectIdentifier gameObjectIdentifier, bool enabled)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
            this.enabled = enabled;
        }
    }

    public class HierarchyUpdateCreateTransformEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;

        public HierarchyUpdateCreateTransformEvent(GameObjectIdentifier gameObjectIdentifier)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
        }
    }

    public class HierarchyUpdateDestroyTransformEvent : IHierarchyUpdateEvent
    {
        public readonly GameObjectIdentifier gameObjectIdentifier;

        public HierarchyUpdateDestroyTransformEvent(GameObjectIdentifier gameObjectIdentifier)
        {
            this.gameObjectIdentifier = gameObjectIdentifier;
        }
    }
}