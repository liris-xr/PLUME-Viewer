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
                var itemData = HierarchyTree.GetItemDataForIndex<HierarchyTreeItem>(i);
                itemData.VisualElement = element;
                TryUpdateItemVisualElement(itemData);
                // Temporary fix for tree view selection not working reliably
                element.RegisterCallback<MouseDownEvent>(evt => OnMouseDownEvent(evt, i));
                Profiler.EndSample();
            };
            HierarchyTree.SetRootItems(new List<TreeViewItemData<HierarchyTreeItem>>());
            HierarchyTree.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            HierarchyTree.ClearSelection();
        }

        internal static bool TryUpdateItemVisualElement(HierarchyTreeItem item)
        {
            if (item.VisualElement == null)
                return false;

            try
            {
                var label = item.VisualElement.Q<Label>("name");
                label.text = item.GameObjectName;
                label.style.color = item.Enabled ? new StyleColor(Color.white) : new StyleColor(Color.gray);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                // ignored, item not found
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

            var selectedItems = HierarchyTree.GetSelectedItems<GameObject>();

            GUIUtility.systemCopyBuffer = string.Join(",", selectedItems.Select(t =>
                player.GetPlayerContext().GetRecordIdentifier(t.data.GetInstanceID())));
        }
    }
}