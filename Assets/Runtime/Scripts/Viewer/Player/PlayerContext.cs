using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using LoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;
using Object = UnityEngine.Object;

namespace PLUME.Viewer.Player
{
    public class PlayerContext
    {
        public Action<IHierarchyUpdateEvent> updatedHierarchy;

        private static readonly List<PlayerContext> Contexts = new();

        private readonly RecordAssetBundle _recordAssetBundle;
        private readonly string _name;
        private Scene _scene;
        private readonly Dictionary<int, bool> _sceneRootObjectsActive = new();
        private bool _active;

        /**
         * Correspondence between record and replay object instance ids
         */
        private readonly Dictionary<Guid, int> _idMap = new();

        /**
         * Correspondence between replay instance ids and record identifiers
         */
        private readonly Dictionary<int, Guid> _invertIdMap = new();

        private readonly Dictionary<int, GameObject> _gameObjectsByInstanceId = new();
        private readonly Dictionary<int, string> _gameObjectsTagByInstanceId = new();
        private readonly Dictionary<int, Transform> _transformsByInstanceId = new();
        private readonly Dictionary<int, Component> _componentByInstanceId = new();

        private PlayerContext(string name, RecordAssetBundle recordAssetBundle, Scene scene)
        {
            _name = name;
            _recordAssetBundle = recordAssetBundle;
            _scene = scene;
        }
        
        internal static async UniTask<PlayerContext> CreateMainPlayerContext(RecordAssetBundle assets)
        {
            const string mainPlayerContextName = "MainPlayerContext";
            
            if (Contexts.Select(ctx => ctx._name).Contains(mainPlayerContextName))
            {
                throw new Exception("MainPlayerContext already exists");
            }
            
            var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive);
            await SceneManager.LoadSceneAsync(mainPlayerContextName, loadSceneParameters);
            var scene = SceneManager.GetSceneByName(mainPlayerContextName);

            if (!scene.IsValid())
                throw new Exception("Failed to load MainPlayerContext scene");
            
            SceneManager.SetActiveScene(scene);
            
            var ctx = new PlayerContext(mainPlayerContextName, assets, scene);
            Contexts.Add(ctx);
            return ctx;
        }

        public static PlayerContext NewTemporaryContext(string name, RecordAssetBundle assets)
        {
            if (name == "MainPlayerContext")
            {
                throw new Exception("The name MainPlayerContext is reserved");
            }
            
            if (Contexts.Select(ctx => ctx._name).Contains(name))
            {
                throw new Exception($"A context with this name already exists: {name}");
            }
            
            var scene = SceneManager.CreateScene(name);
            SceneManager.SetActiveScene(scene);
            
            // Apply default lighting settings
            // RenderSettings.skybox = BuiltinAssets.Instance.defaultSkybox;
            // Lightmapping.lightingSettings = Resources.Load<LightingSettings>("Default Lighting Settings");

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

            updatedHierarchy?.Invoke(new HierarchyForceRebuild());

            _gameObjectsByInstanceId.Clear();
            _gameObjectsTagByInstanceId.Clear();
            _transformsByInstanceId.Clear();
            _componentByInstanceId.Clear();
            _idMap.Clear();
            _invertIdMap.Clear();
        }

        public void PlayFrames(PlayerModule[] playerModules, IEnumerable<FrameSample> frames)
        {
            if (!IsActive())
            {
                Activate(this);
            }

            foreach (var frame in frames)
            {
                PlayFrame(playerModules, frame);
            }
        }

        public void PlayFrame(PlayerModule[] playerModules, FrameSample frame)
        {
            if (!IsActive())
            {
                Activate(this);
            }

            foreach (var sample in frame.Data)
            {
                foreach (var playerModule in playerModules)
                {
                    playerModule.PlaySample(this, sample);
                }
            }
        }

