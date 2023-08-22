using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;

namespace PLUME.UI
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
                case CanvasUpdateRenderMode canvasUpdateRenderMode:
                {
                    var c = ctx.GetOrCreateComponentByIdentifier<Canvas>(canvasUpdateRenderMode.Id);
                    c.renderMode = (RenderMode) canvasUpdateRenderMode.RenderMode;
                    break;
                }
                case CanvasDestroy canvasDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(canvasDestroy.Id);
                    break;
                }
            }
        }
    }
}