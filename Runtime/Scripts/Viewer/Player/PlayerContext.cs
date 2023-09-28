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
        private readonly Dictionary<int, string> _gameObjectsTagByInstanceId = new();
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
                context._gameObjectsTagByInstanceId.Clear();
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
            ctx._gameObjectsTagByInstanceId.Clear();
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
                Object.DestroyImmediate(rootGameObject);
            }
            
            updatedHierarchy?.Invoke(new HierarchyUpdateResetEvent());

            _gameObjectsByInstanceId.Clear();
            _gameObjectsTagByInstanceId.Clear();
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

        public void SetGameObjectTag(TransformGameObjectIdentifier id, string tag)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            _gameObjectsTagByInstanceId[go.GetInstanceID()] = tag;
        }

        public string GetGameObjectTag(int gameObjectId)
        {
            _gameObjectsTagByInstanceId.TryGetValue(gameObjectId, out var tag);
            return tag;
        }
        
        public void SetParent(TransformGameObjectIdentifier id, TransformGameObjectIdentifier parentId)
        {
            var t = GetOrCreateTransformByIdentifier(id);
            var parent = GetOrCreateTransformByIdentifier(parentId);
            t.SetParent(parent);
            updatedHierarchy?.Invoke(new HierarchyUpdateParentEvent(id.TransformId, parentId?.TransformId, t.GetSiblingIndex()));
        }

        public void SetSiblingIndex(TransformGameObjectIdentifier id, int siblingIndex)
        {
            var t = GetOrCreateTransformByIdentifier(id);
            t.SetSiblingIndex(siblingIndex);
            updatedHierarchy?.Invoke(
                new HierarchyUpdateSiblingIndexEvent(id.TransformId, siblingIndex));
        }

        public void SetActive(TransformGameObjectIdentifier id, bool active)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            go.SetActive(active);
            updatedHierarchy?.Invoke(new HierarchyUpdateEnabledEvent(id.TransformId, active));
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

        public GameObject GetOrCreateGameObjectByIdentifier(TransformGameObjectIdentifier id)
        {
            if (id == null)
                return null;

            var replayGoInstanceId = GetReplayInstanceId(id.GameObjectId);
            var replayTransformInstanceId = GetReplayInstanceId(id.TransformId);

            if (replayGoInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGoInstanceId.Value);

                if (go != null)
                {
                    if (!replayTransformInstanceId.HasValue)
                    {
                        _transformsByInstanceId[go.transform.GetInstanceID()] = go.transform;
                        TryAddIdentifierCorrespondence(id.TransformId, go.transform.GetInstanceID());
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
                    TryAddIdentifierCorrespondence(id.GameObjectId, go.GetInstanceID());
                    return go;
                }
            }

            if (!IsActive())
                throw new Exception($"Trying to instantiate a GameObject in context {_name}");

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(id.GameObjectId, newGameObject.GetInstanceID());
            TryAddIdentifierCorrespondence(id.TransformId, newTransform.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(id.TransformId));
            return newGameObject;
        }

        public Transform GetOrCreateTransformByIdentifier(TransformGameObjectIdentifier id)
        {
            if (id == null)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(id.TransformId);
            var replayGameObjectInstanceId = GetReplayInstanceId(id.GameObjectId);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value);

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(id.GameObjectId, t.gameObject.GetInstanceID());
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
                    TryAddIdentifierCorrespondence(id.TransformId, t.GetInstanceID());
                    return t;
                }
            }

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(id.TransformId, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(id.GameObjectId, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(id.TransformId));
            return newTransform;
        }

        public RectTransform GetOrCreateRectTransformByIdentifier(TransformGameObjectIdentifier id)
        {
            if (id == null)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(id.TransformId);
            var replayGameObjectInstanceId = GetReplayInstanceId(id.GameObjectId);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value) as RectTransform;

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(id.GameObjectId, t.gameObject.GetInstanceID());
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
                    TryAddIdentifierCorrespondence(id.TransformId, t.GetInstanceID());
                    return t as RectTransform;
                }
            }

            var newGameObject = new GameObject("newGameObject", typeof(RectTransform));
            var newTransform = newGameObject.transform as RectTransform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(id.TransformId, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(id.GameObjectId, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyUpdateCreateTransformEvent(id.TransformId));
            return newTransform;
        }

        public T GetOrDefaultAssetByIdentifier<T>(AssetIdentifier id) where T : Object
        {
            return _assets.GetOrDefaultAssetByIdentifier<T>(id);
        }

        public T GetOrCreateComponentByIdentifier<T>(ComponentIdentifier id) where T : Component
        {
            if (id == null)
                return null;

            var replayComponentInstanceId = GetReplayInstanceId(id.Id);

            if (replayComponentInstanceId.HasValue)
            {
                if (!_componentByInstanceId.ContainsKey(replayComponentInstanceId.Value))
                {
                    _componentByInstanceId[replayComponentInstanceId.Value] =
                        ObjectExtensions.FindObjectFromInstanceID(replayComponentInstanceId.Value) as T;
                }

                return _componentByInstanceId.GetValueOrDefault(replayComponentInstanceId.Value) as T;
            }

            var go = GetOrCreateGameObjectByIdentifier(id.ParentId);

            var component = go.AddComponent<T>();

            // Component is null when DisallowedMultipleComponent is enabled on the type T
            if (component == null)
            {
                component = go.GetComponent<T>();
                Debug.Log(
                    $"{component} must have been instantiated implicitly by RequireComponent. You can safely ignore the previous log message by the UnityEngine.");
            }

            _componentByInstanceId[component.GetInstanceID()] = component;
            TryAddIdentifierCorrespondence(id.Id, component.GetInstanceID());
            return component;
        }

        public bool TryDestroyGameObjectByIdentifier(TransformGameObjectIdentifier id)
        {
            var goReplayInstanceId = GetReplayInstanceId(id.GameObjectId);
            var transformReplayInstanceId = GetReplayInstanceId(id.TransformId);

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
                _gameObjectsTagByInstanceId.Remove(go.GetInstanceID());
                _transformsByInstanceId.Remove(go.transform.GetInstanceID());

                foreach (var component in go.GetComponents<Component>())
                {
                    _componentByInstanceId.Remove(component.GetInstanceID());
                    RemoveIdentifierCorrespondence(GetRecordIdentifier(component.GetInstanceID()));
                }
                
                updatedHierarchy?.Invoke(new HierarchyUpdateDestroyTransformEvent(id.TransformId));
                Object.DestroyImmediate(go);
            }

            var gameObjectsToRemove = new List<int>();
            var transformsToRemove = new List<int>();
            var componentsToRemove = new List<int>();

            foreach (var (instanceId, gameObject) in _gameObjectsByInstanceId)
            {
                if(gameObject == null)
                    gameObjectsToRemove.Add(instanceId);
            }
            
            foreach (var (instanceId, transform) in _transformsByInstanceId)
            {
                if(transform == null)
                    transformsToRemove.Add(instanceId);
            }
            
            foreach (var (instanceId, component) in _componentByInstanceId)
            {
                if(component == null)
                    componentsToRemove.Add(instanceId);
            }
            
            foreach (var gameObjectInstanceId in gameObjectsToRemove)
            {
                _gameObjectsByInstanceId.Remove(gameObjectInstanceId);
                _gameObjectsTagByInstanceId.Remove(gameObjectInstanceId);
            }

            foreach (var transformInstanceId in transformsToRemove)
            {
                var recordIdentifier = GetRecordIdentifier(transformInstanceId);
                _transformsByInstanceId.Remove(transformInstanceId);
                if(recordIdentifier != null)
                    updatedHierarchy?.Invoke(new HierarchyUpdateDestroyTransformEvent(recordIdentifier));
            }

            foreach (var componentInstanceId in componentsToRemove)
            {
                _componentByInstanceId.Remove(componentInstanceId);
            }
            
            RemoveIdentifierCorrespondence(id.GameObjectId);
            RemoveIdentifierCorrespondence(id.TransformId);
            return true;
        }

        public bool TryDestroyComponentByIdentifier(ComponentDestroyIdentifier identifier)
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