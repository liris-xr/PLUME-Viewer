using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace PLUME
{
    public class PositionHeatmapAnalysisModule : AnalysisModuleWithResults<PositionHeatmapAnalysisResult>
    {
        /**
         * Radius that will match the nSigmas.
         * For nSigmas=4 this means that approximately 99.99% of the projected values will land in the radius around the position.
         */
        public float radius = 0.5f;

        /**
         * Number of sigmas included in the projection. We take nSigma=4 to cover 99.99% of values.
         */
        public float nSigmas = 4;

        public float samplesPerSquareMeter = 1000;

        /**
         * Rate in Hz at which the object's position will be projected
         */
        public float projectionSamplingRate = 50;

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

        public Player player;

        public MeshSampler meshSampler;

        public bool IsGenerating { get; private set; }

        private Camera _segmentedObjectDepthCamera;

        private Material _sampleHeatmapMaterial;
        private Material _defaultHeatmapMaterial;

        private PositionHeatmapAnalysisResult _visibleResult;
        private readonly Dictionary<MeshSamplerResult, MaterialPropertyBlock> _cachedPropertyBlocks = new();

        private void Awake()
        {
            SetupProjectionCamera(segmentedObjectDepthTextureResolution, radius * 2, 0.3f, 1000.0f);
            _sampleHeatmapMaterial = new Material(samplesHeatmapShader);
            _defaultHeatmapMaterial = new Material(defaultHeatmapShader);
        }

        private void SetupProjectionCamera(int res, float size, float nearClipPlane, float farClipPlane)
        {
            var segmentedObjectDepthTexture = new RenderTexture(res, res, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            segmentedObjectDepthTexture.anisoLevel = 0;
            segmentedObjectDepthTexture.useMipMap = false;
            segmentedObjectDepthTexture.Create();

            var half = size / 2;
            var orthographicMatrix = Matrix4x4.Ortho(-half, half, -half, half, nearClipPlane, farClipPlane);
            _segmentedObjectDepthCamera = gameObject.AddComponent<Camera>();
            _segmentedObjectDepthCamera.orthographic = true;
            _segmentedObjectDepthCamera.orthographicSize = size;
            _segmentedObjectDepthCamera.nearClipPlane = nearClipPlane;
            _segmentedObjectDepthCamera.farClipPlane = farClipPlane;
            _segmentedObjectDepthCamera.aspect = 1;
            _segmentedObjectDepthCamera.projectionMatrix = orthographicMatrix;
            _segmentedObjectDepthCamera.targetTexture = segmentedObjectDepthTexture;
        }

        public IEnumerator GenerateHeatmap(BufferedAsyncRecordLoader loader, PlayerAssets assets,
            string projectionCasterIdentifier, string[] projectionReceiversIdentifiers,
            ulong projectionStartTime, ulong projectionEndTime,
            Action<PositionHeatmapAnalysisResult> finishCallback)
        {
            if (projectionEndTime < projectionStartTime)
            {
                throw new Exception(
                    $"{nameof(projectionStartTime)} should be less or equal to {nameof(projectionEndTime)}.");
            }

            IsGenerating = true;

            var samplesMinValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
            var samplesMaxValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
            samplesMinValueBuffer.SetData(new[] { uint.MaxValue });
            samplesMaxValueBuffer.SetData(new[] { uint.MinValue });

            var projectionKernel = projectionShader.FindKernel("project_std_normal_distribution");

            var ctx = PlayerContext.NewContext("GenerateHeatmapContext_" + Guid.NewGuid(), assets);

            // key: mesh record id, value: sampled mesh containing values
            var meshSamplerResults = new Dictionary<int, MeshSamplerResult>();

            var result = new PositionHeatmapAnalysisResult(projectionCasterIdentifier,
                projectionReceiversIdentifiers, projectionStartTime, projectionEndTime,
                samplesMinValueBuffer, samplesMaxValueBuffer, meshSamplerResults);

            SetVisibleResult(result);

            var projectionSamplingInterval = (ulong)(1 / projectionSamplingRate * 1_000_000_000u);

            PrepareProjectionShader(samplesMinValueBuffer, samplesMaxValueBuffer, projectionKernel);

            var prevVSyncCount = QualitySettings.vSyncCount;
            var prevTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = int.MaxValue;

            if (projectionStartTime > 0)
            {
                yield return PlaySamplesInTimeRange(loader, ctx, 0, projectionStartTime - 1u);
            }

            var currentTime = projectionStartTime;

            while (currentTime <= projectionEndTime && currentTime <= loader.Duration)
            {
                var startTime = currentTime;
                var endTime = currentTime + projectionSamplingInterval;
                yield return PlaySamplesInTimeRange(loader, ctx, startTime, endTime);
                
                // TODO: also project onto children
                var replayProjectionCasterId = ctx.GetReplayInstanceId(projectionCasterIdentifier);
                var replayProjectionReceiversIds = projectionReceiversIdentifiers.Select(ctx.GetReplayInstanceId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value).ToArray();
                
                if (replayProjectionCasterId.HasValue && replayProjectionReceiversIds.Length > 0)
                {
                    if (currentTime >= projectionStartTime && currentTime <= projectionEndTime)
                    {
                        // TODO Handle both case where the given ID might be a gameobject or a transform
                        var projectionCaster = ctx.FindGameObjectByInstanceId(replayProjectionCasterId.Value);

                        if (projectionCaster != null)
                        {
                            var projectionReceiversGameObjects = replayProjectionReceiversIds
                                .Select(replayId => ctx.FindGameObjectByInstanceId(replayId))
                                .Where(t => t != null)
                                .Select(t => t.gameObject)
                                .ToArray();

                            if (projectionReceiversGameObjects.Length > 0)
                            {
                                yield return ProjectCurrentPosition(ctx, projectionCaster,
                                    projectionReceiversGameObjects,
                                    meshSamplerResults, projectionKernel);
                            }
                        }
                    }
                }

                currentTime = endTime + 1;
            }

            QualitySettings.vSyncCount = prevVSyncCount;
            Application.targetFrameRate = prevTargetFrameRate;

            PlayerContext.Destroy(ctx);

            PlayerContext.Activate(player.GetPlayerContext());
            IsGenerating = false;
            finishCallback(result);
        }

        private IEnumerator ProjectCurrentPosition(
            PlayerContext ctx,
            GameObject projectionCasterGameObject,
            GameObject[] projectionReceiversGameObjects,
            IDictionary<int, MeshSamplerResult> meshSamplerResults, int projectionKernel)
        {
            _segmentedObjectDepthCamera.transform.SetPositionAndRotation(
                projectionCasterGameObject.transform.position, Quaternion.Euler(90, 0, 0));
            
            yield return RenderSegmentedObjectsDepth(projectionReceiversGameObjects);

            var planes = GeometryUtility.CalculateFrustumPlanes(_segmentedObjectDepthCamera);

            projectionShader.SetMatrix("view_mtx", _segmentedObjectDepthCamera.worldToCameraMatrix);

            // TODO: Handle children of projection receivers as well
            foreach (var go in projectionReceiversGameObjects)
            {
                var hasRenderer = go.TryGetComponent<Renderer>(out var r);

                if (hasRenderer)
                {
                    var insideFrustum = GeometryUtility.TestPlanesAABB(planes, r.bounds);

                    // Only consider objects inside the view frustum of the cropped camera
                    if (!insideFrustum)
                        continue;

                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;

                    if (mesh == null || mesh.vertexBufferCount == 0)
                        continue;

                    var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());
                    var meshRecordIdentifier = ctx.GetRecordIdentifier(mesh.GetInstanceID());
                    
                    if (gameObjectIdentifier == null)
                        continue;
                    if (meshRecordIdentifier == null)
                        continue;
                    
                    // Two GameObjects might have the same sharedMesh. We add the gameObjectIdentifier as a discriminator.
                    var meshSamplerResultHash = HashCode.Combine(gameObjectIdentifier, meshRecordIdentifier);
                    var meshSamplerResult = GetOrCreateMeshSamplerResult(meshSamplerResultHash, mesh, meshSamplerResults);

                    if (meshSamplerResult == null)
                        continue;

                    projectionShader.SetInt("object_instance_id", go.GetInstanceID());
                    projectionShader.SetMatrix("model_mtx", r.localToWorldMatrix);
                    projectionShader.SetInt("n_triangles", (int)meshSamplerResult.NTriangles);
                    projectionShader.SetBuffer(projectionKernel, "index_buffer",
                        meshSamplerResult.IndexBuffer);
                    projectionShader.SetInt("index_buffer_stride", meshSamplerResult.IndexBufferStride);
                    projectionShader.SetBuffer(projectionKernel, "vertex_buffer",
                        meshSamplerResult.VertexBuffer);
                    projectionShader.SetInt("vertex_buffer_stride", meshSamplerResult.VertexBufferStride);
                    projectionShader.SetInt("vertex_buffer_position_offset",
                        meshSamplerResult.VertexBufferPositionOffset);
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
                }
            }
        }

        private IEnumerator RenderSegmentedObjectsDepth(GameObject[] projectionReceiversGameObjects)
        {
            // Only render the receivers as we don't want the caster to cast onto itself
            var cmdBuf = GetSegmentedObjectDepthCmdBuffer(projectionReceiversGameObjects);

            _segmentedObjectDepthCamera.RemoveAllCommandBuffers();
            _segmentedObjectDepthCamera.AddCommandBuffer(CameraEvent.AfterEverything, cmdBuf);

            // Wait for camera to render
            yield return new WaitForEndOfFrame();
        }

        private MeshSamplerResult GetOrCreateMeshSamplerResult(int meshSamplerResultHash, Mesh mesh, IDictionary<int, MeshSamplerResult> meshSamplerResults)
        {
            if (mesh == null || mesh.vertexBufferCount == 0)
                return null;

            if (meshSamplerResults.ContainsKey(meshSamplerResultHash))
                return meshSamplerResults[meshSamplerResultHash];

            var meshSamplerResult = meshSampler.Sample(mesh, samplesPerSquareMeter);
            meshSamplerResults.Add(meshSamplerResultHash, meshSamplerResult);
            return meshSamplerResult;
        }

        private IEnumerator PlaySamplesInTimeRange(BufferedAsyncRecordLoader loader, PlayerContext ctx,
            ulong startTime,
            ulong endTime)
        {
            var samplesLoadingTask = loader.SamplesInTimeRangeAsync(startTime, endTime);
            yield return new WaitUntil(() => samplesLoadingTask.IsCompleted);
            ctx.PlaySamples(player.PlayerModules, samplesLoadingTask.Result);
        }

        private void PrepareProjectionShader(ComputeBuffer samplesMinValueBuffer, ComputeBuffer samplesMaxValueBuffer,
            int projectionKernel)
        {
            projectionShader.SetFloat("n_sigmas", nSigmas);
            projectionShader.SetTexture(projectionKernel, "segmented_object_depth_texture",
                _segmentedObjectDepthCamera.targetTexture);
            projectionShader.SetMatrix("projection_mtx", _segmentedObjectDepthCamera.projectionMatrix);
            projectionShader.SetBool("is_projection_orthographic", _segmentedObjectDepthCamera.orthographic);
            projectionShader.SetBuffer(projectionKernel, "samples_min_value", samplesMinValueBuffer);
            projectionShader.SetBuffer(projectionKernel, "samples_max_value", samplesMaxValueBuffer);
        }

        private CommandBuffer GetSegmentedObjectDepthCmdBuffer(IEnumerable<GameObject> gameObjects)
        {
            var objectInstanceID = Shader.PropertyToID("object_instance_id");

            var cmdBuf = new CommandBuffer();
            cmdBuf.SetRenderTarget(_segmentedObjectDepthCamera.targetTexture);
            cmdBuf.ClearRenderTarget(true, true, Color.clear);

            foreach (var go in gameObjects)
            {
                var goRenderer = go.GetComponent<Renderer>();

                if (goRenderer != null)
                {
                    var instanceMaterial = new Material(segmentedObjectDepthShader);
                    instanceMaterial.SetInt(objectInstanceID, go.GetInstanceID());
                    cmdBuf.DrawRenderer(goRenderer, instanceMaterial);
                }
            }

            return cmdBuf;
        }

        private void LateUpdate()
        {
            var activeContext = PlayerContext.GetActiveContext();

            if (activeContext == null)
                return;

            if (_visibleResult != null)
            {
                var gameObjects = activeContext.GetAllGameObjects();
                
                foreach (var go in gameObjects)
                {
                    ApplyHeatmapMaterial(go, activeContext);
                }
            }
        }

        private void RestorePreviousMaterials(IEnumerable<GameObject> gameObjects)
        {
            foreach (var go in gameObjects)
            {
                if (!go.TryGetComponent<Renderer>(out var goRenderer))
                    continue;
                goRenderer.SetSharedMaterials(new List<Material>());
            }

            var samples = player.GetRecordLoader().SamplesInTimeRangeAsync(0, player.GetCurrentPlayTimeInNanoseconds());
            foreach (var sample in samples.Result)
            {
                if (sample.Payload is MeshRendererUpdateMaterials or SkinnedMeshRendererUpdateMaterials)
                {
                    foreach (var playerModule in player.PlayerModules)
                    {
                        playerModule.PlaySample(player.GetPlayerContext(), sample);
                    }
                }
            }
        }

        private void ApplyHeatmapMaterial(GameObject go, PlayerContext ctx)
        {
            if (!go.TryGetComponent<Renderer>(out var goRenderer))
            {
                return;
            }

            goRenderer.sharedMaterial = _defaultHeatmapMaterial;
            goRenderer.SetPropertyBlock(null);

            if (!go.TryGetComponent<MeshFilter>(out var meshFilter) || meshFilter.sharedMesh == null || meshFilter.sharedMesh.vertexCount == 0)
                return;

            var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());
            var meshRecordIdentifier = ctx.GetRecordIdentifier(meshFilter.sharedMesh.GetInstanceID());
            
            if (gameObjectIdentifier == null)
                return;
            if (meshRecordIdentifier == null)
                return;
            
            var meshSamplerResultHash = HashCode.Combine(gameObjectIdentifier, meshRecordIdentifier);
            
            var hasMeshSamplerResult =
                _visibleResult.SamplerResults.TryGetValue(meshSamplerResultHash, out var meshSamplerResult);

            if (!hasMeshSamplerResult)
                return;

            goRenderer.sharedMaterial = _sampleHeatmapMaterial;
            var propertyBlock = GetOrCreateResultPropertyBlock(meshSamplerResult);
            goRenderer.SetPropertyBlock(propertyBlock);
        }

        private MaterialPropertyBlock GetOrCreateResultPropertyBlock(MeshSamplerResult meshSamplerResult)
        {
            if (_cachedPropertyBlocks.TryGetValue(meshSamplerResult, out var propertyBlock))
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
            _cachedPropertyBlocks.Add(meshSamplerResult, newPropertyBlock);
            return newPropertyBlock;
        }

        public override void RemoveResult(PositionHeatmapAnalysisResult result)
        {
            base.RemoveResult(result);

            if (result == _visibleResult)
            {
                _cachedPropertyBlocks.Clear();
                SetVisibleResult(null);
            }
        }

        private void OnDestroy()
        {
            foreach (var result in GetResults())
            {
                result.Dispose();
            }

            _segmentedObjectDepthCamera.targetTexture.Release();
            _segmentedObjectDepthCamera.targetTexture = null;
        }

        public void SetVisibleResult(PositionHeatmapAnalysisResult result)
        {
            _visibleResult = result;

            if (result == null)
            {
                var gameObjects = player.GetPlayerContext().GetAllGameObjects();
                RestorePreviousMaterials(gameObjects);
            }
        }

        public PositionHeatmapAnalysisResult GetVisibleResult()
        {
            return _visibleResult;
        }
    }
}