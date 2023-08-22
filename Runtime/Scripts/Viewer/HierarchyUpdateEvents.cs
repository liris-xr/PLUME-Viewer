namespace PLUME
{
    public interface IHierarchyUpdateEvent
    {
    }

    public class HierarchyUpdateSiblingIndexEvent : IHierarchyUpdateEvent
    {
        public readonly int transformId;
        public readonly int prevSiblingIndex;
        public readonly int newSiblingIndex;

        public HierarchyUpdateSiblingIndexEvent(int transformId, int prevSiblingIndex, int newSiblingIndex)
        {
            this.transformId = transformId;
            this.prevSiblingIndex = prevSiblingIndex;
            this.newSiblingIndex = newSiblingIndex;
        }
    }

    public class HierarchyUpdateParentEvent : IHierarchyUpdateEvent
    {
        public readonly int transformId;
        public readonly int siblingIdx;
        public readonly int prevParentTransformId;
        public readonly int newParentTransformId;

        public HierarchyUpdateParentEvent(int transformId, int siblingIdx, int prevParentTransformId, int newParentTransformId)
        {
            this.transformId = transformId;
            this.siblingIdx = siblingIdx;
            this.prevParentTransformId = prevParentTransformId;
            this.newParentTransformId = newParentTransformId;
        }
    }
    
    public class HierarchyUpdateEnabledEvent : IHierarchyUpdateEvent
    {
        public readonly int transformId;
        public readonly bool enabled;

        public HierarchyUpdateEnabledEvent(int transformId, bool enabled)
        {
            this.transformId = transformId;
            this.enabled = enabled;
        }
    }

    public class HierarchyUpdateCreateTransformEvent : IHierarchyUpdateEvent
    {
        public readonly int transformId;

        public HierarchyUpdateCreateTransformEvent(int transformId)
        {
            this.transformId = transformId;
        }
    }

    public class HierarchyUpdateDestroyTransformEvent : IHierarchyUpdateEvent
    {
        public readonly int transformId;

        public HierarchyUpdateDestroyTransformEvent(int transformId)
        {
            this.transformId = transformId;
        }
    }
}