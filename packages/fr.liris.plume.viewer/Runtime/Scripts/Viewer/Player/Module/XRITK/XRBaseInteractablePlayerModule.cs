using PLUME.Sample;
using PLUME.Sample.Unity.XRITK;
using PLUME.Viewer.Player.Proxy;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.XRITK
{
    public class XRBaseInteractablePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            var payload = rawSample.Payload;

            switch (payload)
            {
                case XRBaseInteractableCreate xrBaseInteractableCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<XRBaseInteractable>(xrBaseInteractableCreate.Component);
                    break;
                }
                case XRBaseInteractableDestroy xrBaseInteractableDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(xrBaseInteractableDestroy.Component);
                    break;
                }
                case XRBaseInteractableUpdate xrBaseInteractableUpdate:
                {
                    var interactable = ctx.GetOrCreateComponentByIdentifier<XRBaseInteractable>(xrBaseInteractableUpdate.Component);
                    interactable.enabled = xrBaseInteractableUpdate.Enabled;
                    break;
                }
            }
        }
    }
}