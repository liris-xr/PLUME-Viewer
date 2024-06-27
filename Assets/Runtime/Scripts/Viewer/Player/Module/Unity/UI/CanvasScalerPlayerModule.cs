using PLUME.Sample.Unity.UI;
using UnityEngine.UI;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class CanvasScalerPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case CanvasScalerCreate canvasScalerCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<CanvasScaler>(canvasScalerCreate.Id);
                    break;
                }
                case CanvasScalerDestroy canvasScalerDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(canvasScalerDestroy.Id);
                    break;
                }
                case CanvasScalerUpdate canvasScalerUpdate:
                {
                    var cs = ctx.GetOrCreateComponentByIdentifier<CanvasScaler>(canvasScalerUpdate.Id);

                    if (canvasScalerUpdate.HasUiScaleMode)
                        cs.uiScaleMode = canvasScalerUpdate.UiScaleMode.ToEngineType();

                    if (canvasScalerUpdate.HasReferencePixelsPerUnit)
                        cs.referencePixelsPerUnit = canvasScalerUpdate.ReferencePixelsPerUnit;

                    if (canvasScalerUpdate.HasScaleFactor)
                        cs.scaleFactor = canvasScalerUpdate.ScaleFactor;

                    if (canvasScalerUpdate.ReferenceResolution != null)
                        cs.referenceResolution = canvasScalerUpdate.ReferenceResolution.ToEngineType();

                    if (canvasScalerUpdate.HasScreenMatchMode)
                        cs.screenMatchMode = canvasScalerUpdate.ScreenMatchMode.ToEngineType();

                    if (canvasScalerUpdate.HasMatchWidthOrHeight)
                        cs.matchWidthOrHeight = canvasScalerUpdate.MatchWidthOrHeight;

                    if (canvasScalerUpdate.HasPhysicalUnit)
                        cs.physicalUnit = canvasScalerUpdate.PhysicalUnit.ToEngineType();

                    if (canvasScalerUpdate.HasFallbackScreenDpi)
                        cs.fallbackScreenDPI = canvasScalerUpdate.FallbackScreenDpi;

                    if (canvasScalerUpdate.HasDefaultSpriteDpi)
                        cs.defaultSpriteDPI = canvasScalerUpdate.DefaultSpriteDpi;

                    if (canvasScalerUpdate.HasDynamicPixelsPerUnit)
                        cs.dynamicPixelsPerUnit = canvasScalerUpdate.DynamicPixelsPerUnit;

                    break;
                }
            }
        }
    }
}