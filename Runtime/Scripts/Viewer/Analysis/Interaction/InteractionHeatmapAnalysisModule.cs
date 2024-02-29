using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.XRITK;
using PLUME.Viewer.Player;
using UnityEngine;

namespace PLUME.Viewer.Analysis.Interaction
{
    public class InteractionHeatmapAnalysisModule : AnalysisModuleWithResults<InteractionHeatmapAnalysisResult>
    {
        public Player.Player player;

        public Shader interactionHeatmapShader;
        public Shader defaultHeatmapShader;

        public Color startColor = Color.white;
        public Color endColor = Color.red;

        private Material _interactionHeatmapMaterial;
        private Material _defaultHeatmapMaterial;

        private InteractionHeatmapAnalysisResult _visibleResult;

        private static readonly int StartColor = Shader.PropertyToID("_StartColor");
        private static readonly int EndColor = Shader.PropertyToID("_EndColor");
        private static readonly int InteractionCount = Shader.PropertyToID("_InteractionCount");
        private static readonly int MaxInteractionCount = Shader.PropertyToID("_MaxInteractionCount");

        private readonly Dictionary<int, MaterialPropertyBlock> _cachedInteractionsPropertyBlocks = new();

        private void Awake()
        {
            _defaultHeatmapMaterial = new Material(defaultHeatmapShader);
            _interactionHeatmapMaterial = new Material(interactionHeatmapShader);

            _defaultHeatmapMaterial.SetColor(StartColor, startColor);
            _interactionHeatmapMaterial.SetColor(StartColor, startColor);
            _interactionHeatmapMaterial.SetColor(EndColor, endColor);
        }

        public IEnumerator GenerateHeatmap(BufferedAsyncFramesLoader framesLoader,
            InteractionAnalysisModuleParameters parameters,
            Action<InteractionHeatmapAnalysisResult> finishCallback)
        {
            Dictionary<string, int> interactions = new();

            var totalInteractionsCount = 0;
            var maxInteractionsCount = 0;

            if (parameters.EndTime < parameters.StartTime)
            {
                throw new Exception(
                    $"{nameof(parameters.EndTime)} should be less or equal {nameof(parameters.StartTime)}.");
            }

            var framesLoadingTask = framesLoader.FramesInTimeRangeAsync(parameters.StartTime, parameters.EndTime);
            yield return new WaitUntil(() => framesLoadingTask.IsCompleted);

            foreach (var frame in framesLoadingTask.Result)
            {
                foreach (var sample in frame.Data)
                {
                    GameObjectIdentifier interactorIdentifier;
                    GameObjectIdentifier interactableIdentifier;

                    if (parameters.InteractionType == InteractionType.Hover &&
                        sample.Payload is XRBaseInteractableHoverEnter hoverEnter)
                    {
                        interactorIdentifier = hoverEnter.InteractorCurrent.ParentId;
                        interactableIdentifier = hoverEnter.Id.ParentId;
                    }
                    else if (parameters.InteractionType == InteractionType.Select &&
                             sample.Payload is XRBaseInteractableSelectEnter selectEnter)
                    {
                        interactorIdentifier = selectEnter.InteractorCurrent.ParentId;
                        interactableIdentifier = selectEnter.Id.ParentId;
                    }
                    else if (parameters.InteractionType == InteractionType.Activate &&
                             sample.Payload is XRBaseInteractableActivateEnter activateEnter)
                    {
                        interactorIdentifier = activateEnter.InteractorCurrent.ParentId;
                        interactableIdentifier = activateEnter.Id.ParentId;
                    }
                    else
                    {
                        continue;
                    }

                    if (interactorIdentifier == null || interactableIdentifier == null)
                        continue;

                    if (!parameters.InteractorsIds.Contains(interactorIdentifier.GameObjectId)) continue;

                    if (parameters.InteractablesIds.Length > 0 &&
                        !parameters.InteractablesIds.Contains(interactableIdentifier.GameObjectId)) continue;

                    if (interactions.ContainsKey(interactableIdentifier.GameObjectId))
                    {
                        interactions[interactableIdentifier.GameObjectId]++;
                    }
                    else
                    {
                        interactions.Add(interactableIdentifier.GameObjectId, 1);
                    }

                    maxInteractionsCount = Math.Max(maxInteractionsCount,
                        interactions[interactableIdentifier.GameObjectId]);
                    totalInteractionsCount++;
                }
            }

            var result = new InteractionHeatmapAnalysisResult(parameters, interactions, totalInteractionsCount,
                maxInteractionsCount);

            finishCallback(result);
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

            var frames = player.GetFramesLoader()
                .FramesInTimeRangeAsync(0, player.GetCurrentPlayTimeInNanoseconds());
            foreach (var frame in frames.Result)
            {
                foreach (var data in frame.Data)
                {
                    if (data.Payload is RendererUpdate or SkinnedMeshRendererUpdate)
                    {
                        foreach (var playerModule in player.PlayerModules)
                        {
                            playerModule.PlaySample(ctx, data);
                        }
                    }
                }
            }
        }

