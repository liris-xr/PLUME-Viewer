using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class CanvasPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case CanvasCreate canvasCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Canvas>(canvasCreate.Id);
                    break;
                }
                case CanvasDestroy canvasDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(canvasDestroy.Id);
                    break;
                }
                case CanvasUpdate canvasUpdate:
                {
                    var c = ctx.GetOrCreateComponentByIdentifier<Canvas>(canvasUpdate.Id);


                    if (canvasUpdate.HasRenderMode)
                        c.renderMode = canvasUpdate.RenderMode.ToEngineType();

                    if (canvasUpdate.HasScaleFactor)
                        c.scaleFactor = canvasUpdate.ScaleFactor;

                    if (canvasUpdate.HasReferencePixelsPerUnit)
                        c.referencePixelsPerUnit = canvasUpdate.ReferencePixelsPerUnit;

                    if (canvasUpdate.HasOverridePixelPerfect)
                        c.overridePixelPerfect = canvasUpdate.OverridePixelPerfect;

                    if (canvasUpdate.HasVertexColorAlwaysGammaSpace)
                        c.vertexColorAlwaysGammaSpace = canvasUpdate.VertexColorAlwaysGammaSpace;

                    if (canvasUpdate.HasPixelPerfect)
                        c.pixelPerfect = canvasUpdate.PixelPerfect;

                    if (canvasUpdate.HasPlaneDistance)
                        c.planeDistance = canvasUpdate.PlaneDistance;

                    if (canvasUpdate.HasOverrideSorting)
                        c.overrideSorting = canvasUpdate.OverrideSorting;

                    if (canvasUpdate.HasSortingOrder)
                        c.sortingOrder = canvasUpdate.SortingOrder;

                    if (canvasUpdate.HasTargetDisplay)
                        c.targetDisplay = canvasUpdate.TargetDisplay;

                    if (canvasUpdate.HasSortingLayerId)
                        c.sortingLayerID = canvasUpdate.SortingLayerId;

                    if (canvasUpdate.HasAdditionalShaderChannels)
                        c.additionalShaderChannels = (AdditionalCanvasShaderChannels) canvasUpdate.AdditionalShaderChannels;

                    if (canvasUpdate.HasSortingLayerName)
                        c.sortingLayerName = canvasUpdate.SortingLayerName;

                    if (canvasUpdate.HasUpdateRectTransformForStandalone)
                        c.updateRectTransformForStandalone =
                            canvasUpdate.UpdateRectTransformForStandalone.ToEngineType();

                    if (canvasUpdate.WorldCamera != null)
                        c.worldCamera = ctx.GetOrCreateComponentByIdentifier<Camera>(canvasUpdate.WorldCamera);

                    if (canvasUpdate.HasNormalizedSortingGridSize)
                        c.normalizedSortingGridSize = canvasUpdate.NormalizedSortingGridSize;

                    break;
                }
            }
        }
    }
}