using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using PLUME;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.XRITK;
using PLUME.Viewer.Player;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace PLUME.Viewer.Analysis.EyeGaze
{
    /// <summary>
    /// Simplified eye-gaze heatmap that accumulates data at mesh vertices rather than at
    /// sub-triangle sample points. For each gaze frame the projection camera is pointed along
    /// the gaze direction; every receiver triangle whose screen-space footprint contains the
    /// camera centre (NDC 0, 0) and whose depth matches the rendered depth texture receives a
    /// unit weight that is split across its three vertices proportionally to their barycentric
    /// coordinates relative to the gaze hit point.
    /// </summary>
    public class EyeGazeVertexHeatmapAnalysisModule : AnalysisModuleWithResults<EyeGazeVertexHeatmapResult>
    {
        public float fovealVisionOpticalAxisAngle = 2.5f;

        public Shader segmentedObjectDepthShader;
        public int segmentedObjectDepthTextureResolution = 512;

        public ComputeShader projectionShader;

        public Shader vertexHeatmapShader;
        public Shader defaultHeatmapShader;

        public Player.Player player;

        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }

        private PlayerContext _generationContext;

        private Camera _projectionCamera;
        public Transform projectionCameraTransform;

        private Material _vertexHeatmapMaterial;
        private Material _defaultHeatmapMaterial;
        private Material _segmentedObjectDepthMaterial;

        private EyeGazeVertexHeatmapResult _visibleResult;

        private readonly Dictionary<EyeGazeVertexHeatmapMeshResult, MaterialPropertyBlock> _cachedPropertyBlocks =
            new();

        private readonly Dictionary<int, MaterialPropertyBlock> _cachedDepthPropertyBlocks = new();

        private readonly Dictionary<int, Mesh> _bakedMeshes = new();

        private void Awake()
        {
            SetupProjectionCamera(segmentedObjectDepthTextureResolution, fovealVisionOpticalAxisAngle * 2, 0.3f, 1000.0f);
            _vertexHeatmapMaterial = new Material(vertexHeatmapShader);
            _defaultHeatmapMaterial = new Material(defaultHeatmapShader);
            _segmentedObjectDepthMaterial = new Material(segmentedObjectDepthShader);
        }

        private void SetupProjectionCamera(int res, float fieldOfView, float nearClipPlane, float farClipPlane)
        {
            var depthTexture = new RenderTexture(res, res, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            depthTexture.anisoLevel = 0;
            depthTexture.useMipMap = false;
            depthTexture.Create();

            _projectionCamera = projectionCameraTransform.gameObject.AddComponent<Camera>();
            _projectionCamera.enabled = false;
            _projectionCamera.orthographic = false;
            _projectionCamera.nearClipPlane = nearClipPlane;
            _projectionCamera.farClipPlane = farClipPlane;
            _projectionCamera.aspect = 1;
            _projectionCamera.projectionMatrix = Matrix4x4.Perspective(fieldOfView, 1.0f, nearClipPlane, farClipPlane);
            _projectionCamera.targetTexture = depthTexture;
        }

        public IEnumerator GenerateHeatmap(
            Record record,
            RecordAssetBundle assets,
            EyeGazeAnalysisModuleParameters parameters,
            Action<EyeGazeVertexHeatmapResult> finishCallback)
        {
            if (parameters.EndTime < parameters.StartTime)
                throw new Exception($"{nameof(parameters.StartTime)} must be <= {nameof(parameters.EndTime)}.");

            if (player.GetModuleGenerating() != null)
            {
                Debug.LogWarning("Another module is already generating");
                yield break;
            }

            GenerationProgress = 0;
            IsGenerating = true;
            player.SetModuleGenerating(this);

            ComputeBuffer maxValueBuffer;
            int projectionKernel;

            try
            {
                maxValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
                maxValueBuffer.SetData(new[] { 0u });

                projectionKernel = projectionShader.FindKernel("project_eye_gaze_to_vertices");
                _generationContext = PlayerContext.CreatePlayerContext(assets);
            }
            catch (Exception e)
            {
                Debug.LogError("[EyeGazeVertex] Setup failed: " + e);
                IsGenerating = false;
                if (player.GetModuleGenerating() == this) player.SetModuleGenerating(null);
                yield break;
            }

            var meshResults = new Dictionary<int, EyeGazeVertexHeatmapMeshResult>();
            var result = new EyeGazeVertexHeatmapResult(parameters, maxValueBuffer, meshResults);

            SetVisibleResult(result);

            _projectionCamera.projectionMatrix = Matrix4x4.Perspective(
                parameters.FovealVisionOpticalAxisAngle * 2, 1.0f,
                _projectionCamera.nearClipPlane, _projectionCamera.farClipPlane);

            projectionShader.SetTexture(projectionKernel, "segmented_object_depth_texture",
                _projectionCamera.targetTexture);
            projectionShader.SetMatrix("projection_mtx", _projectionCamera.projectionMatrix);
            projectionShader.SetBool("is_projection_orthographic", _projectionCamera.orthographic);
            projectionShader.SetBuffer(projectionKernel, "samples_max_value", maxValueBuffer);

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
            var eyeGazePositionSamples =
                inputActionSamples.Where(s => s.Payload.BindingPaths.Contains("<EyeGaze>/pose/position"));
            var eyeGazeRotationSamples =
                inputActionSamples.Where(s => s.Payload.BindingPaths.Contains("<EyeGaze>/pose/rotation"));

            Debug.Log($"[EyeGazeVertex] gaze position samples: {eyeGazePositionSamples.Count()}, " +
                      $"gaze rotation samples: {eyeGazeRotationSamples.Count()}");

            if (!eyeGazePositionSamples.Any() || !eyeGazeRotationSamples.Any())
            {
                var paths = inputActionSamples.SelectMany(s => s.Payload.BindingPaths).Distinct();
                Debug.LogWarning("[EyeGazeVertex] No gaze samples. Recorded binding paths: "
                                 + string.Join(", ", paths));
            }

            var cameraNeverFound = true;

            for (var frameIdx = 0; frameIdx < nFrames; ++frameIdx)
            {
                var frame = frames[frameIdx];

                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed - lastYieldTime > 33)
                {
                    lastYieldTime = elapsed;
                    yield return null;
                }

                _generationContext.PlayFrame(player.PlayerModules, frame);

                var replayCameraId = _generationContext.GetReplayInstanceId(parameters.XrCameraIdentifier);
                var replayReceiverIds = new List<int>();

                foreach (var receiverId in parameters.ReceiversIdentifiers)
                {
                    var replayId = _generationContext.GetReplayInstanceId(receiverId);
                    if (!replayId.HasValue) continue;

                    if (!replayReceiverIds.Contains(replayId.Value))
                        replayReceiverIds.Add(replayId.Value);

                    if (!parameters.IncludeReceiversChildren) continue;

                    var go = _generationContext.FindGameObjectByInstanceId(replayId.Value);
                    foreach (var instanceId in go.GetComponentsInChildren<Renderer>()
                                 .Select(r => r.gameObject.GetInstanceID()))
                    {
                        if (!replayReceiverIds.Contains(instanceId))
                            replayReceiverIds.Add(instanceId);
                    }
                }

                if (!replayCameraId.HasValue)
                    continue;

                cameraNeverFound = false;

                if (replayReceiverIds.Count == 0)
                    continue;

                var xrCamera = _generationContext.FindGameObjectByInstanceId(replayCameraId.Value);
                if (xrCamera == null)
                    continue;

                var receiverGameObjects = replayReceiverIds
                    .Select(id => _generationContext.FindGameObjectByInstanceId(id))
                    .Where(go => go != null)
                    .ToArray();

                if (receiverGameObjects.Length == 0)
                    continue;

                var posIdx = eyeGazePositionSamples.FirstIndexBeforeTimestamp(frame.Timestamp);
                var rotIdx = eyeGazeRotationSamples.FirstIndexBeforeTimestamp(frame.Timestamp);

                Vector3? gazePosition = posIdx >= 0
                    ? eyeGazePositionSamples[posIdx].Payload.Vector3.ToEngineType()
                    : (Vector3?)null;
                Quaternion? gazeRotation = rotIdx >= 0
                    ? eyeGazeRotationSamples[rotIdx].Payload.Quaternion.ToEngineType()
                    : (Quaternion?)null;

                ProjectGazeFrame(
                    _generationContext, xrCamera,
                    parameters.CoordinateSystem,
                    gazePosition, gazeRotation,
                    receiverGameObjects, meshResults, projectionKernel);

                GenerationProgress = (float)frameIdx / nFrames;
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
                Debug.LogWarning($"[EyeGazeVertex] Camera {parameters.XrCameraIdentifier} not found in time range " +
                                 $"{parameters.StartTime}–{parameters.EndTime}.");

            finishCallback(result);
        }

        private void ProjectGazeFrame(
            PlayerContext ctx,
            GameObject xrCamera,
            EyeGazeCoordinateSystem coordinateSystem,
            Vector3? gazePosition,
            Quaternion? gazeRotation,
            GameObject[] receiverGameObjects,
            IDictionary<int, EyeGazeVertexHeatmapMeshResult> meshResults,
            int projectionKernel)
        {
            // Position and orient the projection camera to match the gaze direction
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

                        if (gazeRotation != null && gazePosition != null)
                        {
                            var q = gazeRotation.Value;
                            projectionCameraTransform.localPosition = gazePosition.Value;
                            projectionCameraTransform.localRotation = new Quaternion(q.x, q.y, q.z, q.w);
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

                        if (gazeRotation != null && gazePosition != null)
                        {
                            var q = gazeRotation.Value;
                            projectionCameraTransform.localPosition = gazePosition.Value;
                            projectionCameraTransform.localRotation = new Quaternion(q.x, q.y, q.z, q.w);
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

                    if (gazeRotation != null && gazePosition != null)
                    {
                        var q = gazeRotation.Value;
                        var rotation = new Quaternion(q.x, -q.y, -q.z, -q.w);
                        var dir = Quaternion.LookRotation(rotation * -Vector3.forward);
                        projectionCameraTransform.localPosition = gazePosition.Value;
                        projectionCameraTransform.localRotation = dir;
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(coordinateSystem), coordinateSystem, null);
            }

            // Render depth + instance ID for all receivers
            ApplyDepthMaterials(receiverGameObjects);

            var wasEnabled = new Dictionary<Renderer, bool>();
            foreach (var go in ctx.GetAllGameObjects())
            {
                if (!go.TryGetComponent<Renderer>(out var r)) continue;
                wasEnabled[r] = r.enabled;
                r.enabled = r.enabled && receiverGameObjects.Contains(go);
            }

            _projectionCamera.Render();

            foreach (var go in ctx.GetAllGameObjects())
            {
                if (!go.TryGetComponent<Renderer>(out var r)) continue;
                r.enabled = wasEnabled[r];
            }

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_projectionCamera);
            projectionShader.SetMatrix("view_mtx", _projectionCamera.worldToCameraMatrix);

            foreach (var go in receiverGameObjects)
            {
                if (!go.TryGetComponent<Renderer>(out var r)) continue;

                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, r.bounds))
                    continue;

                Mesh mesh = null;
                SkinnedMeshRenderer smr = null;

                if (go.TryGetComponent<MeshFilter>(out var meshFilter))
                    mesh = meshFilter.sharedMesh;
                else if (go.TryGetComponent<SkinnedMeshRenderer>(out smr))
                    mesh = smr.sharedMesh;

                if (mesh == null || mesh.vertexBufferCount == 0)
                    continue;

                var meshResult = GetOrCreateMeshResult(ctx, go, mesh, meshResults);
                if (meshResult == null)
                    continue;

                var vertexBuffer = meshResult.VertexBuffer;
                var vertexBufferStride = meshResult.VertexBufferStride;
                var vertexBufferPositionOffset = meshResult.VertexBufferPositionOffset;
                GraphicsBuffer bakedVertexBuffer = null;

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
                projectionShader.SetInt("n_triangles", (int)meshResult.NTriangles);
                projectionShader.SetBuffer(projectionKernel, "index_buffer", meshResult.IndexBuffer);
                projectionShader.SetInt("index_buffer_stride", meshResult.IndexBufferStride);
                projectionShader.SetBuffer(projectionKernel, "vertex_buffer", vertexBuffer);
                projectionShader.SetInt("vertex_buffer_stride", vertexBufferStride);
                projectionShader.SetInt("vertex_buffer_position_offset", vertexBufferPositionOffset);
                projectionShader.SetBuffer(projectionKernel, "vertex_value_buffer", meshResult.VertexValueBuffer);

                projectionShader.GetKernelThreadGroupSizes(projectionKernel, out var threadGroupSizeX, out _, out _);
                var totalGroupsX = Mathf.CeilToInt(meshResult.NTriangles / (float)threadGroupSizeX);
                projectionShader.SplitDispatch(projectionKernel, totalGroupsX, 1);

                bakedVertexBuffer?.Release();
            }
        }

        private EyeGazeVertexHeatmapMeshResult GetOrCreateMeshResult(
            PlayerContext ctx,
            GameObject go,
            Mesh mesh,
            IDictionary<int, EyeGazeVertexHeatmapMeshResult> meshResults)
        {
            var goId = ctx.GetRecordIdentifier(go.GetInstanceID());
            var meshId = ctx.GetRecordIdentifier(mesh.GetInstanceID());

            if (goId == null || meshId == null)
                return null;

            var hash = HashCode.Combine(goId, meshId);

            if (meshResults.TryGetValue(hash, out var existing))
                return existing;

            // Build readable copies of the vertex and index buffers (same approach as MeshSampler)
            var readableCopy = MeshSampler.MakeReadableMeshCopy(mesh);
            readableCopy.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            readableCopy.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            var vertexBuffer = readableCopy.GetVertexBuffer(0);
            var indexBuffer = readableCopy.GetIndexBuffer();

            var nVertices = (uint)vertexBuffer.count;
            var nTriangles = (uint)(indexBuffer.count / 3);

            // Initialize all vertex values to 0 (float 0.0 == uint 0)
            var vertexValueBuffer = new ComputeBuffer((int)nVertices, Marshal.SizeOf(typeof(uint)));
            vertexValueBuffer.SetData(new uint[nVertices]);

            var result = new EyeGazeVertexHeatmapMeshResult(
                nVertices, nTriangles,
                vertexValueBuffer,
                indexBuffer, indexBuffer.stride,
                vertexBuffer, vertexBuffer.stride,
                readableCopy.GetVertexAttributeOffset(VertexAttribute.Position));

            result.Name = go.name + "_" + (uint)hash;
            meshResults.Add(hash, result);
            return result;
        }

        private Mesh GetOrCreateBakedMesh(int instanceId)
        {
            if (_bakedMeshes.TryGetValue(instanceId, out var mesh))
                return mesh;

            var newMesh = new Mesh { name = "EyeGazeVertexBakedMesh_" + instanceId };
            newMesh.MarkDynamic();
            newMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _bakedMeshes[instanceId] = newMesh;
            return newMesh;
        }

        private void DisposeBakedMeshes()
        {
            foreach (var mesh in _bakedMeshes.Values)
                if (mesh != null) Destroy(mesh);
            _bakedMeshes.Clear();
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

        private void LateUpdate()
        {
            var activeContext = PlayerContext.GetActiveContext();
            if (activeContext == null) return;

            if (_visibleResult != null)
                ApplyHeatmapMaterials(activeContext);
        }

        private void ApplyDepthMaterials(IEnumerable<GameObject> receivers)
        {
            foreach (var go in receivers)
            {
                if (!go.TryGetComponent<Renderer>(out var r)) continue;
                var n = r.sharedMaterials.Length;
                r.sharedMaterials = Enumerable.Repeat(_segmentedObjectDepthMaterial, n).ToArray();
                r.SetPropertyBlock(GetOrCreateDepthPropertyBlock(go));
            }
        }

        private MaterialPropertyBlock GetOrCreateDepthPropertyBlock(GameObject go)
        {
            if (_cachedDepthPropertyBlocks.TryGetValue(go.GetInstanceID(), out var pb))
                return pb;

            pb = new MaterialPropertyBlock();
            pb.SetInt(Shader.PropertyToID("object_instance_id"), go.GetInstanceID());
            _cachedDepthPropertyBlocks[go.GetInstanceID()] = pb;
            return pb;
        }

        private void ApplyHeatmapMaterials(PlayerContext ctx)
        {
            foreach (var go in ctx.GetAllGameObjects())
            {
                if (go.TryGetComponent<Terrain>(out var terrain))
                {
                    terrain.treeDistance = 0;
                    terrain.detailObjectDensity = 0;
                    terrain.materialTemplate = _defaultHeatmapMaterial;
                }

                if (go.TryGetComponent<Graphic>(out var graphic))
                    graphic.enabled = false;

                if (!go.TryGetComponent<Renderer>(out var goRenderer)) continue;

                var n = goRenderer.sharedMaterials.Length;
                goRenderer.sharedMaterials = Enumerable.Repeat(_defaultHeatmapMaterial, n).ToArray();
                goRenderer.SetPropertyBlock(null);

                Mesh mesh = null;

                if (go.TryGetComponent<MeshFilter>(out var meshFilter))
                    mesh = meshFilter.sharedMesh;
                else if (go.TryGetComponent<SkinnedMeshRenderer>(out var smr))
                    mesh = smr.sharedMesh;

                if (mesh == null || mesh.vertexCount == 0)
                    continue;

                var goId = ctx.GetRecordIdentifier(go.GetInstanceID());
                var meshId = ctx.GetRecordIdentifier(mesh.GetInstanceID());

                if (goId == null || meshId == null)
                    continue;

                var hash = HashCode.Combine(goId, meshId);

                if (!_visibleResult.MeshResults.TryGetValue(hash, out var meshResult))
                    continue;

                goRenderer.sharedMaterials = Enumerable.Repeat(_vertexHeatmapMaterial, n).ToArray();
                goRenderer.SetPropertyBlock(GetOrCreateHeatmapPropertyBlock(meshResult));
            }
        }

        private MaterialPropertyBlock GetOrCreateHeatmapPropertyBlock(EyeGazeVertexHeatmapMeshResult meshResult)
        {
            if (_cachedPropertyBlocks.TryGetValue(meshResult, out var pb))
                return pb;

            pb = new MaterialPropertyBlock();
            pb.SetBuffer(Shader.PropertyToID("vertex_value_buffer"), meshResult.VertexValueBuffer);
            pb.SetBuffer(Shader.PropertyToID("samples_max_value"), _visibleResult.MaxValueBuffer);
            _cachedPropertyBlocks[meshResult] = pb;
            return pb;
        }

        private void RestoreRecordMaterials(PlayerContext ctx)
        {
            foreach (var go in ctx.GetAllGameObjects())
            {
                if (go.TryGetComponent<Graphic>(out var graphic))
                    graphic.enabled = true;

                if (!go.TryGetComponent<Renderer>(out var r)) continue;
                r.SetSharedMaterials(new List<Material>());
            }

            var frameSamples = player.Record.Frames.GetInTimeRange(0, player.GetCurrentPlayTimeInNanoseconds());
            foreach (var frameSample in frameSamples)
            {
                foreach (var sample in frameSample.Data)
                {
                    if (sample.Payload is TerrainUpdate or RendererUpdate)
                    {
                        foreach (var module in player.PlayerModules)
                            module.PlaySample(ctx, sample);
                    }
                }
            }
        }

        public void SetVisibleResult(EyeGazeVertexHeatmapResult result)
        {
            var prev = _visibleResult;
            _visibleResult = result;

            if (result == null && prev != null)
                RestoreRecordMaterials(player.GetMainPlayerContext());
        }

        public EyeGazeVertexHeatmapResult GetVisibleResult() => _visibleResult;

        public override void RemoveResult(EyeGazeVertexHeatmapResult result)
        {
            base.RemoveResult(result);

            if (result == _visibleResult)
            {
                foreach (var meshResult in result.MeshResults.Values)
                    _cachedPropertyBlocks.Remove(meshResult);

                SetVisibleResult(null);
            }
        }

        private void OnDestroy()
        {
            DisposeBakedMeshes();

            foreach (var result in GetResults())
                result.Dispose();

            _projectionCamera.targetTexture.Release();
            _projectionCamera.targetTexture = null;
        }

        public override void Dispose()
        {
            foreach (var result in GetResults())
                result.Dispose();
        }
    }
}
