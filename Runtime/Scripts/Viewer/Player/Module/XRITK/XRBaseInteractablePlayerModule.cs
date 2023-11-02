using PLUME.Sample;
using PLUME.Sample.Unity.XRITK;
using UnityEngine;

namespace PLUME
{
    public class XRBaseInteractablePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            var payload = sample.Payload;
            var time = sample.Header.Time;

            switch (payload)
            {
                case XRBaseInteractableCreate xrBaseInteractableCreate:
                    {
                        var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractableCreate.Id.ParentId);
                        Debug.Log($"XR Base Interactable : {go.name} has been created");
                        break;
                    }
                case XRBaseInteractableDestroy xrBaseInteractableDestroy:
                    {
                        var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractableDestroy.Id.ParentId);
                        Debug.Log($"XR Base Interactable : {go.name} has been destroyed");
                        break;
                    }
                case XRBaseInteractableSetEnabled xrBaseInteractableSetEnabled:
                    {
                        var go = ctx.GetOrCreateGameObjectByIdentifier(xrBaseInteractableSetEnabled.Id.ParentId);
                        string message;
                        if (xrBaseInteractableSetEnabled.Enabled)
                            message = "XR Base Interactable : {0} has been enabled";
                        else
                            message = "XR Base Interactable : {0} has been disabled";
                        Debug.Log(string.Format(message, go.name));
                        break;
                    }
                
            }

        }
    }
}