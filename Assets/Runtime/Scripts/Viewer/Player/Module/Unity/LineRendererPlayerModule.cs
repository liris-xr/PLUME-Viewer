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
                    ctx.GetOrCreateComponentByIdentifier<LineRenderer>(lineRendererCreate.Id);
                    break;
                }
                case LineRendererDestroy lineRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(lineRendererDestroy.Id);
                    break;
                }
                case LineRendererUpdate lineRendererUpdate:
                {
                    var lineRenderer = ctx.GetOrCreateComponentByIdentifier<LineRenderer>(lineRendererUpdate.Id);

                    if (lineRendererUpdate.HasLoop)
                        lineRenderer.loop = lineRendererUpdate.Loop;

                    if (lineRendererUpdate.Color != null)
                        lineRenderer.colorGradient = lineRendererUpdate.Color.ToEngineType();

                    if (lineRendererUpdate.WidthCurve != null)
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