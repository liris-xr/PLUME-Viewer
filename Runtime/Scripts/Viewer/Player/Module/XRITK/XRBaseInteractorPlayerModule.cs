using PLUME.Sample;
using PLUME.Sample.Unity.XRITK;
using UnityEngine;

namespace PLUME
{
    public class XRBaseInteractorPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            var payload = sample.Payload;
            var time = sample.Header.Time;

            switch (payload)
            {
                case XRBaseInteractorCreate xrBaseInteractorCreate:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractorCreate.Id.ParentId);
                    Debug.Log($"XR Base Interactor : {go.name} has been created");
                    break;
                }
                case XRBaseInteractorDestroy xrBaseInteractorDestroy:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractorDestroy.Id.ParentId);
                    Debug.Log($"XR Base Interactor : {go.name} has been destroyed");
                    break;
                }
                case XRBaseInteractorSetEnabled xrBaseInteractorSetEnabled:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractorSetEnabled.Id.ParentId);
                    string message;
                    if (xrBaseInteractorSetEnabled.Enabled)
                        message = "XR Base Interactor : {0} has been enabled";
                    else
                        message = "XR Base Interactor : {0} has been disabled";
                    Debug.Log(string.Format(message, go.name));
                    break;
                }
            }
        }
    }
}