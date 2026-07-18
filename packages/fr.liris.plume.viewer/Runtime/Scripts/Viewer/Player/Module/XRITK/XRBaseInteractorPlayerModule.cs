using PLUME.Sample.Unity.XRITK;
using PLUME.Viewer.Player.Proxy;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.XRITK
{
    public class XRBaseInteractorPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            var payload = rawSample.Payload;

            switch (payload)
            {
                case XRBaseInteractorCreate xrBaseInteractorCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<XRBaseInteractor>(xrBaseInteractorCreate.Component);
                    break;
                }
                case XRBaseInteractorDestroy xrBaseInteractorDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(xrBaseInteractorDestroy.Component);
                    break;
                }
                case XRBaseInteractorUpdate xrBaseInteractorUpdate:
                {
                    var interactor =
                        ctx.GetOrCreateComponentByIdentifier<XRBaseInteractor>(xrBaseInteractorUpdate.Component);
                    interactor.enabled = xrBaseInteractorUpdate.Enabled;
                    break;
                }
            }
        }
    }
}