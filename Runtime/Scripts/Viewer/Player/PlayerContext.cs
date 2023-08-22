using System;
using System.Collections.Generic;
using System.Linq;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PLUME
{
    public class PlayerContext
    {
        public Action<IHierarchyUpdateEvent> updatedHierarchy;

        private static readonly List<PlayerContext> Contexts = new();

        private readonly PlayerAssets _assets;
        private readonly string _name;
        private Scene _scene;
        private readonly Dictionary<int, bool> _sceneRootObjectsActive = new();
        private bool _active;

        /**
         * Correspondence between record and replay object instance ids
         */
        private readonly Dictionary<string, int> _idMap = new();

        /**
         * Correspondence between replay instance ids and record identifiers
         */
        private readonly Dictionary<int, string> _invertIdMap = new();

        private readonly Dictionary<int, GameObject> _gameObjectsByInstanceId = new();
        private readonly Dictionary<int, Transform> _transformsByInstanceId = new();
        private readonly Dictionary<int, Component> _componentByInstanceId = new();

        private PlayerContext(string name, PlayerAssets assets, Scene scene)
        {
            _name = name;
            _assets = assets;
            _scene = scene;
        }

        public static PlayerContext NewContext(string name, PlayerAssets assets)
        {
            if (Contexts.Select(ctx => ctx._name).Contains(name))
            {
                throw new Exception($"A context with this name already exists: {name}");
            }

            var scene = SceneManager.CreateScene(name);
            var ctx = new PlayerContext(name, assets, scene);
            Contexts.Add(ctx);
            return ctx;
        }

        public static void DestroyAll()
        {
            foreach (var context in Contexts)
            {
                context._gameObjectsByInstanceId.Clear();
                context._transformsByInstanceId.Clear();
                context._componentByInstanceId.Clear();
                SceneManager.UnloadSceneAsync(context._scene);
            }

            Contexts.Clear();
            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
        }

        public static void Destroy(PlayerContext ctx)
        {
            if (!Contexts.Contains(ctx))
                return;

            ctx._gameObjectsByInstanceId.Clear();
            ctx._transformsByInstanceId.Clear();
            ctx._componentByInstanceId.Clear();
            SceneManager.UnloadSceneAsync(ctx._scene);
            Contexts.Remove(ctx);

            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
        }

        public static void Activate(PlayerContext playerContext)
        {
            foreach (var ctx in Contexts)
            {
                if (ctx == playerContext)
                    continue;

                ctx.DisableRootGameObjects();
                ctx._active = false;
            }

            playerContext.EnableRootGameObjects();
            playerContext._active = true;
            SceneManager.SetActiveScene(playerContext._scene);
        }

        public static PlayerContext GetActiveContext()
        {
            foreach (var ctx in Contexts)
            {
                if (ctx.IsActive())
                    return ctx;
            }

            return null;
        }

        public Object FindObjectByInstanceId(int instanceId)
        {
            if (_gameObjectsByInstanceId.TryGetValue(instanceId, out var gameObject))
            {
                return gameObject;
            }

            if (_transformsByInstanceId.TryGetValue(instanceId, out var transform))
            {
                return transform;
            }

            if (_componentByInstanceId.TryGetValue(instanceId, out var component))
            {
                return component;
            }

            return ObjectExtensions.FindObjectFromInstanceID(instanceId);
        }
        
        public GameObject FindGameObjectByInstanceId(int instanceId)
        {
            return _gameObjectsByInstanceId.GetValueOrDefault(instanceId);
        }
        
        public Transform FindTransformByInstanceId(int instanceId)
        {
            return _transformsByInstanceId.GetValueOrDefault(instanceId);
        }
        
        public void Reset()
        {
            foreach (var rootGameObject in _scene.GetRootGameObjects())
            {
                updatedHierarchy?.Invoke(
                    new HierarchyUpdateDestroyTransformEvent(rootGameObject.transform.GetInstanceID()));
                Object.DestroyImmediate(rootGameObject);
            }

            _gameObjectsByInstanceId.Clear();
            _transformsByInstanceId.Clear();
            _componentByInstanceId.Clear();
            _idMap.Clear();
            _invertIdMap.Clear();
        }

        public void PlaySamples(PlayerModule[] playerModules, IEnumerable<UnpackedSample> samples)
        {
            foreach (var sample in samples)
            {
                if (!IsActive())
                {
                    Activate(this);
                }

                foreach (var playerModule in playerModules)
                {
                    playerModule.PlaySample(this, sample);
                }
            }
        }

        public void SetParent(TransformGameObjectIdentifier transformId,
            TransformGameObjectIdentifier transformParentId)
        {
            var t = GetOrCreateTransformByIdentifier(transformId);
            var parent = GetOrCreateTransformByIdentifier(transformParentId);
            var prevParentTransformId = t.parent == null ? 0 : t.parent.GetInstanceID();
            t.parent = parent;
            var newParentTransformId = parent == null ? 0 : parent.GetInstanceID();
            updatedHierarchy?.Invoke(new HierarchyUpdateParentEvent(t.GetInstanceID(), t.GetSiblingIndex(), prevParentTransformId,
                newParentTransformId));
        }

        public void SetSiblingIndex(TransformGameObjectIdentifier transformId, int siblingIndex)
        {
            var t = GetOrCreateTransformByIdentifier(transformId);
            var prevSiblingIndex = t.GetSiblingIndex();
            t.SetSiblingIndex(siblingIndex);
            updatedHierarchy?.Invoke(
                new HierarchyUpdateSiblingIndexEvent(t.GetInstanceID(), prevSiblingIndex, siblingIndex));
        }

        public void SetActive(TransformGameObjectIdentifier id, bool active)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            go.SetActive(active);
            updatedHierarchy?.Invoke(new HierarchyUpdateEnabledEvent(go.transform.GetInstanceID(), active));
        }

        private void EnableRootGameObjects()
        {
            foreach (var go in _scene.GetRootGameObjects())
            {
                if (_sceneRootObjectsActive.TryGetValue(go.GetInstanceID(), out var active))
                {
                    go.SetActive(active);
                }
            }
        }

        private void DisableRootGameObjects()
        {
            _sceneRootObjectsActive.Clear();

            foreach (var go in _scene.GetRootGameObjects())
            {
                _sceneRootObjectsActive.Add(go.GetInstanceID(), go.activeSelf);
                go.SetActive(false);
            }
        }

        public IEnumerable<GameObject> GetAllGameObjects()
        {
            return _gameObjectsByInstanceId.Values;
        }

        public GameObject GetOrCreateGameObjectByIdentifier(TransformGameObjectIdentifier identifier)
        {
            if (identifier == null)
                return null;

            var replayGoInstanceId = GetReplayInstanceId(identifier.GameObjectId);
            var replayTransformInstanceId = GetReplayInstanceId(identifier.TransformId);

            if (replayGoInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGoInstanceId.Value);

                if (go != null)
                {
                    if (!replayTransformInstanceId.HasValue)
                    {
                        _transformsByInstanceId[go.transform.GetInstanceID()] = go.transform;
                        TryAddIdentifierCorrespondence(identifier.TransformId, go.transform.GetInstanceID());
                    }

                    return go;
                }
            }

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value);

                if (t != null)
                {
                    var go = t.gameObject;
                    _gameObjectsByInstanceId[go.GetInstanceID()] = go;
                    TryAddIdentifierCorrespondence(identifier.GameObjectId, go.GetInstanceID());
                    return go;
                }
            }

            if (!IsActive())
                throw new Exception($"Trying to instantiate a GameObject in context {_name}");

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(identifier.GameObjectId, newGameObject.GetInstanceID());
            TryAddIdentifierCorrespondence(identifier.TransformId, newTransform.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(newTransform.GetInstanceID()));
            return newGameObject;
        }

        public Transform GetOrCreateTransformByIdentifier(TransformGameObjectIdentifier identifier)
        {
            if (identifier == null)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(identifier.TransformId);
            var replayGameObjectInstanceId = GetReplayInstanceId(identifier.GameObjectId);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value);

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(identifier.GameObjectId, t.gameObject.GetInstanceID());
                    }

                    return t;
                }
            }

            if (replayGameObjectInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGameObjectInstanceId.Value);

                if (go != null)
                {
                    var t = go.transform;
                    _transformsByInstanceId[t.GetInstanceID()] = t;
                    TryAddIdentifierCorrespondence(identifier.TransformId, t.GetInstanceID());
                    return t;
                }
            }

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(identifier.TransformId, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(identifier.GameObjectId, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(newTransform.GetInstanceID()));
            return newTransform;
        }

        public RectTransform GetOrCreateRectTransformByIdentifier(TransformGameObjectIdentifier identifier)
        {
            if (identifier == null)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(identifier.TransformId);
            var replayGameObjectInstanceId = GetReplayInstanceId(identifier.GameObjectId);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value) as RectTransform;

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(identifier.GameObjectId, t.gameObject.GetInstanceID());
                    }

                    return t;
                }
            }

            if (replayGameObjectInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGameObjectInstanceId.Value);

                if (go != null)
                {
                    if (go!.transform is not RectTransform) go.AddComponent<RectTransform>();
                    var t = go.transform;
                    _transformsByInstanceId[t.GetInstanceID()] = t;
                    TryAddIdentifierCorrespondence(identifier.TransformId, t.GetInstanceID());
                    return t as RectTransform;
                }
            }

            var newGameObject = new GameObject("newGameObject", typeof(RectTransform));
            var newTransform = newGameObject.transform as RectTransform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(identifier.TransformId, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(identifier.GameObjectId, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(newTransform.GetInstanceID()));
            return newTransform;
        }

        public T GetOrDefaultAssetByIdentifier<T>(AssetIdentifier identifier) where T : Object
        {
            if (identifier == null)
            {
                return null;
            }

            return _assets.FindAssetByHash<T>(identifier.Hash);
        }

        public T GetOrCreateComponentByIdentifier<T>(ComponentIdentifier identifier) where T : Component
        {
            if (identifier == null)
                return null;

            var replayComponentInstanceId = GetReplayInstanceId(identifier.Id);

            if (replayComponentInstanceId.HasValue)
            {
                if (!_componentByInstanceId.ContainsKey(replayComponentInstanceId.Value))
                {
                    _componentByInstanceId[replayComponentInstanceId.Value] =
                        ObjectExtensions.FindObjectFromInstanceID(replayComponentInstanceId.Value) as T;
                }

                return _componentByInstanceId.GetValueOrDefault(replayComponentInstanceId.Value) as T;
            }

            var go = GetOrCreateGameObjectByIdentifier(identifier.ParentId);

            var component = go.AddComponent<T>();

            // Component is null when DisallowedMultipleComponent is enabled on the type T
            if (component == null)
            {
                component = go.GetComponent<T>();
                Debug.Log(
                    $"{component} must have been instantiated implicitly by RequireComponent. You can safely ignore the previous log message by the UnityEngine.");
            }

            _componentByInstanceId[component.GetInstanceID()] = component;
            TryAddIdentifierCorrespondence(identifier.Id, component.GetInstanceID());
            return component;
        }

        public bool TryDestroyGameObjectByIdentifier(TransformGameObjectIdentifier identifier)
        {
            var goReplayInstanceId = GetReplayInstanceId(identifier.GameObjectId);
            var transformReplayInstanceId = GetReplayInstanceId(identifier.TransformId);

            GameObject go = null;

            if (goReplayInstanceId.HasValue)
            {
                go = _gameObjectsByInstanceId.GetValueOrDefault(goReplayInstanceId.Value);
            }
            else if (transformReplayInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(transformReplayInstanceId.Value);
                if (t == null) return true;
                go = t.gameObject;
            }

            if (go != null)
            {
                _gameObjectsByInstanceId.Remove(go.GetInstanceID());
                _transformsByInstanceId.Remove(go.transform.GetInstanceID());

                foreach (var component in go.GetComponents<Component>())
                {
                    _componentByInstanceId.Remove(component.GetInstanceID());
                    RemoveIdentifierCorrespondence(GetRecordIdentifier(component.GetInstanceID()));
                }

                updatedHierarchy?.Invoke(new HierarchyUpdateDestroyTransformEvent(go.transform.GetInstanceID()));
                Object.DestroyImmediate(go);
            }

            var gameObjectsEntriesToRemove = _gameObjectsByInstanceId.Where(pair => pair.Value == null);
            var transformsEntriesToRemove = _transformsByInstanceId.Where(pair => pair.Value == null);
            var componentsEntriesToRemove = _componentByInstanceId.Where(pair => pair.Value == null);

            foreach (var entry in gameObjectsEntriesToRemove)
            {
                _gameObjectsByInstanceId.Remove(entry.Key);
            }

            foreach (var entry in transformsEntriesToRemove)
            {
                _transformsByInstanceId.Remove(entry.Key);
            }

            foreach (var entry in componentsEntriesToRemove)
            {
                _componentByInstanceId.Remove(entry.Key);
            }

            RemoveIdentifierCorrespondence(identifier.GameObjectId);
            RemoveIdentifierCorrespondence(identifier.TransformId);
            return true;
        }

        public bool TryDestroyComponentByIdentifier(ComponentIdentifier identifier)
        {
            var componentReplayInstanceId = GetReplayInstanceId(identifier.Id);

            if (componentReplayInstanceId.HasValue)
            {
                var component = _componentByInstanceId.GetValueOrDefault(componentReplayInstanceId.Value);

                RemoveIdentifierCorrespondence(identifier.Id);
                if (component != null)
                {
                    _componentByInstanceId.Remove(component.GetInstanceID());
                    Object.DestroyImmediate(component);
                }

                return true;
            }

            var componentsEntriesToRemove = _componentByInstanceId.Where(pair => pair.Value == null);

            foreach (var entry in componentsEntriesToRemove)
            {
                _componentByInstanceId.Remove(entry.Key);
            }

            return false;
        }

        public string GetRecordIdentifier(int replayInstanceId)
        {
            return _invertIdMap.GetValueOrDefault(replayInstanceId, null);
        }

        public int? GetReplayInstanceId(string recordIdentifier)
        {
            return _idMap.TryGetValue(recordIdentifier, out var replayInstanceId) ? replayInstanceId : null;
        }

        public bool RemoveIdentifierCorrespondence(string recordIdentifier)
        {
            var result = true;

            if (_idMap.TryGetValue(recordIdentifier, out var instanceId))
            {
                result = _invertIdMap.Remove(instanceId);
            }

            result = result && _idMap.Remove(recordIdentifier);

            return result;
        }

        public bool TryAddIdentifierCorrespondence(string recordIdentifier, int replayInstanceId)
        {
            return _idMap.TryAdd(recordIdentifier, replayInstanceId) &&
                   _invertIdMap.TryAdd(replayInstanceId, recordIdentifier);
        }

        public bool TryAddAssetIdentifierCorrespondence(AssetIdentifier recordIdentifier, Object replayAsset)
        {
            if (recordIdentifier == null || replayAsset == null)
                return false;

            return _idMap.TryAdd(recordIdentifier.Id, replayAsset.GetInstanceID()) &&
                   _invertIdMap.TryAdd(replayAsset.GetInstanceID(), recordIdentifier.Id);
        }

        public bool IsActive()
        {
            return _active;
        }

        public Scene GetScene()
        {
            return _scene;
        }
    }
}