using System;

namespace PLUME.Viewer
{
    internal struct HierarchyTreeItemData
    {
        public readonly Guid GameObjectGuid;
        public Guid ParentGameObjectGuid;
        public int SiblingIndex;
        public string Name;
        public bool Enabled;

        public HierarchyTreeItemData(Guid gameObjectGuid)
        {
            GameObjectGuid = gameObjectGuid;
            ParentGameObjectGuid = Guid.Empty;
            SiblingIndex = 0;
            Name = "New Game Object";
            Enabled = true;
        }

        public int GetId()
        {
            return GameObjectGuid.GetHashCode();
        }

        public int GetParentId()
        {
            if (ParentGameObjectGuid == Guid.Empty)
                return -1;
            return ParentGameObjectGuid.GetHashCode();
        }
    }
}