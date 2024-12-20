using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace PLUME.Viewer
{
    public class HierarchyTreePresenter : MonoBehaviour
    {
        public Player.Player player;
        public double refreshInterval = 60; // in frames

        private HierarchyTreeUI _hierarchyTreeUI;
        private TreeView _hierarchyTree;

        private readonly Dictionary<Guid, HierarchyTreeItemData> _currentItems = new();

        // New or updated items that need to be processed
        private readonly Dictionary<Guid, HierarchyTreeItemData> _createdOrUpdatedItems = new();
        private readonly HashSet<Guid> _destroyedItems = new();

        private double _lastFrameUpdate;

        private void Awake()
        {
            _hierarchyTreeUI = GetComponent<HierarchyTreeUI>();
        }

        private void Start()
        {
            _hierarchyTree = _hierarchyTreeUI.HierarchyTree;
            player.mainContextUpdatedHierarchy += OnHierarchyUpdateEvent;
        }

        public void OnHierarchyUpdateEvent(IHierarchyUpdateEvent evt)
        {
            switch (evt)
            {
                case HierarchyCreateGameObjectEvent createEvt:
                {
                    var gameObjectGuid = Guid.Parse(createEvt.gameObjectIdentifier.GameObjectId);

                    if (!_currentItems.ContainsKey(gameObjectGuid))
                    {
                        var item = new HierarchyTreeItemData(gameObjectGuid);
                        _createdOrUpdatedItems[gameObjectGuid] = item;
                    }

                    break;
                }
                case HierarchyDestroyGameObjectEvent destroyEvt:
                {
                    var gameObjectGuid = Guid.Parse(destroyEvt.gameObjectIdentifier.GameObjectId);
                    _destroyedItems.Add(gameObjectGuid);
                    break;
                }
                case HierarchyUpdateGameObjectNameEvent nameUpdateEvt:
                {
                    var gameObjectGuid = Guid.Parse(nameUpdateEvt.gameObjectIdentifier.GameObjectId);

                    if (!_createdOrUpdatedItems.TryGetValue(gameObjectGuid, out var item))
                    {
                        if (!_currentItems.TryGetValue(gameObjectGuid, out item))
                        {
                            item = new HierarchyTreeItemData(gameObjectGuid);
                        }
                    }

                    item.Name = nameUpdateEvt.name;
                    _createdOrUpdatedItems[gameObjectGuid] = item;
                    break;
                }
                case HierarchyUpdateGameObjectSiblingIndexEvent siblingUpdateEvt:
                {
                    var gameObjectGuid = Guid.Parse(siblingUpdateEvt.gameObjectIdentifier.GameObjectId);

                    if (!_createdOrUpdatedItems.TryGetValue(gameObjectGuid, out var item))
                    {
                        if (!_currentItems.TryGetValue(gameObjectGuid, out item))
                        {
                            item = new HierarchyTreeItemData(gameObjectGuid);
                        }
                    }

                    item.SiblingIndex = siblingUpdateEvt.siblingIndex;
                    _createdOrUpdatedItems[gameObjectGuid] = item;
                    break;
                }
                case HierarchyUpdateGameObjectEnabledEvent enabledUpdateEvt:
                {
                    var gameObjectGuid = Guid.Parse(enabledUpdateEvt.gameObjectIdentifier.GameObjectId);

                    if (!_createdOrUpdatedItems.TryGetValue(gameObjectGuid, out var item))
                    {
                        if (!_currentItems.TryGetValue(gameObjectGuid, out item))
                        {
                            item = new HierarchyTreeItemData(gameObjectGuid);
                        }
                    }

                    item.Enabled = enabledUpdateEvt.enabled;
                    _createdOrUpdatedItems[gameObjectGuid] = item;
                    break;
                }
                case HierarchyUpdateGameObjectParentEvent updateParentEvt:
                {
                    var gameObjectGuid = Guid.Parse(updateParentEvt.gameObjectIdentifier.GameObjectId);
                    var parentGameObjectGuid = Guid.Parse(updateParentEvt.parentIdentifier.GameObjectId);

                    if (!_createdOrUpdatedItems.TryGetValue(gameObjectGuid, out var item))
                    {
                        if (!_currentItems.TryGetValue(gameObjectGuid, out item))
                        {
                            item = new HierarchyTreeItemData(gameObjectGuid);
                        }
                    }

                    if (parentGameObjectGuid != Guid.Empty &&
                        !_createdOrUpdatedItems.TryGetValue(parentGameObjectGuid, out var parentItem))
                    {
                        if (!_currentItems.TryGetValue(parentGameObjectGuid, out parentItem))
                        {
                            Debug.Log("created missing parent");
                            parentItem = new HierarchyTreeItemData(gameObjectGuid);
                            _createdOrUpdatedItems[parentGameObjectGuid] = parentItem;
                        }
                    }

                    item.ParentGameObjectGuid = parentGameObjectGuid;
                    item.SiblingIndex = updateParentEvt.siblingIdx;
                    _createdOrUpdatedItems[gameObjectGuid] = item;
                    break;
                }
                case HierarchyForceRebuild:
                {
                    _destroyedItems.Clear();
                    _createdOrUpdatedItems.Clear();

                    var playerCtx = player.GetMainPlayerContext();
                    var gameObjectsGuids = playerCtx.GetAllGameObjects()
                        .Select(go => playerCtx.GetRecordIdentifier(go.GetInstanceID()))
                        .ToList();

                    var existingItems = new Dictionary<Guid, HierarchyTreeItemData>();
                    var createdItems = new List<Guid>(gameObjectsGuids);

                    foreach (var goGuid in gameObjectsGuids)
                    {
                        if (!_currentItems.ContainsKey(goGuid)) continue;
                        existingItems.TryAdd(goGuid, _currentItems[goGuid]);
                        createdItems.Remove(goGuid);
                    }

                    var deletedItems = _currentItems.Except(existingItems).ToList();

                    foreach (var (guid, item) in deletedItems)
                    {
                        _destroyedItems.Add(guid);
                    }

                    foreach (var createdGoGuid in createdItems)
                    {
                        var item = new HierarchyTreeItemData(createdGoGuid);
                        _createdOrUpdatedItems[createdGoGuid] = item;
                    }

                    break;
                }
            }
        }

        private void Update()
        {
            var elapsedTime = Time.frameCount - _lastFrameUpdate;

            if (elapsedTime < refreshInterval)
                return;

            _lastFrameUpdate = Time.frameCount;
            UpdateHierarchyTree();
        }

        private void UpdateHierarchyTree()
        {
            if (_createdOrUpdatedItems.Count == 0)
                return;

            var controller = _hierarchyTree.viewController;
            var rebuildTree = false;

            Profiler.BeginSample("UpdateHierarchyTree");

            var justCreated = new HashSet<Guid>();

            // 1. Create new items if needed
            foreach (var (guid, updatedItem) in _createdOrUpdatedItems)
            {
                // Skip items that already exist in the tree.
                if (_currentItems.ContainsKey(guid)) continue;

                try
                {
                    // Item doesn't exist in the tree, create it.
                    _hierarchyTree.AddItem(
                        new TreeViewItemData<HierarchyTreeItemData>(updatedItem.GetId(), updatedItem),
                        rebuildTree: false);
                    _currentItems.Add(updatedItem.GameObjectGuid, updatedItem);
                    justCreated.Add(updatedItem.GameObjectGuid);
                    rebuildTree = true;
                }
                catch (Exception)
                {
                    Debug.LogWarning(
                        $"Couldn't add item '{updatedItem.Name}' to tree view, item already exists. (guid={updatedItem.GameObjectGuid}, id={updatedItem.GetId()})");
                }
            }

            // 2. Update existing items with the new data
            foreach (var (guid, updatedItem) in _createdOrUpdatedItems)
            {
                if (!_currentItems.TryGetValue(guid, out var item))
                {
                    Debug.LogWarning(
                        $"Couldn't update item '{updatedItem.Name}' in tree view, item not found. (guid={updatedItem.GameObjectGuid}, id={updatedItem.GetId()})");
                    continue;
                }

                var attributesChanged = item.Name != updatedItem.Name ||
                                        item.Enabled != updatedItem.Enabled || justCreated.Contains(guid);

                if (attributesChanged)
                {
                    item.Name = updatedItem.Name;
                    item.Enabled = updatedItem.Enabled;
                    _hierarchyTreeUI.TryUpdateItemVisualElement(updatedItem);
                }

                var hierarchyChanged = item.ParentGameObjectGuid != updatedItem.ParentGameObjectGuid ||
                                       item.SiblingIndex != updatedItem.SiblingIndex || justCreated.Contains(guid);

                if (!hierarchyChanged) continue;

                try
                {
                    controller.Move(updatedItem.GetId(), updatedItem.GetParentId(), updatedItem.SiblingIndex,
                        rebuildTree: false);
                    rebuildTree = true;
                }
                catch (Exception)
                {
                    Debug.LogWarning(
                        $"Couldn't move item '{updatedItem.Name}' in tree view, item not found. (guid={updatedItem.GameObjectGuid}, id={updatedItem.GetId()}, parentGuid={updatedItem.ParentGameObjectGuid}, parentId={updatedItem.GetParentId()}, siblingIndex={updatedItem.SiblingIndex})");
                }

                _currentItems[guid] = updatedItem;
            }

            // 3. Remove destroyed items
            foreach (var guid in _destroyedItems)
            {
                if (!_currentItems.TryGetValue(guid, out var item)) continue;

                if (controller.TryRemoveItem(item.GetId(), rebuildTree: false))
                {
                    _currentItems.Remove(guid);

                    try
                    {
                        _hierarchyTree.RemoveFromSelectionById(item.GetId());
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning(
                            $"Failed to remove item '{item.Name}' from selection. (guid={item.GameObjectGuid}, id={item.GetId()})");
                    }

                    rebuildTree = true;
                }
                else
                {
                    Debug.LogWarning(
                        $"Failed to remove item '{item.Name}' from tree view. (guid={item.GameObjectGuid}, id={item.GetId()})");
                }
            }

            _destroyedItems.Clear();
            _createdOrUpdatedItems.Clear();

            if (rebuildTree)
            {
                controller.RebuildTree();
                _hierarchyTree.RefreshItems();
            }

            Profiler.EndSample();
        }
    }
}