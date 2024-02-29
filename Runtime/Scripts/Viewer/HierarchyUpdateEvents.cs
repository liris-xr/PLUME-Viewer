namespace PLUME
{
    public interface IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateResetEvent : IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateSiblingIndexEvent : IHierarchyUpdateEvent
    {
        public readonly string transformIdentifier;
        public readonly int siblingIndex;

        public HierarchyUpdateSiblingIndexEvent(string transformIdentifier, int siblingIndex)
        {
            this.transformIdentifier = transformIdentifier;
            this.siblingIndex = siblingIndex;
        }
    }

    public class HierarchyUpdateParentEvent : IHierarchyUpdateEvent
    {
        public readonly string transformIdentifier;
        public readonly string parentTransformIdentifier;
        public readonly int siblingIdx;

        public HierarchyUpdateParentEvent(string transformIdentifier, string parentTransformIdentifier, int siblingIdx)
        {
            this.transformIdentifier = transformIdentifier;
            this.parentTransformIdentifier = parentTransformIdentifier;
            this.siblingIdx = siblingIdx;
        }
    }

    public class HierarchyUpdateEnabledEvent : IHierarchyUpdateEvent
    {
        public readonly string transformIdentifier;
        public readonly bool enabled;

        public HierarchyUpdateEnabledEvent(string transformIdentifier, bool enabled)
        {
            this.transformIdentifier = transformIdentifier;
            this.enabled = enabled;
        }
    }

    public class HierarchyUpdateCreateTransformEvent : IHierarchyUpdateEvent
    {
        public readonly string transformIdentifier;

        public HierarchyUpdateCreateTransformEvent(string transformIdentifier)
        {
            this.transformIdentifier = transformIdentifier;
        }
    }

    public class HierarchyUpdateDestroyTransformEvent : IHierarchyUpdateEvent
    {
        public readonly string transformIdentifier;

        public HierarchyUpdateDestroyTransformEvent(string transformIdentifier)
        {
            this.transformIdentifier = transformIdentifier;
        }
    }
}