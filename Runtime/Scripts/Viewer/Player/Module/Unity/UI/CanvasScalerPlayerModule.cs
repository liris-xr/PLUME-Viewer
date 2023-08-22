using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine.UI;

namespace PLUME.UI
{
    public class CanvasScalerPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case CanvasScalerCreate canvasScalerCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<CanvasScaler>(canvasScalerCreate.Id);
                    break;
                }
                case CanvasScalerUpdatePixelsPerUnit canvasScalerUpdatePixelsPerUnit:
                {
                    var cs = ctx.GetOrCreateComponentByIdentifier<CanvasScaler>(canvasScalerUpdatePixelsPerUnit.Id);
                    cs.dynamicPixelsPerUnit = canvasScalerUpdatePixelsPerUnit.DynamicPixelsPerUnit;
                    cs.referencePixelsPerUnit = canvasScalerUpdatePixelsPerUnit.ReferencePixelsPerUnit;
                    break;
                }
                case CanvasScalerDestroy canvasScalerDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(canvasScalerDestroy.Id);
                    break;
                }
            }
        }
    }
}