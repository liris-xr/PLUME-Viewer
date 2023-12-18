using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PLUME.Sample.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PLUME
{
    public class PositionHeatmapAnalysisModule : AnalysisModuleWithResults<PositionHeatmapAnalysisResult>, IDisposable
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

        public PlayerContext _generationContext;
        public bool IsGenerating { get; private set; }
        public float GenerationProgress { get; private set; }

        private Camera _projectionCamera;

        private Material _sampleHeatmapMaterial;
        private Material _defaultHeatmapMaterial;
        private Material _segmentedObjectDepthMaterial;

        private PositionHeatmapAnalysisResult _visibleResult;
        private readonly Dictionary<MeshSamplerResult, MaterialPropertyBlock> _cachedMeshSamplerResultPropertyBlocks = new();
        private readonly Dictionary<int, MaterialPropertyBlock> _cachedSegmentedObjectsDepthPropertyBlocks = new();

        private void Awake()
        {
            SetupProjectionCamera(segmentedObjectDepthTextureResolution, radius * 2, 0.3f, 1000.0f);
            _sampleHeatmapMaterial = new Material(samplesHeatmapShader);
            _defaultHeatmapMaterial = new Material(defaultHeatmapShader);
            _segmentedObjectDepthMaterial = new Material(segmentedObjectDepthShader);
        }

        private void SetupProjectionCamera(int res, float size, float nearClipPlane, float farClipPlane)
        {
            var segmentedObjectDepthTexture = new RenderTexture(res, res, 24, GraphicsFormat.R32G32B32A32_SFloat, 1);
            segmentedObjectDepthTexture.anisoLevel = 0;
            segmentedObjectDepthTexture.useMipMap = false;
            segmentedObjectDepthTexture.Create();

            var half = size / 2;
            var orthographicMatrix = Matrix4x4.Ortho(-half, half, -half, half, nearClipPlane, farClipPlane);
            _projectionCamera = gameObject.AddComponent<Camera>();
            _projectionCamera.enabled = false;
            _projectionCamera.orthographic = true;
            _projectionCamera.orthographicSize = size;
            _projectionCamera.nearClipPlane = nearClipPlane;
            _projectionCamera.farClipPlane = farClipPlane;
            _projectionCamera.aspect = 1;
            _projectionCamera.projectionMatrix = orthographicMatrix;
            _projectionCamera.targetTexture = segmentedObjectDepthTexture;
        }

        public IEnumerator GenerateHeatmap(BufferedAsyncRecordLoader loader, PlayerAssets assets,
            PositionHeatmapAnalysisModuleParameters parameters,
            Action<PositionHeatmapAnalysisResult> finishCallback)
        {
            if (parameters.EndTime < parameters.StartTime)
            {
                throw new Exception(
                    $"{nameof(parameters.StartTime)} should be less or equal to {nameof(parameters.EndTime)}.");
            }
            
            if (player.GetModuleGenerating() != null)
            {
                Debug.LogWarning("Another module is already generating");
                yield break;
            }

            GenerationProgress = 0;
            IsGenerating = true;
            player.SetModuleGenerating(this);

            var samplesMinValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
            var samplesMaxValueBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(uint)));
            samplesMinValueBuffer.SetData(new[] { uint.MaxValue });
            samplesMaxValueBuffer.SetData(new[] { uint.MinValue });

            var projectionKernel = projectionShader.FindKernel("project_std_normal_distribution");

            _generationContext = PlayerContext.NewContext("GenerateHeatmapContext_" + Guid.NewGuid(), assets);

            // key: mesh record id, value: sampled mesh containing values
            var meshSamplerResults = new Dictionary<int, MeshSamplerResult>();

            var result = new PositionHeatmapAnalysisResult(parameters, samplesMinValueBuffer, samplesMaxValueBuffer, meshSamplerResults);

            SetVisibleResult(result);

            var projectionSamplingInterval = (ulong)(1 / projectionSamplingRate * 1_000_000_000u);

            PrepareProjectionShader(samplesMinValueBuffer, samplesMaxValueBuffer, projectionKernel);

            var prevVSyncCount = QualitySettings.vSyncCount;
            var prevTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = int.MaxValue;

            if (parameters.StartTime > 0)
            {
                yield return PlaySamplesInTimeRange(loader, _generationContext, 0, parameters.StartTime - 1u);
            }

            var currentTime = parameters.StartTime;

            while (currentTime <= parameters.EndTime && currentTime <= loader.Duration)
            {
                var startTime = currentTime;
                var endTime = currentTime + projectionSamplingInterval;
                yield return PlaySamplesInTimeRange(loader, _generationContext, startTime, endTime);
                
                var replayProjectionCasterId = _generationContext.GetReplayInstanceId(parameters.CasterIdentifier);
                var replayProjectionReceiversIds = new List<int>();

                foreach (var receiversIdentifier in parameters.ReceiversIdentifiers)
                {
                    var replayId = _generationContext.GetReplayInstanceId(receiversIdentifier);
                    if (!replayId.HasValue) continue;
                    
                    if(!replayProjectionReceiversIds.Contains(replayId.Value))
                        replayProjectionReceiversIds.Add(replayId.Value);

                    if (!parameters.IncludeReceiversChildren) continue;
                    
                    var go = _generationContext.FindGameObjectByInstanceId(replayId.Value);

                    foreach (var goInstanceId in go.GetComponentsInChildren<Renderer>().Select(r => r.gameObject.GetInstanceID()))
                    {
                        if(!replayProjectionReceiversIds.Contains(goInstanceId))
                            replayProjectionReceiversIds.Add(goInstanceId);
                    }
                }
                
                if (replayProjectionCasterId.HasValue && replayProjectionReceiversIds.Count > 0)
                {
                    if (currentTime >= parameters.StartTime && currentTime <= parameters.EndTime)
                    {
                        var projectionCaster = _generationContext.FindGameObjectByInstanceId(replayProjectionCasterId.Value);

                        if (projectionCaster != null)
                        {
                            var projectionReceiversGameObjects = replayProjectionReceiversIds
                                .Select(replayId => _generationContext.FindGameObjectByInstanceId(replayId))
                                .Where(t => t != null)
                                .Select(t => t.gameObject)
                                .ToArray();

                            if (projectionReceiversGameObjects.Length > 0)
                            {
                                ProjectCurrentPosition(_generationContext, projectionCaster,
                                    projectionReceiversGameObjects,
                                    meshSamplerResults, projectionKernel);
                            }
                        }
                    }
                }

                currentTime = endTime + 1;
                GenerationProgress = (currentTime - parameters.StartTime) / (float)(parameters.EndTime - parameters.StartTime);
            }
            
            GenerationProgress = 1;
            
            QualitySettings.vSyncCount = prevVSyncCount;
            Application.targetFrameRate = prevTargetFrameRate;

            PlayerContext.Destroy(_generationContext);
            _generationContext = null;

            PlayerContext.Activate(player.GetPlayerContext());
            IsGenerating = false;
            
            if(player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);
            
            finishCallback(result);
        }

        private void ProjectCurrentPosition(
            PlayerContext ctx,
            GameObject projectionCasterGameObject,
            GameObject[] projectionReceiversGameObjects,
            IDictionary<int, MeshSamplerResult> meshSamplerResults, int projectionKernel)
        {
            _projectionCamera.transform.SetPositionAndRotation(
                projectionCasterGameObject.transform.position, Quaternion.Euler(90, 0, 0));

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

                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;

                    if (mesh == null || mesh.vertexBufferCount == 0)
                        continue;
                    
                    var meshSamplerResult = GetOrCreateMeshSamplerResult(ctx, go, mesh, meshSamplerResults);

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

        public void CancelGenerate()
        {
            if (_generationContext != null)
            {
                PlayerContext.Destroy(_generationContext);
                _generationContext = null;
            }

            PlayerContext.Activate(player.GetPlayerContext());
            IsGenerating = false;
            
            if(player.GetModuleGenerating() == this)
                player.SetModuleGenerating(null);
        }
        
        private MeshSamplerResult GetOrCreateMeshSamplerResult(PlayerContext ctx, GameObject go, Mesh mesh, IDictionary<int, MeshSamplerResult> meshSamplerResults)
        {
            var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());
            var meshIdentifier = ctx.GetRecordIdentifier(mesh.GetInstanceID());
                    
            if (gameObjectIdentifier == null || meshIdentifier == null)
                return null;
            
            // Two GameObjects might have the same sharedMesh. We add the gameObjectIdentifier as a discriminator.
            var meshSamplerResultHash = HashCode.Combine(gameObjectIdentifier, meshIdentifier);
            
            if (mesh == null || mesh.vertexBufferCount == 0)
                return null;

            if (meshSamplerResults.TryGetValue(meshSamplerResultHash, out var result))
                return result;
            
            var meshSamplerResult = meshSampler.Sample(mesh, samplesPerSquareMeter, go.transform.lossyScale);
            meshSamplerResult.Name = go.name + "_" + (uint) meshSamplerResultHash;
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
                        playerModule.PlaySample(ctx, sample);
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
                goRenderer.sharedMaterials = Enumerable.Repeat(_segmentedObjectDepthMaterial, nSharedMaterials).ToArray();
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
            newPropertyBlock.SetInteger(objectInstanceID, go.GetInstanceID());
            _cachedSegmentedObjectsDepthPropertyBlocks.Add(go.GetInstanceID(), newPropertyBlock);
            return newPropertyBlock;
        }
        
        private void ApplyHeatmapMaterials(PlayerContext ctx)
        {
            var gameObjects = ctx.GetAllGameObjects();

            foreach (var go in gameObjects)
            {
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

                if (gameObjectIdentifier == null)
                    continue;
                if (meshRecordIdentifier == null)
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

        public override void RemoveResult(PositionHeatmapAnalysisResult result)
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
        
        private Vector3 SampleIndexToBarycentricWeights(uint sampleIdx, uint triangleResolution)
        {
            var row = (uint)Math.Ceiling((-3 + Math.Sqrt(8.0 * sampleIdx + 9.0)) / 2.0);
            var col = sampleIdx - row * (row + 1) / 2u;

            var wi = col / (float)triangleResolution;
            var wj = 1 - row / (float)triangleResolution;
            var wk = 1 - (wi + wj);

            return new Vector3(wi, wj, wk);
        }
        
        public void ExportResult(PositionHeatmapAnalysisResult result)
        {
            var resultIdx = GetResultIndex(result);
            var outputDir = $"Outputs/Analysis/EyeGazeHeatmaps/{resultIdx}";
            
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
                    filePath = $"{outputDir}/heatmap_{(uint) hash}.ply";
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
                        BitConverter.ToSingle(verticesArr, i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset),
                        BitConverter.ToSingle(verticesArr, i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset + 4),
                        BitConverter.ToSingle(verticesArr, i * samplerResult.VertexBuffer.stride + samplerResult.VertexBufferPositionOffset + 8)
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

                resultIdx++;
            }
        }
        
        public void SetVisibleResult(PositionHeatmapAnalysisResult result)
        {
            var prevVisibleResult = _visibleResult;
            
            _visibleResult = result;
            
            if (result == null && prevVisibleResult != null)
            {
                RestoreRecordMaterials(player.GetPlayerContext());
            }
        }

        public PositionHeatmapAnalysisResult GetVisibleResult()
        {
            return _visibleResult;
        }

        private void OnDestroy()
        {
            foreach (var result in GetResults())
            {
                result.Dispose();
            }

            _projectionCamera.targetTexture.Release();
            _projectionCamera.targetTexture = null;
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