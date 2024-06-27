using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class MeshRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case MeshRendererCreate meshRendererCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<MeshRenderer>(meshRendererCreate.Id);
                    break;
                }
                case MeshRendererDestroy meshRendererDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(meshRendererDestroy.Id);
                    break;
                }
            }
        }
    }
}