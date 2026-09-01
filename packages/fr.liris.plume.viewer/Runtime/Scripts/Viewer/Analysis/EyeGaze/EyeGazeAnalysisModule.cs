using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.XRITK;
using PLUME.Viewer.Player;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using UnityEngine.Rendering;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    public class EyeGazeAnalysisModule : AnalysisModuleWithResults<EyeGazeAnalysisResult>
    {
        /**
         * Angle in degrees between the optical axis of the eye and the fovea boundary (5° in total when considering both sides).
         * The fovea is a small region in the retina where visual acuity is the highest. We make 2.5° corresponds to 4sigma of
         * the
         */
        public float fovealVisionOpticalAxisAngle = 2.5f;

        /**
         * Number of sigmas included in the projection. We take nSigma=4 to cover 99.99% of values.
         */
        public float nSigmas = 4;

        public float samplesPerSquareMeter = 1000;

        /**
         * Shader used to encode the object's depth and instance id into a RenderTexture.
         */
        public Shader segmentedObjectDepthShader;

        public int segmentedObjectDepthTextureResolution = 512;

        /**
         * The standard normal distribution projection shader
         */
        public ComputeShader projectionShader;

        /**
         * Shader used to display the samples values as an heatmap (from blue to red).
         */
        public Shader samplesHeatmapShader;

        public Shader defaultHeatmapShader;

        public Player.Player player;

        public MeshSampler meshSampler;

        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }

        private PlayerContext _generationContext;

        private Camera _projectionCamera;
        public Transform projectionCameraTransform;

        private Material _sampleHeatmapMaterial;
        private Material _defaultHeatmapMaterial;
        private Material _segmentedObjectDepthMaterial;

        private EyeGazeAnalysisResult _visibleResult;

        private readonly Dictionary<MeshSamplerResult, MaterialPropertyBlock> _cachedMeshSamplerResultPropertyBlocks =
            new();

        private readonly Dictionary<int, MaterialPropertyBlock> _cachedSegmentedObjectsDepthPropertyBlocks = new();

        private readonly Dictionary<int, Mesh> _bakedMeshes = new();
        
        private void Awake()
        {
            SetupProjectionCamera(segmentedObjectDepthTextureResolution, fovealVisionOpticalAxisAngle * 2, 0.3f,
                1000.0f);
            _sampleHeatmapMaterial = new Material(samplesHeatmapShader);
            _defaultHeatmapMaterial = new Material(defaultHeatmapShader);
            _segmentedObjectDepthMaterial = new Material(segmentedObjectDepthShader);
        }

        private void SetupProjectionCamera(int res, float fieldOfView, float nearClipPlane, float farClipPlane)
        {
            var segmentedObjectDepthTexture = new RenderTexture(res, res, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            segmentedObjectDepthTexture.anisoLevel = 0;
            segmentedObjectDepthTexture.useMipMap = false;
            segmentedObjectDepthTexture.Create();

            var eyeGazeMatrix = Matrix4x4.Perspective(fieldOfView, 1.0f, nearClipPlane, farClipPlane);
            _projectionCamera = projectionCameraTransform.gameObject.AddComponent<Camera>();
            _projectionCamera.enabled = false;
            _projectionCamera.orthographic = false;
            _projectionCamera.nearClipPlane = nearClipPlane;
            _projectionCamera.farClipPlane = farClipPlane;
            _projectionCamera.aspect = 1;
            _projectionCamera.projectionMatrix = eyeGazeMatrix;
            _projectionCamera.targetTexture = segmentedObjectDepthTexture;
        }

        // TODO: refactoring needed
        public IEnumerator GenerateHeatmap(Record record, RecordAssetBundle assets,
            EyeGazeAnalysisModuleParameters parameters, Action<EyeGazeAnalysisResult> finishCallback)
        {
            if (parameters.EndTime < parameters.StartTime)
            {
                throw new Exception(
                    $"{nameof(parameters.StartTime)} should be less or equal to {nameof(parameters.EndTime)}.");
            }

            if (player.GetModuleGenerating() != null)
            {
                Debug.LogWarning($"Cannot start generating {GetType().Name}: {player.GetModuleGenerating().GetType().Name} " +
                                 "is already generating. Wait for it to finish or cancel it first.");
                yield break;
            }

            GenerationProgress = 0;
            IsGenerating = true;
            player.SetModuleGenerating(this);

            ComputeBuffer samplesMinValueBuffer;
            ComputeBuffer samplesMaxValueBuffer;
            int projectionKernel;

            try
            {
                samplesMinValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
                samplesMaxValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
                samplesMinValueBuffer.SetData(new[] { uint.MaxValue });
                samplesMaxValueBuffer.SetData(new[] { uint.MinValue });

                projectionKernel = projectionShader.FindKernel("project_std_normal_distribution");

                _generationContext = PlayerContext.CreatePlayerContext(assets);
            }
            catch (Exception e)
            {
                Debug.LogError("[EyeGaze] Setup failed, generation aborted. Could not allocate the value buffers, find " +
                               $"the 'project_std_normal_distribution' kernel in '{projectionShader?.name}', or " +
                               $"create the playback context.\n{e}");
                IsGenerating = false;
                if (player.GetModuleGenerating() == this) player.SetModuleGenerating(null);
                yield break;
            }

            // key: mesh record id, value: sampled mesh containing values
            var meshSamplerResults = new Dictionary<int, MeshSamplerResult>();

            var result = new EyeGazeAnalysisResult(parameters, samplesMinValueBuffer, samplesMaxValueBuffer,
                meshSamplerResults);

            SetVisibleResult(result);
            
            _projectionCamera.projectionMatrix = Matrix4x4.Perspective(
                parameters.FovealVisionOpticalAxisAngle * 2, 1.0f,
                _projectionCamera.nearClipPlane, _projectionCamera.farClipPlane);

            PrepareProjectionShader(samplesMinValueBuffer, samplesMaxValueBuffer, projectionKernel, parameters.NSigmas);

            if (parameters.StartTime > 0)
            {
                var skippedFrames = record.Frames.GetInTimeRange(0, parameters.StartTime - 1u);
                _generationContext.PlayFrames(player.PlayerModules, skippedFrames);
            }
            
            var stopwatch = Stopwatch.StartNew();
            var lastYieldTime = stopwatch.ElapsedMilliseconds;
            
            var frames = record.Frames.GetInTimeRange(parameters.StartTime, parameters.EndTime);
            
            var nFrames = frames.Count;
            
            var inputActionSamples = record.InputActions.GetInTimeRange(parameters.StartTime, parameters.EndTime);
            // Filtered on the value type too: a path pointing at an action of another type leaves the field read below unset.
            var eyeGazePositionSamples =
                inputActionSamples.Where(s => s.Payload.BindingPaths.Contains(parameters.GazePositionBindingPath) &&
                                              s.Payload.ValueCase == InputAction.ValueOneofCase.Vector3);
            var eyeGazeRotationSamples =
                inputActionSamples.Where(s => s.Payload.BindingPaths.Contains(parameters.GazeRotationBindingPath) &&
                                              s.Payload.ValueCase == InputAction.ValueOneofCase.Quaternion);
            
            Debug.Log($"[EyeGaze] {inputActionSamples.Count()} input action samples in " +
                      $"[{parameters.StartTime}, {parameters.EndTime}]; " +
                      $"{eyeGazePositionSamples.Count()} match position binding '{parameters.GazePositionBindingPath}', " +
                      $"{eyeGazeRotationSamples.Count()} match rotation binding '{parameters.GazeRotationBindingPath}'");

            if (!eyeGazePositionSamples.Any())
                Debug.LogWarning("[EyeGaze] No gaze position samples. " +
                                 EyeGazeDiagnostics.DescribeBindingMismatch(inputActionSamples,
                                     parameters.GazePositionBindingPath, InputAction.ValueOneofCase.Vector3));

            if (!eyeGazeRotationSamples.Any())
                Debug.LogWarning("[EyeGaze] No gaze rotation samples. " +
                                 EyeGazeDiagnostics.DescribeBindingMismatch(inputActionSamples,
                                     parameters.GazeRotationBindingPath, InputAction.ValueOneofCase.Quaternion));

            var cameraNeverFound = true;
            var receiversNeverResolved = new HashSet<Guid>(parameters.ReceiversIdentifiers);
            var unresolvedChildLookups = 0;

            for (var frameIdx = 0; frameIdx < nFrames; ++frameIdx)
            {
                var frame = frames[frameIdx];

                var time = stopwatch.ElapsedMilliseconds;

                // Yield every 33ms (~30fps) to avoid freezing the game
                if (time - lastYieldTime > 33)
                {
                    lastYieldTime = time;
                    yield return null;
                }

                _generationContext.PlayFrame(player.PlayerModules, frame);

                var replayCameraId = _generationContext.GetReplayInstanceId(parameters.XrCameraIdentifier);
                var replayProjectionReceiversIds = new List<int>();

                foreach (var receiversIdentifier in parameters.ReceiversIdentifiers)
                {
                    var replayId = _generationContext.GetReplayInstanceId(receiversIdentifier);
                    if (!replayId.HasValue) continue;

                    receiversNeverResolved.Remove(receiversIdentifier);

                    if (!replayProjectionReceiversIds.Contains(replayId.Value))
                        replayProjectionReceiversIds.Add(replayId.Value);

                    if (!parameters.IncludeReceiversChildren) continue;

                    var go = _generationContext.FindGameObjectByInstanceId(replayId.Value);

                    // Identifiers of transforms, components and assets share the same map, so the id may not resolve
                    // to a GameObject.
                    if (go == null)
                    {
                        unresolvedChildLookups++;
                        continue;
                    }

                    foreach (var goInstanceId in go.GetComponentsInChildren<Renderer>()
                                 .Select(r => r.gameObject.GetInstanceID()))
                    {
                        if (!replayProjectionReceiversIds.Contains(goInstanceId))
                            replayProjectionReceiversIds.Add(goInstanceId);
                    }
                }

                if (replayCameraId.HasValue)
                {
                    cameraNeverFound = false;
                    
                    if (replayProjectionReceiversIds.Count > 0)
                    {
                        var xrCamera = _generationContext.FindGameObjectByInstanceId(replayCameraId.Value);

                        if (xrCamera != null)
                        {
                            var projectionReceiversGameObjects = replayProjectionReceiversIds
                                .Select(replayId => _generationContext.FindGameObjectByInstanceId(replayId))
                                .Where(t => t != null)
                                .Select(t => t.gameObject)
                                .ToArray();

                            if (projectionReceiversGameObjects.Length > 0)
                            {
                                Vector3? eyeGazePosition;
                                Quaternion? eyeGazeRotation;

                                var eyeGazePositionIdx =
                                    eyeGazePositionSamples.FirstIndexBeforeTimestamp(frame.Timestamp);
                                var eyeGazeRotationIdx =
                                    eyeGazeRotationSamples.FirstIndexBeforeTimestamp(frame.Timestamp);

                                if (eyeGazePositionIdx >= 0)
                                    eyeGazePosition = eyeGazePositionSamples[eyeGazePositionIdx].Payload.Vector3
                                        .ToEngineType();
                                else
                                    eyeGazePosition = null;

                                if (eyeGazeRotationIdx >= 0)
                                    eyeGazeRotation = eyeGazeRotationSamples[eyeGazeRotationIdx].Payload.Quaternion
                                        .ToEngineType();
                                else
                                    eyeGazeRotation = null;

                                ProjectCurrentEyeGaze(_generationContext, xrCamera,
                                    parameters.CoordinateSystem,
                                    eyeGazePosition, eyeGazeRotation,
                                    projectionReceiversGameObjects,
                                    meshSamplerResults, projectionKernel,
                                    parameters.SamplesPerSquareMeter);
                            }
                        }
                    }

                    GenerationProgress = (float)frameIdx / nFrames;
                }
            }

            GenerationProgress = 1;

            PlayerContext.Destroy(_generationContext);
            _generationContext = null;

            DisposeBakedMeshes();

            PlayerContext.Activate(player.GetMainPlayerContext());
            IsGenerating = false;

            if (player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);

            if (cameraNeverFound)
                Debug.LogWarning($"[EyeGaze] The XR camera {parameters.XrCameraIdentifier} never appeared in " +
                                 $"[{parameters.StartTime}, {parameters.EndTime}] over {nFrames} frames, so nothing " +
                                 "was projected. Check the XR camera identifier and the time range.");

            if (receiversNeverResolved.Count > 0)
                Debug.LogWarning($"[EyeGaze] {receiversNeverResolved.Count} of " +
                                 $"{parameters.ReceiversIdentifiers.Length} projection receivers never appeared in " +
                                 $"[{parameters.StartTime}, {parameters.EndTime}] and received no gaze: " +
                                 string.Join(", ", receiversNeverResolved));

            if (unresolvedChildLookups > 0)
                Debug.LogWarning($"[EyeGaze] {unresolvedChildLookups} receiver lookup(s) resolved to something that " +
                                 "is not a GameObject, so their children were skipped. The receiver identifiers are " +
                                 "probably transform, component or asset identifiers rather than GameObject ones.");

            Debug.Log($"[EyeGaze] Generated {meshSamplerResults.Count} sampled mesh(es) over {nFrames} frames in " +
                      $"{stopwatch.ElapsedMilliseconds} ms.");

            finishCallback(result);
        }

        public void CancelGenerate()
        {
            if (_generationContext != null)
            {
                PlayerContext.Destroy(_generationContext);
                _generationContext = null;
            }

            DisposeBakedMeshes();

            PlayerContext.Activate(player.GetMainPlayerContext());
            IsGenerating = false;

            if (player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);
        }

        private void ProjectCurrentEyeGaze(
            PlayerContext ctx,
            GameObject xrCamera,
            EyeGazeCoordinateSystem coordinateSystem,
            Vector3? eyeGazePosition,
            Quaternion? eyeGazeRotation,
            GameObject[] projectionReceiversGameObjects,
            IDictionary<int, MeshSamplerResult> meshSamplerResults, int projectionKernel,
            float samplesPerSquareMeter)
        {
            switch (coordinateSystem)
            {
                case EyeGazeCoordinateSystem.TrackingSpace:
                {
                    var offsetTransform = projectionCameraTransform.parent;

                    var xrCameraOffset = xrCamera.transform.parent;

                    if (xrCameraOffset != null)
                    {
                        offsetTransform.position = xrCameraOffset.position;
                        offsetTransform.rotation = xrCameraOffset.rotation;

                        if (eyeGazeRotation != null && eyeGazePosition != null)
                        {
                            var p = eyeGazePosition.Value;
                            var q = eyeGazeRotation.Value;
                            var rotation = new Quaternion(q.x, q.y, q.z, q.w);
                            offsetTransform.position = xrCameraOffset.position;
                            offsetTransform.rotation = xrCameraOffset.rotation;
                            projectionCameraTransform.localPosition = p;
                            projectionCameraTransform.localRotation = rotation;
                        }
                    }

                    break;
                }
                case EyeGazeCoordinateSystem.World:
                {
                    var offsetTransform = projectionCameraTransform.parent;

                    var xrCameraOffset = xrCamera.transform.parent;

                    if (xrCameraOffset != null)
                    {
                        offsetTransform.position = xrCameraOffset.position;
                        offsetTransform.rotation = xrCameraOffset.rotation;

                        if (eyeGazeRotation != null && eyeGazePosition != null)
                        {
                            var p = eyeGazePosition.Value;
                            var q = eyeGazeRotation.Value;
                            var rotation = new Quaternion(q.x, q.y, q.z, q.w);
                            offsetTransform.position = xrCameraOffset.position;
                            offsetTransform.rotation = xrCameraOffset.rotation;
                            projectionCameraTransform.localPosition = p;
                            projectionCameraTransform.localRotation = rotation;
                        }
                    }

                    break;
                }
                case EyeGazeCoordinateSystem.Camera:
                {
                    var offsetTransform = projectionCameraTransform.parent;

                    offsetTransform.position = xrCamera.transform.position;
                    offsetTransform.rotation = xrCamera.transform.rotation;
                    projectionCameraTransform.localPosition = Vector3.zero;
                    projectionCameraTransform.localRotation = Quaternion.identity;

                    if (eyeGazeRotation != null && eyeGazePosition != null)
                    {
                        var p = eyeGazePosition.Value;
                        var q = eyeGazeRotation.Value;
                        var rotation = new Quaternion(q.x, -q.y, -q.z, -q.w);
                        // Invert Z orientation, this could probably be done by modifying the quaternion and its conjugate directly
                        var dir = Quaternion.LookRotation(rotation * -Vector3.forward);
                        projectionCameraTransform.localPosition = p;
                        projectionCameraTransform.localRotation = dir;
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(coordinateSystem), coordinateSystem, null);
            }

            var wasRendererEnabled = new Dictionary<Renderer, bool>();

            // Render object depth with an extra channel containing their instance ID
            ApplySegmentedObjectsDepthMaterials(projectionReceiversGameObjects);
            // Only render projection receivers
            foreach (var go in ctx.GetAllGameObjects())
            {
                if (!go.TryGetComponent<Renderer>(out var goRenderer)) continue;
                wasRendererEnabled.Add(goRenderer, goRenderer.enabled);
                goRenderer.enabled = goRenderer.enabled && projectionReceiversGameObjects.Contains(go);
            }

            _projectionCamera.Render();

            foreach (var go in ctx.GetAllGameObjects())
            {
                if (!go.TryGetComponent<Renderer>(out var goRenderer)) continue;
                goRenderer.enabled = wasRendererEnabled[goRenderer];
            }

            var planes = GeometryUtility.CalculateFrustumPlanes(_projectionCamera);
            projectionShader.SetMatrix("view_mtx", _projectionCamera.worldToCameraMatrix);

            foreach (var go in projectionReceiversGameObjects)
            {
                var hasRenderer = go.TryGetComponent<Renderer>(out var r);

                if (hasRenderer)
                {
                    var insideFrustum = GeometryUtility.TestPlanesAABB(planes, r.bounds);

                    // Only consider objects inside the view frustum of the cropped camera
                    if (!insideFrustum)
                        continue;

                    Mesh mesh = null;
                    SkinnedMeshRenderer smr = null;

                    if (go.TryGetComponent<MeshFilter>(out var meshFilter))
                    {
                        mesh = meshFilter.sharedMesh;
                    }
                    else if (go.TryGetComponent<SkinnedMeshRenderer>(out smr))
                    {
                        // Topology/resolution come from the rest pose; deformed positions are bound below.
                        mesh = smr.sharedMesh;
                    }

                    if (mesh == null || mesh.vertexBufferCount == 0)
                        continue;

                    var meshSamplerResult =
                        GetOrCreateMeshSamplerResult(ctx, go, mesh, meshSamplerResults, samplesPerSquareMeter);

                    if (meshSamplerResult == null)
                        continue;

                    // Default (static MeshFilter): rest-pose buffer, model_mtx carries T/R/S.
                    var vertexBuffer = meshSamplerResult.VertexBuffer;
                    var vertexBufferStride = meshSamplerResult.VertexBufferStride;
                    var vertexBufferPositionOffset = meshSamplerResult.VertexBufferPositionOffset;
                    GraphicsBuffer bakedVertexBuffer = null;

                    // Skinned: bake the current pose so projection uses deformed positions.
                    // useScale:false keeps vertices unscaled so localToWorldMatrix applies scale exactly once.
                    if (smr != null)
                    {
                        var baked = GetOrCreateBakedMesh(go.GetInstanceID());
                        smr.BakeMesh(baked, false);
                        bakedVertexBuffer = baked.GetVertexBuffer(0);
                        vertexBuffer = bakedVertexBuffer;
                        vertexBufferStride = bakedVertexBuffer.stride;
                        vertexBufferPositionOffset = baked.GetVertexAttributeOffset(VertexAttribute.Position);
                    }

                    projectionShader.SetInt("object_instance_id", go.GetInstanceID());
                    projectionShader.SetMatrix("model_mtx", r.localToWorldMatrix);
                    projectionShader.SetInt("n_triangles", (int)meshSamplerResult.NTriangles);
                    projectionShader.SetBuffer(projectionKernel, "index_buffer",
                        meshSamplerResult.IndexBuffer);
                    projectionShader.SetInt("index_buffer_stride", meshSamplerResult.IndexBufferStride);
                    projectionShader.SetBuffer(projectionKernel, "vertex_buffer", vertexBuffer);
                    projectionShader.SetInt("vertex_buffer_stride", vertexBufferStride);
                    projectionShader.SetInt("vertex_buffer_position_offset", vertexBufferPositionOffset);
                    projectionShader.SetBuffer(projectionKernel, "triangles_resolution_buffer",
                        meshSamplerResult.TrianglesResolutionBuffer);
                    projectionShader.SetBuffer(projectionKernel, "triangles_samples_index_offset_buffer",
                        meshSamplerResult.TrianglesSamplesIndexOffsetBuffer);
                    projectionShader.SetBuffer(projectionKernel, "samples_value_buffer",
                        meshSamplerResult.SampleValuesBuffer);

                    projectionShader.GetKernelThreadGroupSizes(projectionKernel, out var threadGroupSizeX,
                        out var threadGroupSizeY, out _);
                    var totalNumberOfGroupsNeededX =
                        Mathf.CeilToInt(meshSamplerResult.NTriangles / (float)threadGroupSizeX);
                    var totalNumberOfGroupsNeededY =
                        Mathf.CeilToInt(meshSamplerResult.NSamplesMaxPerTriangle / (float)threadGroupSizeY);
                    projectionShader.SplitDispatch(projectionKernel, totalNumberOfGroupsNeededX,
                        totalNumberOfGroupsNeededY);

                    bakedVertexBuffer?.Release();
                }
            }
        }

        private Mesh GetOrCreateBakedMesh(int instanceId)
        {
            if (_bakedMeshes.TryGetValue(instanceId, out var mesh))
                return mesh;

            var newMesh = new Mesh { name = "EyeGazeBakedMesh_" + instanceId };
            newMesh.MarkDynamic();
            newMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _bakedMeshes.Add(instanceId, newMesh);
            return newMesh;
        }

        private void DisposeBakedMeshes()
        {
            foreach (var mesh in _bakedMeshes.Values)
                if (mesh != null) Destroy(mesh);
            _bakedMeshes.Clear();
        }

        private MeshSamplerResult GetOrCreateMeshSamplerResult(PlayerContext ctx, GameObject go, Mesh mesh,
            IDictionary<int, MeshSamplerResult> meshSamplerResults, float samplesPerSquareMeter)
        {
            var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());
            var meshIdentifier = ctx.GetRecordIdentifier(mesh.GetInstanceID());

            if (gameObjectIdentifier == Guid.Empty || meshIdentifier == Guid.Empty)
                return null;

            // Two GameObjects might have the same sharedMesh. We add the gameObjectIdentifier as a discriminator.
            var meshSamplerResultHash = HashCode.Combine(gameObjectIdentifier, meshIdentifier);

            if (mesh == null || mesh.vertexBufferCount == 0)
                return null;

            if (meshSamplerResults.TryGetValue(meshSamplerResultHash, out var result))
                return result;

            var meshSamplerResult = meshSampler.Sample(mesh, samplesPerSquareMeter, go.transform.lossyScale);
            meshSamplerResult.Name = go.name + "_" + (uint)meshSamplerResultHash;
            meshSamplerResults.Add(meshSamplerResultHash, meshSamplerResult);
            return meshSamplerResult;
        }

        private void PrepareProjectionShader(ComputeBuffer samplesMinValueBuffer, ComputeBuffer samplesMaxValueBuffer,
            int projectionKernel, float nSigmas)
        {
            projectionShader.SetFloat("n_sigmas", nSigmas);
            projectionShader.SetTexture(projectionKernel, "segmented_object_depth_texture",
                _projectionCamera.targetTexture);
            projectionShader.SetMatrix("projection_mtx", _projectionCamera.projectionMatrix);
            projectionShader.SetBool("is_projection_orthographic", _projectionCamera.orthographic);
            projectionShader.SetBuffer(projectionKernel, "samples_min_value", samplesMinValueBuffer);
            projectionShader.SetBuffer(projectionKernel, "samples_max_value", samplesMaxValueBuffer);
        }

        private void LateUpdate()
        {
            var activeContext = PlayerContext.GetActiveContext();

            if (activeContext == null)
                return;

            if (_visibleResult != null)
            {
                ApplyHeatmapMaterials(activeContext);
            }
        }

        private void RestoreRecordMaterials(PlayerContext ctx)
        {
            var gameObjects = ctx.GetAllGameObjects();

            foreach (var go in gameObjects)
            {
                if (go.TryGetComponent<Graphic>(out var graphic))
                {
                    graphic.enabled = true;
                }
                
                if (!go.TryGetComponent<Renderer>(out var goRenderer))
                    continue;
                goRenderer.SetSharedMaterials(new List<Material>());
            }

            var frameSamples = player.Record.Frames.GetInTimeRange(0, player.GetCurrentPlayTimeInNanoseconds());

            foreach (var frameSample in frameSamples)
            {
                foreach (var sample in frameSample.Data)
                {
                    if (sample.Payload is TerrainUpdate)
                    {
                        foreach (var playerModule in player.PlayerModules)
                        {
                            playerModule.PlaySample(ctx, sample);
                        }
                    }
                    
                    if (sample.Payload is RendererUpdate)
                    {
                        foreach (var playerModule in player.PlayerModules)
                        {
                            playerModule.PlaySample(ctx, sample);
                        }
                    }
                }
            }
        }

        private void ApplySegmentedObjectsDepthMaterials(IEnumerable<GameObject> projectionReceivers)
        {
            foreach (var go in projectionReceivers)
            {
                if (!go.TryGetComponent<Renderer>(out var goRenderer))
                {
                    continue;
                }

                var nSharedMaterials = goRenderer.sharedMaterials.Length;
                goRenderer.sharedMaterials =
                    Enumerable.Repeat(_segmentedObjectDepthMaterial, nSharedMaterials).ToArray();
                var propertyBlock = GetOrCreateSegmentedObjectsDepthPropertyBlock(go);
                goRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private MaterialPropertyBlock GetOrCreateSegmentedObjectsDepthPropertyBlock(GameObject go)
        {
            if (_cachedSegmentedObjectsDepthPropertyBlocks.TryGetValue(go.GetInstanceID(), out var propertyBlock))
            {
                return propertyBlock;
            }

            var objectInstanceID = Shader.PropertyToID("object_instance_id");
            var newPropertyBlock = new MaterialPropertyBlock();
            newPropertyBlock.SetInt(objectInstanceID, go.GetInstanceID());
            _cachedSegmentedObjectsDepthPropertyBlocks.Add(go.GetInstanceID(), newPropertyBlock);
            return newPropertyBlock;
        }

        private void ApplyHeatmapMaterials(PlayerContext ctx)
        {
            var gameObjects = ctx.GetAllGameObjects();

            foreach (var go in gameObjects)
            {
                if (go.TryGetComponent<Terrain>(out var terrain))
                {
                    // Disable trees and grass
                    terrain.treeDistance = 0;
                    terrain.detailObjectDensity = 0;
                    terrain.materialTemplate = _defaultHeatmapMaterial;
                }
                
                if (go.TryGetComponent<Graphic>(out var graphic))
                {
                    graphic.enabled = false;
                }
                
                if (!go.TryGetComponent<Renderer>(out var goRenderer))
                {
                    continue;
                }

                var nSharedMaterials = goRenderer.sharedMaterials.Length;
                goRenderer.sharedMaterials = Enumerable.Repeat(_defaultHeatmapMaterial, nSharedMaterials).ToArray();
                goRenderer.SetPropertyBlock(null);

                Mesh mesh = null;

                if (go.TryGetComponent<MeshFilter>(out var meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                }
                else if (go.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
                {
                    mesh = skinnedMeshRenderer.sharedMesh;
                }

                if (mesh == null || mesh.vertexCount == 0)
                    continue;

                var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());
                var meshRecordIdentifier = ctx.GetRecordIdentifier(mesh.GetInstanceID());

                if (gameObjectIdentifier == Guid.Empty)
                    continue;
                if (meshRecordIdentifier == Guid.Empty)
                    continue;

                var meshSamplerResultHash = HashCode.Combine(gameObjectIdentifier, meshRecordIdentifier);

                var hasMeshSamplerResult =
                    _visibleResult.SamplerResults.TryGetValue(meshSamplerResultHash, out var meshSamplerResult);

                if (!hasMeshSamplerResult)
                    continue;

                goRenderer.sharedMaterials = Enumerable.Repeat(_sampleHeatmapMaterial, nSharedMaterials).ToArray();
                var propertyBlock = GetOrCreateMeshSamplerResultPropertyBlock(meshSamplerResult);
                goRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private MaterialPropertyBlock GetOrCreateMeshSamplerResultPropertyBlock(MeshSamplerResult meshSamplerResult)
        {
            if (_cachedMeshSamplerResultPropertyBlocks.TryGetValue(meshSamplerResult, out var propertyBlock))
            {
                return propertyBlock;
            }

            var trianglesResolutionBuffer = Shader.PropertyToID("triangles_resolution_buffer");
            var trianglesSamplesIndexOffsetBuffer = Shader.PropertyToID("triangles_samples_index_offset_buffer");
            var samplesValueBuffer = Shader.PropertyToID("samples_value_buffer");
            var samplesMinValue = Shader.PropertyToID("samples_min_value");
            var samplesMaxValue = Shader.PropertyToID("samples_max_value");

            var newPropertyBlock = new MaterialPropertyBlock();
            newPropertyBlock.SetBuffer(samplesMinValue, _visibleResult.MinValueBuffer);
            newPropertyBlock.SetBuffer(samplesMaxValue, _visibleResult.MaxValueBuffer);
            newPropertyBlock.SetBuffer(trianglesResolutionBuffer, meshSamplerResult.TrianglesResolutionBuffer);
            newPropertyBlock.SetBuffer(trianglesSamplesIndexOffsetBuffer,
                meshSamplerResult.TrianglesSamplesIndexOffsetBuffer);
            newPropertyBlock.SetBuffer(samplesValueBuffer, meshSamplerResult.SampleValuesBuffer);
            _cachedMeshSamplerResultPropertyBlocks.Add(meshSamplerResult, newPropertyBlock);
            return newPropertyBlock;
        }

        private Vector3 SampleIndexToBarycentricWeights(uint sampleIdx, uint triangleResolution)
        {
            var row = (uint)Math.Ceiling((-3 + Math.Sqrt(8.0 * sampleIdx + 9.0)) / 2.0);
            var col = sampleIdx - row * (row + 1) / 2u;

            var wi = col / (float)triangleResolution;
            var wj = 1 - row / (float)triangleResolution;
            var wk = 1 - (wi + wj);

            return new Vector3(wi, wj, wk);
        }

        public void ExportResult(EyeGazeAnalysisResult result)
        {
            var resultIdx = GetResultIndex(result);
            var outputDir = $"Outputs/{player.Record.ToSafeString()}/Analysis/EyeGazeHeatmaps/{resultIdx}";

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            foreach (var (hash, samplerResult) in result.SamplerResults)
            {
                string filePath;

                if (samplerResult.Name != null)
                {
                    filePath = $"{outputDir}/heatmap_{samplerResult.Name}.ply";
                }
                else
                {
                    filePath = $"{outputDir}/heatmap_{(uint)hash}.ply";
                }

                var w = File.CreateText(filePath);

                // Write PLY file header for a point cloud
                w.WriteLine("ply");
                w.WriteLine("format ascii 1.0");
                w.WriteLine("element vertex " + samplerResult.NSamples);
                w.WriteLine("property float x");
                w.WriteLine("property float y");
                w.WriteLine("property float z");
                w.WriteLine("property float value");
                w.WriteLine("end_header");

                // Get the values from the GPU to the CPU
                var samplesValueArr = new float[samplerResult.NSamples];
                samplerResult.SampleValuesBuffer.GetData(samplesValueArr);

                // Extract unity VertexBuffer data
                var verticesArr = new byte[samplerResult.VertexBuffer.count * samplerResult.VertexBuffer.stride];
                samplerResult.VertexBuffer.GetData(verticesArr);
                // From the vertex buffer data, extract all vertices positions
                var verticesPositionsArr = new Vector3[samplerResult.VertexBuffer.count];
                for (var i = 0; i < samplerResult.VertexBuffer.count; ++i)
                {
                    var vertex = new Vector3(
                        BitConverter.ToSingle(verticesArr,
                            i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset),
                        BitConverter.ToSingle(verticesArr,
                            i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset + 4),
                        BitConverter.ToSingle(verticesArr,
                            i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset + 8)
                    );
                    verticesPositionsArr[i] = vertex;
                }

                var indicesArr = new ushort[samplerResult.IndexBuffer.count];
                samplerResult.IndexBuffer.GetData(indicesArr);

                // For each triangle, get the triangle resolution
                var trianglesResolutionArr = new uint[samplerResult.NTriangles];
                samplerResult.TrianglesResolutionBuffer.GetData(trianglesResolutionArr);

                var trianglesSamplesIndexOffsetArr = new uint[samplerResult.NTriangles];
                samplerResult.TrianglesSamplesIndexOffsetBuffer.GetData(trianglesSamplesIndexOffsetArr);

                for (var triangleIdx = 0; triangleIdx < samplerResult.NTriangles; ++triangleIdx)
                {
                    var triangleResolution = trianglesResolutionArr[triangleIdx];
                    var sampleIndexOffset = trianglesSamplesIndexOffsetArr[triangleIdx];

                    // nth triangle formula
                    var nSamples = (triangleResolution + 1) * (triangleResolution + 2) / 2u;

                    for (var sampleIdx = 0u; sampleIdx < nSamples; sampleIdx++)
                    {
                        var barycentricWeights = SampleIndexToBarycentricWeights(sampleIdx, triangleResolution);

                        var v0 = verticesPositionsArr[indicesArr[triangleIdx * 3]];
                        var v1 = verticesPositionsArr[indicesArr[triangleIdx * 3 + 1]];
                        var v2 = verticesPositionsArr[indicesArr[triangleIdx * 3 + 2]];

                        var samplePos =
                            v0 * barycentricWeights.x +
                            v1 * barycentricWeights.y +
                            v2 * barycentricWeights.z;

                        var sampleValue = samplesValueArr[sampleIndexOffset + sampleIdx];

                        // Write the sample to the PLY file
                        w.WriteLine($"{samplePos.x} {samplePos.y} {samplePos.z} {sampleValue}");
                    }
                }

                w.Close();
                Debug.Log("PLY file exported to " + Path.GetFullPath(filePath));
            }
        }

        public override void RemoveResult(EyeGazeAnalysisResult result)
        {
            base.RemoveResult(result);

            if (result == _visibleResult)
            {
                foreach (var meshSamplerResult in result.SamplerResults.Values)
                {
                    _cachedMeshSamplerResultPropertyBlocks.Remove(meshSamplerResult);
                }

                SetVisibleResult(null);
            }
        }

        public void SetVisibleResult(EyeGazeAnalysisResult result)
        {
            var prevVisibleResult = _visibleResult;

            _visibleResult = result;

            // Switch to the heatmap-friendly pipeline (URP/built-in) while a result is generating/visible, restore on hide.
            if (result != null && prevVisibleResult == null)
            {
                if (HeatmapPipelineSwitcher.Instance != null)
                    HeatmapPipelineSwitcher.Instance.Acquire();
            }
            else if (result == null && prevVisibleResult != null)
            {
                RestoreRecordMaterials(player.GetMainPlayerContext());

                if (HeatmapPipelineSwitcher.Instance != null)
                    HeatmapPipelineSwitcher.Instance.Release();
            }
        }

        private void OnDestroy()
        {
            DisposeBakedMeshes();

            foreach (var result in GetResults())
            {
                result.Dispose();
            }

            // The projection camera lives on another GameObject, which may already have been destroyed since teardown
            // order across GameObjects is undefined.
            if (_projectionCamera == null || _projectionCamera.targetTexture == null)
                return;

            _projectionCamera.targetTexture.Release();
            _projectionCamera.targetTexture = null;
        }

        public EyeGazeAnalysisResult GetVisibleResult()
        {
            return _visibleResult;
        }

        public override void Dispose()
        {
            foreach (var result in GetResults())
            {
                result.Dispose();
            }
        }
    }
}