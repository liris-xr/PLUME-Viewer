using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer
{
    public class HierarchyTreePresenter : MonoBehaviour
    {
        public Player.Player player;
        public double refreshInterval = 1; // seconds

        private HierarchyTreeUI _hierarchyTreeUI;
        private TreeView _hierarchyTree;

        private readonly Dictionary<string, HierarchyTreeItem> _items = new();
        private readonly HashSet<HierarchyTreeItem> _createdItems = new(HierarchyTreeItemComparer.Instance);
        private readonly HashSet<HierarchyTreeItem> _deletedItems = new(HierarchyTreeItemComparer.Instance);
        private readonly HashSet<HierarchyTreeItem> _updatedItems = new(HierarchyTreeItemComparer.Instance);

        private double _lastUpdate;
        private bool _forceRebuild;

        private void Awake()
        {
            _hierarchyTreeUI = GetComponent<HierarchyTreeUI>();
        }

        private void Start()
        {
            _hierarchyTree = _hierarchyTreeUI.HierarchyTree;
            player.GetPlayerContext().updatedHierarchy += OnHierarchyUpdateEvent;
        }

        public void OnHierarchyUpdateEvent(IHierarchyUpdateEvent evt)
        {
            switch (evt)
            {
                case HierarchyCreateGameObjectEvent createEvt:
                {
                    var gameObjectGuid = createEvt.gameObjectIdentifier.GameObjectId;
                    var newHierarchyItem = new HierarchyTreeItem(gameObjectGuid);

                    if (_items.TryAdd(gameObjectGuid, newHierarchyItem))
                    {
                        _createdItems.Add(newHierarchyItem);
                        _deletedItems.Remove(newHierarchyItem);
                    }

                    break;
                }
                case HierarchyDestroyGameObjectEvent destroyEvt:
                {
                    var gameObjectGuid = destroyEvt.gameObjectIdentifier.GameObjectId;

                    if (_items.TryGetValue(gameObjectGuid, out var hierarchyTreeItem))
                    {
                        _deletedItems.Add(hierarchyTreeItem);
                        _updatedItems.Remove(hierarchyTreeItem);
                        _createdItems.Remove(hierarchyTreeItem);
                        _items.Remove(gameObjectGuid);
                    }

                    break;
                }
                case HierarchyUpdateGameObjectNameEvent nameUpdateEvt:
                {
                    var gameObjectGuid = nameUpdateEvt.gameObjectIdentifier.GameObjectId;

                    if (_items.TryGetValue(gameObjectGuid, out var hierarchyTreeItem))
                    {
                        hierarchyTreeItem.GameObjectName = nameUpdateEvt.name;
                        _updatedItems.Add(hierarchyTreeItem);
                    }

                    break;
                }
                case HierarchyUpdateGameObjectSiblingIndexEvent siblingUpdateEvt:
                {
                    var gameObjectGuid = siblingUpdateEvt.gameObjectIdentifier.GameObjectId;

                    if (_items.TryGetValue(gameObjectGuid, out var hierarchyTreeItem))
                    {
                        hierarchyTreeItem.SiblingIndex = siblingUpdateEvt.siblingIndex;
                        _updatedItems.Add(hierarchyTreeItem);
                    }

                    break;
                }
                case HierarchyUpdateGameObjectEnabledEvent enabledUpdateEvt:
                {
                    var gameObjectGuid = enabledUpdateEvt.gameObjectIdentifier.GameObjectId;

                    if (_items.TryGetValue(gameObjectGuid, out var hierarchyTreeItem))
                    {
                        hierarchyTreeItem.Enabled = enabledUpdateEvt.enabled;
                        _updatedItems.Add(hierarchyTreeItem);
                    }

                    break;
                }
                case HierarchyUpdateGameObjectParentEvent updateParentEvt:
                {
                    var gameObjectGuid = updateParentEvt.gameObjectIdentifier.GameObjectId;

                    if (_items.TryGetValue(gameObjectGuid, out var hierarchyTreeItem))
                    {
                        hierarchyTreeItem.ParentGuid = updateParentEvt.parentIdentifier.GameObjectId;
                        hierarchyTreeItem.SiblingIndex = updateParentEvt.siblingIdx;
                        _updatedItems.Add(hierarchyTreeItem);
                    }

                    break;
                }
                case HierarchyForceRebuild:
                {
                    var playerCtx = player.GetPlayerContext();
                    var goGuids = playerCtx.GetAllGameObjects().Select(go =>
                        playerCtx.GetRecordIdentifier(go.GetInstanceID())).ToList();

                    var keptItems = new Dictionary<string, HierarchyTreeItem>();
                    var createdGoGuids = new List<string>(goGuids);
                    
                    foreach(var goGuid in goGuids)
                    {
                        keptItems.Add(goGuid, _items[goGuid]);
                        createdGoGuids.Remove(goGuid);
                    }

                    var deletedItems = _items.Except(keptItems).ToList();
                    
                    foreach (var deletedItem in deletedItems)
                    {
                        _deletedItems.Add(deletedItem.Value);
                        _updatedItems.Remove(deletedItem.Value);
                        _createdItems.Remove(deletedItem.Value);
                        _items.Remove(deletedItem.Key);
                    }
                    
                    foreach (var createdGoGuid in createdGoGuids)
                    {
                        var newHierarchyItem = new HierarchyTreeItem(createdGoGuid);
                        _items.Add(createdGoGuid, newHierarchyItem);
                        _createdItems.Add(newHierarchyItem);
                        _deletedItems.Remove(newHierarchyItem);
                    }
                    
                    _forceRebuild = true;
                    break;
                }
            }
        }

        private void Update()
        {
            var elapsedTime = Time.time - _lastUpdate;

            if (elapsedTime < refreshInterval && !_forceRebuild)
                return;

            _lastUpdate = Time.time;
            _forceRebuild = false;
            UpdateHierarchyTree();
        }

        private void UpdateHierarchyTree()
        {
            var controller = _hierarchyTree.viewController;
            var requiresRebuild = false;

            foreach (var createdItem in _createdItems)
            {
                try
                {
                    _hierarchyTree.AddItem(new TreeViewItemData<HierarchyTreeItem>(createdItem.ItemId, createdItem),
                        rebuildTree: true);
                    requiresRebuild = true;
                }
                catch (Exception)
                {
                    // ignored, couldn't add item (duplicate)
                }
            }

            foreach (var deletedItem in _deletedItems)
            {
                controller.TryRemoveItem(deletedItem.ItemId, rebuildTree: false);
                requiresRebuild = true;
            }

            foreach (var updatedItem in _updatedItems)
            {
                HierarchyTreeUI.TryUpdateItemVisualElement(updatedItem);

                if (updatedItem.IsParentDirty || updatedItem.IsSiblingIndexDirty)
                {
                    try
                    {
                        controller.Move(updatedItem.ItemId, updatedItem.ParentId, updatedItem.SiblingIndex,
                            rebuildTree: false);
                        requiresRebuild = true;
                    }
                    catch (Exception)
                    {
                        // ignored, item not found
                    }
                }

                updatedItem.MarkClean();
            }

            if (requiresRebuild)
            {
                controller.RebuildTree();
                _hierarchyTree.RefreshItems();
                _hierarchyTree.ClearSelection();
            }

            _createdItems.Clear();
            _deletedItems.Clear();
            _updatedItems.Clear();
        }
    }
}