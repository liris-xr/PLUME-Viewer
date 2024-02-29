using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class MeshRendererPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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