using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME
{
    public class CameraPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case CameraCreate cameraCreate:
                    ctx.GetOrCreateComponentByIdentifier<Camera>(cameraCreate.Id);
                    break;
                case CameraDestroy cameraDestroy:
                    ctx.TryDestroyComponentByIdentifier(cameraDestroy.Id);
                    break;
            }
        }
    }
}