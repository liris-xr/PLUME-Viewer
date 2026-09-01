using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace PLUME.Viewer
{
    // TODO: decouple from the UI document
    public class HierarchyTreeUI : MonoBehaviour
    {
        public Player.Player player;
        public UIDocument document;

        public TreeView HierarchyTree { get; private set; }

        private bool _loggedMissingNameLabel;
        
        private readonly Dictionary<int, VisualElement> _itemIdToVisualElement = new();

        private void Awake()
        {
            var root = document.rootVisualElement;
            HierarchyTree = root.Q("viewer").Q<TreeView>("hierarchy-tree");

            HierarchyTree.makeItem = () =>
            {
                Profiler.BeginSample("MakeItem");
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.Add(new Label { name = "name" });
                Profiler.EndSample();
                return container;
            };
            HierarchyTree.bindItem = (element, i) =>
            {
                Profiler.BeginSample("BindItem");
                var itemData = HierarchyTree.GetItemDataForIndex<HierarchyTreeItemData>(i);
                _itemIdToVisualElement[itemData.GetId()] = element;
                TryUpdateItemVisualElement(itemData);
                // Temporary fix for tree view selection not working reliably
                element.RegisterCallback<MouseDownEvent>(evt => OnMouseDownEvent(evt, i));
                Profiler.EndSample();
            };
            HierarchyTree.unbindItem = (element, i) =>
            {
                Profiler.BeginSample("UnbindItem");
                var itemData = HierarchyTree.GetItemDataForIndex<HierarchyTreeItemData>(i);
                _itemIdToVisualElement.Remove(itemData.GetId());
                Profiler.EndSample();
            };
            HierarchyTree.SetRootItems(new List<TreeViewItemData<HierarchyTreeItemData>>());
            HierarchyTree.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            HierarchyTree.ClearSelection();
        }

        internal bool TryUpdateItemVisualElement(HierarchyTreeItemData itemData)
        {
            var visualElement = _itemIdToVisualElement.GetValueOrDefault(itemData.GetId());
            
            if (visualElement == null)
                return false;

            var label = visualElement.Q<Label>("name");

            if (label == null)
            {
                // Logged once: this runs per item per refresh, and the template either has the label or it never will.
                if (!_loggedMissingNameLabel)
                {
                    _loggedMissingNameLabel = true;
                    Debug.LogWarning($"Hierarchy item '{itemData.Name}' has no 'name' label; the tree item template no " +
                                     "longer matches what this code expects. Hierarchy names will not be shown.");
                }

                return false;
            }

            try
            {
                label.text = itemData.Name;
                label.style.color = itemData.Enabled ? new StyleColor(Color.white) : new StyleColor(Color.gray);
                label.MarkDirtyRepaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to bind hierarchy item '{itemData.Name}' " +
                               $"(guid={itemData.GameObjectGuid}, id={itemData.GetId()}).\n{e}");
                return false;
            }

            return true;
        }
        
        private void OnMouseDownEvent(MouseDownEvent evt, int index)
        {
            if (evt.ctrlKey)
            {
                // Add to existing selection
                HierarchyTree.AddToSelection(index);
            }
            else
            {
                HierarchyTree.SetSelection(index);
            }
        }

        private void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (!evt.ctrlKey || evt.keyCode != KeyCode.C) return;

            var selectedItems = HierarchyTree.GetSelectedItems<HierarchyTreeItemData>();

            GUIUtility.systemCopyBuffer = string.Join(",", selectedItems.Select(t => t.data.GameObjectGuid.ToString("N")));
        }
    }
}