        private void ApplyHeatmapMaterials(PlayerContext ctx)
        {
            var gameObjects = ctx.GetAllGameObjects();

            // For GameObjects with interactions, apply the custom heatmap material to its render and its children
            foreach (var go in gameObjects)
            {
                if (!go.TryGetComponent<Renderer>(out var goRenderer))
                    continue;

                var nSharedMaterials = goRenderer.sharedMaterials.Length;
                goRenderer.sharedMaterials = Enumerable.Repeat(_defaultHeatmapMaterial, nSharedMaterials).ToArray();
                goRenderer.SetPropertyBlock(null);

                var gameObjectIdentifier = ctx.GetRecordIdentifier(go.GetInstanceID());

                if (gameObjectIdentifier == null)
                    continue;

                var renderers = new List<Renderer>();
                renderers.Add(goRenderer);
                renderers.AddRange(go.GetComponentsInChildren<Renderer>());

                if (_visibleResult == null ||
                    !_visibleResult.Interactions.TryGetValue(gameObjectIdentifier, out var interactionsCount) ||
                    interactionsCount == 0)
                    continue;

                foreach (var r in renderers)
                {
                    var propertyBlock = GetOrCreateInteractionsPropertyBlock(r);
                    propertyBlock.SetInt(InteractionCount, interactionsCount);
                    propertyBlock.SetInt(MaxInteractionCount, _visibleResult.MaxInteractionCount);

                    var nRendererSharedMaterials = r.sharedMaterials.Length;
                    r.sharedMaterials = Enumerable.Repeat(_interactionHeatmapMaterial, nRendererSharedMaterials)
                        .ToArray();
                    r.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private MaterialPropertyBlock GetOrCreateInteractionsPropertyBlock(Renderer r)
        {
            if (_cachedInteractionsPropertyBlocks.TryGetValue(r.GetInstanceID(), out var propertyBlock))
            {
                return propertyBlock;
            }

            var newPropertyBlock = new MaterialPropertyBlock();
            _cachedInteractionsPropertyBlocks.Add(r.GetInstanceID(), newPropertyBlock);
            return newPropertyBlock;
        }

        public override void RemoveResult(InteractionHeatmapAnalysisResult result)
        {
            base.RemoveResult(result);

            if (result == _visibleResult)
            {
                _cachedInteractionsPropertyBlocks.Clear();
                SetVisibleResult(null);
            }
        }

        public void SetVisibleResult(InteractionHeatmapAnalysisResult result)
        {
            var prevVisibleResult = _visibleResult;

            _visibleResult = result;

            if (result == null && prevVisibleResult != null)
            {
                RestoreRecordMaterials(player.GetPlayerContext());
            }
        }

        public InteractionHeatmapAnalysisResult GetVisibleResult()
        {
            return _visibleResult;
        }
    }
}