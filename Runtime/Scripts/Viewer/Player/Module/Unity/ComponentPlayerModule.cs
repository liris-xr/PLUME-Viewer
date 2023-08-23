using PLUME.Sample;
using PLUME.Sample.Unity;

namespace PLUME
{
    public class ComponentPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case ComponentDestroy componentDestroy:
                    ctx.TryDestroyComponentByIdentifier(componentDestroy.Id);
                    break;
            }
        }
    }
}