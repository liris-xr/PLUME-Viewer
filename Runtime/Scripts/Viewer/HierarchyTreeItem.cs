using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PLUME.Viewer
{
    internal class HierarchyTreeItem
    {
        public readonly string GameObjectGuid;
        public readonly int ItemId;
        public bool Enabled { get; set; }
        public string GameObjectName { get; set; }
        public int ParentId { get; private set; }
        public bool IsParentDirty { get; private set; }
        public bool IsSiblingIndexDirty { get; private set; }

        private int _siblingIndex;

        public int SiblingIndex
        {
            get => _siblingIndex;
            set
            {
                var oldValue = _siblingIndex;
                _siblingIndex = value;
                if (oldValue != value)
                {
                    IsSiblingIndexDirty = true;
                }
            }
        }

        private string _parentGuid;

        public string ParentGuid
        {
            get => _parentGuid;
            set
            {
                var oldValue = _parentGuid;
                _parentGuid = value;

                if (string.IsNullOrEmpty(value) || value == "00000000000000000000000000000000")
                {
                    ParentId = -1;
                    return;
                }

                ParentId = value.GetHashCode();

                if (oldValue != value)
                {
                    IsParentDirty = true;
                }
            }
        }

        public VisualElement VisualElement;

        public HierarchyTreeItem(string gameObjectGuid)
        {
            GameObjectGuid = gameObjectGuid;
            ItemId = gameObjectGuid.GetHashCode();
            GameObjectName = "New Game Object";
            _parentGuid = "00000000000000000000000000000000"; // null guid by default (item is at root level)
            ParentId = -1;
            _siblingIndex = -1;
            Enabled = true;
            IsParentDirty = false;
            IsSiblingIndexDirty = false;
            VisualElement = null;
        }

        public void MarkClean()
        {
            IsParentDirty = false;
            IsSiblingIndexDirty = false;
        }
    }

    internal class HierarchyTreeItemComparer : IEqualityComparer<HierarchyTreeItem>
    {
        public static HierarchyTreeItemComparer Instance { get; } = new();

        public bool Equals(HierarchyTreeItem x, HierarchyTreeItem y)
        {
            return x.GameObjectGuid == y.GameObjectGuid;
        }

        public int GetHashCode(HierarchyTreeItem obj)
        {
            return obj.GameObjectGuid.GetHashCode();
        }
    }
}