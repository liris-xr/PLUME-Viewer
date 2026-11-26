using System.Linq;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class LineRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case LineRendererCreate lineRendererCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<LineRenderer>(lineRendererCreate.Component);
                    break;
                }
                case LineRendererDestroy lineRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(lineRendererDestroy.Component);
                    break;
                }
                case LineRendererPositionsUpdate positionsUpdate:
                {
                    var lineRenderer = ctx.GetOrCreateComponentByIdentifier<LineRenderer>(positionsUpdate.Component);
                    if (lineRenderer == null)
                        break;

                    // Packed x, y, z triplets.
                    var coordinates = positionsUpdate.Positions;
                    var positions = new Vector3[coordinates.Count / 3];
                    for (var i = 0; i < positions.Length; i++)
                        positions[i] = new Vector3(coordinates[3 * i], coordinates[3 * i + 1], coordinates[3 * i + 2]);

                    lineRenderer.positionCount = positions.Length;
                    lineRenderer.SetPositions(positions);
                    break;
                }
                case LineRendererUpdate lineRendererUpdate:
                {
                    var lineRenderer = ctx.GetOrCreateComponentByIdentifier<LineRenderer>(lineRendererUpdate.Component);

                    if (lineRendererUpdate.HasLoop)
                        lineRenderer.loop = lineRendererUpdate.Loop;

                    if (lineRendererUpdate.Color != null)
                        lineRenderer.colorGradient = lineRendererUpdate.Color.ToEngineType();
                    
                    if(lineRendererUpdate.WidthCurve != null)
                        lineRenderer.widthCurve = lineRendererUpdate.WidthCurve.ToEngineType();
                    
                    if (lineRendererUpdate.HasWidthMultiplier)
                        lineRenderer.widthMultiplier = lineRendererUpdate.WidthMultiplier;

                    if (lineRendererUpdate.HasCornerVertices)
                        lineRenderer.numCornerVertices = lineRendererUpdate.CornerVertices;

                    if (lineRendererUpdate.HasEndCapVertices)
                        lineRenderer.numCapVertices = lineRendererUpdate.EndCapVertices;

                    if (lineRendererUpdate.HasAlignment)
                        lineRenderer.alignment = lineRendererUpdate.Alignment.ToEngineType();

                    if (lineRendererUpdate.HasTextureMode)
                        lineRenderer.textureMode = lineRendererUpdate.TextureMode.ToEngineType();

                    if (lineRendererUpdate.TextureScale != null)
                        lineRenderer.textureScale = lineRendererUpdate.TextureScale.ToEngineType();

                    if (lineRendererUpdate.HasShadowBias)
                        lineRenderer.shadowBias = lineRendererUpdate.ShadowBias;

                    if (lineRendererUpdate.HasGenerateLightingData)
                        lineRenderer.generateLightingData = lineRendererUpdate.GenerateLightingData;

                    if (lineRendererUpdate.HasUseWorldSpace)
                        lineRenderer.useWorldSpace = lineRendererUpdate.UseWorldSpace;

                    if (lineRendererUpdate.HasMaskInteraction)
                        lineRenderer.maskInteraction = lineRendererUpdate.MaskInteraction.ToEngineType();
                    
                    if (lineRendererUpdate.Positions != null)
                    {
                        var positions = lineRendererUpdate.Positions.Positions_.Select(p => p.ToEngineType()).ToArray();
                        lineRenderer.positionCount = positions.Length;
                        lineRenderer.SetPositions(positions);
                    }
                    
                    break;
                }
            }
        }
    }
}