        public void SetGameObjectTag(GameObjectIdentifier id, string tag)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            _gameObjectsTagByInstanceId[go.GetInstanceID()] = tag;
        }

        public string GetGameObjectTag(int gameObjectId)
        {
            _gameObjectsTagByInstanceId.TryGetValue(gameObjectId, out var tag);
            return tag;
        }

        public void SetParent(ComponentIdentifier transformIdentifier, ComponentIdentifier parentTransformIdentifier)
        {
            var t = GetOrCreateTransformByIdentifier(transformIdentifier);
            var parent = GetOrCreateTransformByIdentifier(parentTransformIdentifier);
            t.SetParent(parent);
            updatedHierarchy?.Invoke(new HierarchyUpdateGameObjectParentEvent(transformIdentifier.ParentId,
                parentTransformIdentifier.ParentId, t.GetSiblingIndex()));
        }

        public void SetName(GameObjectIdentifier id, string name)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            go.name = name;
            updatedHierarchy?.Invoke(new HierarchyUpdateGameObjectNameEvent(id, name));
        }
        
        public void SetSiblingIndex(ComponentIdentifier transformIdentifier, int siblingIndex)
        {
            var t = GetOrCreateTransformByIdentifier(transformIdentifier);
            t.SetSiblingIndex(siblingIndex);
            updatedHierarchy?.Invoke(
                new HierarchyUpdateGameObjectSiblingIndexEvent(transformIdentifier.ParentId, siblingIndex));
        }

        public void SetActive(GameObjectIdentifier id, bool active)
        {
            var go = GetOrCreateGameObjectByIdentifier(id);
            go.SetActive(active);
            updatedHierarchy?.Invoke(new HierarchyUpdateGameObjectEnabledEvent(id, active));
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
            return _gameObjectsByInstanceId.Values.Where(go => go != null);
        }

        public IEnumerable<Component> GetAllComponents()
        {
            return _componentByInstanceId.Values.Where(component => component != null);
        }

        public GameObject GetOrCreateGameObjectByIdentifier(GameObjectIdentifier id)
        {
            var gameObjectGuid = Guid.Parse(id.GameObjectId);
            var transformGuid = Guid.Parse(id.TransformId);
            
            if (transformGuid == Guid.Empty)
                return null;

            var replayGoInstanceId = GetReplayInstanceId(gameObjectGuid);
            var replayTransformInstanceId = GetReplayInstanceId(transformGuid);

            if (replayGoInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGoInstanceId.Value);

                if (go != null)
                {
                    if (!replayTransformInstanceId.HasValue)
                    {
                        _transformsByInstanceId[go.transform.GetInstanceID()] = go.transform;
                        TryAddIdentifierCorrespondence(transformGuid, go.transform.GetInstanceID());
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
                    TryAddIdentifierCorrespondence(gameObjectGuid, go.GetInstanceID());
                    return go;
                }
            }

            if (!IsActive())
                throw new Exception($"Trying to instantiate a GameObject in context {_name}");

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(gameObjectGuid, newGameObject.GetInstanceID());
            TryAddIdentifierCorrespondence(transformGuid, newTransform.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyCreateGameObjectEvent(id));
            return newGameObject;
        }

        public Transform GetOrCreateTransformByIdentifier(ComponentIdentifier id)
        {
            var transformGuid = Guid.Parse(id.ComponentId);
            var gameObjectGuid = Guid.Parse(id.ParentId.GameObjectId);
            
            if (transformGuid == Guid.Empty)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(transformGuid);
            var replayGameObjectInstanceId = GetReplayInstanceId(gameObjectGuid);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value);

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(gameObjectGuid, t.gameObject.GetInstanceID());
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
                    TryAddIdentifierCorrespondence(transformGuid, t.GetInstanceID());
                    return t;
                }
            }

            var newGameObject = new GameObject();
            var newTransform = newGameObject.transform;
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(transformGuid, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(gameObjectGuid, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyCreateGameObjectEvent(id.ParentId));
            return newTransform;
        }

        public RectTransform GetOrCreateRectTransformByIdentifier(ComponentIdentifier id)
        {
            var transformGuid = Guid.Parse(id.ComponentId);
            var gameObjectGuid = Guid.Parse(id.ParentId.GameObjectId);
            
            if (transformGuid == Guid.Empty)
                return null;

            var replayTransformInstanceId = GetReplayInstanceId(transformGuid);
            var replayGameObjectInstanceId = GetReplayInstanceId(gameObjectGuid);

            if (replayTransformInstanceId.HasValue)
            {
                var t = _transformsByInstanceId.GetValueOrDefault(replayTransformInstanceId.Value) as RectTransform;

                if (t != null)
                {
                    if (!replayGameObjectInstanceId.HasValue)
                    {
                        _gameObjectsByInstanceId[t.gameObject.GetInstanceID()] = t.gameObject;
                        TryAddIdentifierCorrespondence(gameObjectGuid, t.gameObject.GetInstanceID());
                    }

                    return t;
                }
            }

            if (replayGameObjectInstanceId.HasValue)
            {
                var go = _gameObjectsByInstanceId.GetValueOrDefault(replayGameObjectInstanceId.Value);

                if (go != null)
                {
                    if (go.transform is not RectTransform rectTransform)
                    {
                        var prevLocalPosition = go.transform.localPosition;
                        var prevLocalRotation = go.transform.localRotation;
                        var prevLocalScale = go.transform.localScale;
                        rectTransform = go.AddComponent<RectTransform>();
                        rectTransform.localPosition = prevLocalPosition;
                        rectTransform.localRotation = prevLocalRotation;
                        rectTransform.localScale = prevLocalScale;
                    }

                    _transformsByInstanceId[rectTransform.GetInstanceID()] = rectTransform;
                    TryAddIdentifierCorrespondence(transformGuid, rectTransform.GetInstanceID());
                    return rectTransform;
                }
            }
            
            var newGameObject = new GameObject();
            var newTransform = newGameObject.AddComponent<RectTransform>();
            _gameObjectsByInstanceId[newGameObject.GetInstanceID()] = newGameObject;
            _transformsByInstanceId[newTransform.GetInstanceID()] = newTransform;
            TryAddIdentifierCorrespondence(transformGuid, newTransform.GetInstanceID());
            TryAddIdentifierCorrespondence(gameObjectGuid, newGameObject.GetInstanceID());
            updatedHierarchy?.Invoke(new HierarchyCreateGameObjectEvent(id.ParentId));
            return newTransform;
        }

        public T GetOrDefaultAssetByIdentifier<T>(AssetIdentifier id) where T : Object
        {
            return _recordAssetBundle.GetOrDefaultAssetByIdentifier<T>(id);
        }

        public T GetOrCreateComponentByIdentifier<T>(ComponentIdentifier id) where T : Component
        {
            var componentGuid = Guid.Parse(id.ComponentId);
            
            if (componentGuid == Guid.Empty)
                return null;

            var replayComponentInstanceId = GetReplayInstanceId(componentGuid);

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
            }

            if (component == null)
                return null;

            _componentByInstanceId[component.GetInstanceID()] = component;
            TryAddIdentifierCorrespondence(componentGuid, component.GetInstanceID());
            return component;
        }

        public bool TryDestroyGameObjectByIdentifier(GameObjectIdentifier id)
        {
            var gameObjectGuid = Guid.Parse(id.GameObjectId);
            var transformGuid = Guid.Parse(id.TransformId);
            
            var goReplayInstanceId = GetReplayInstanceId(gameObjectGuid);
            var transformReplayInstanceId = GetReplayInstanceId(transformGuid);

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
                updatedHierarchy?.Invoke(new HierarchyDestroyGameObjectEvent(id));
                
                _gameObjectsByInstanceId.Remove(go.GetInstanceID());
                _gameObjectsTagByInstanceId.Remove(go.GetInstanceID());
                _transformsByInstanceId.Remove(go.transform.GetInstanceID());

                var children = go.GetComponentsInChildren<Component>();

                foreach (var child in children)
                {
                    if (child is Transform childTransform)
                    {
                        var childTransformInstanceId = childTransform.GetInstanceID();
                        var childGameObjectInstanceId = childTransform.gameObject.GetInstanceID();
                        _transformsByInstanceId.Remove(childTransformInstanceId);
                        _gameObjectsByInstanceId.Remove(childGameObjectInstanceId);
                        _gameObjectsTagByInstanceId.Remove(childGameObjectInstanceId);
                        RemoveIdentifierCorrespondence(GetRecordIdentifier(childTransformInstanceId));
                        RemoveIdentifierCorrespondence(GetRecordIdentifier(childGameObjectInstanceId));
                    }
                    else
                    {
                        var childComponentInstanceId = child.GetInstanceID();
                        _componentByInstanceId.Remove(childComponentInstanceId);
                        RemoveIdentifierCorrespondence(GetRecordIdentifier(childComponentInstanceId));
                    }
                }
                
                Object.DestroyImmediate(go);
            }

            RemoveIdentifierCorrespondence(gameObjectGuid);
            RemoveIdentifierCorrespondence(transformGuid);
            return true;
        }

        public bool TryDestroyComponentByIdentifier(ComponentIdentifier identifier)
        {
            var guid = Guid.Parse(identifier.ComponentId);
            var componentReplayInstanceId = GetReplayInstanceId(guid);

            if (componentReplayInstanceId.HasValue)
            {
                var component = _componentByInstanceId.GetValueOrDefault(componentReplayInstanceId.Value);

                RemoveIdentifierCorrespondence(guid);
                if (component != null)
                {
                    _componentByInstanceId.Remove(component.GetInstanceID());
                    Object.DestroyImmediate(component);
                }

                return true;
            }

            // Make sure to create a new list to avoid concurrent modification
            var componentsEntriesToRemove = _componentByInstanceId.Where(pair => pair.Value == null).ToList();

            foreach (var entry in componentsEntriesToRemove)
            {
                _componentByInstanceId.Remove(entry.Key);
            }

            return false;
        }

        public Guid GetRecordIdentifier(int replayInstanceId)
        {
            return _invertIdMap.GetValueOrDefault(replayInstanceId, Guid.Empty);
        }

        public int? GetReplayInstanceId(Guid recordIdentifier)
        {
            return _idMap.TryGetValue(recordIdentifier, out var replayInstanceId) ? replayInstanceId : null;
        }

        public bool RemoveIdentifierCorrespondence(Guid recordIdentifier)
        {
            if (recordIdentifier == Guid.Empty)
                return false;

            var result = true;

            if (_idMap.TryGetValue(recordIdentifier, out var instanceId))
            {
                result = _invertIdMap.Remove(instanceId);
            }

            result = result && _idMap.Remove(recordIdentifier);

            return result;
        }

        public bool TryAddIdentifierCorrespondence(Guid recordIdentifier, int replayInstanceId)
        {
            if (recordIdentifier == Guid.Empty)
                return false;
            
            return _idMap.TryAdd(recordIdentifier, replayInstanceId) &&
                   _invertIdMap.TryAdd(replayInstanceId, recordIdentifier);
        }

        public bool TryAddAssetIdentifierCorrespondence(AssetIdentifier recordIdentifier, Object replayAsset)
        {
            if (recordIdentifier == null || replayAsset == null)
                return false;

            var guid = Guid.Parse(recordIdentifier.Id);
            
            return _idMap.TryAdd(guid, replayAsset.GetInstanceID()) &&
                   _invertIdMap.TryAdd(replayAsset.GetInstanceID(), guid);
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