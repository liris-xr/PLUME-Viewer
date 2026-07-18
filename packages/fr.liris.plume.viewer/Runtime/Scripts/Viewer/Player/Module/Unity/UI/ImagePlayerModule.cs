using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class ImagePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case ImageCreate imageCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Image>(imageCreate.Component);
                    break;
                }
                case ImageDestroy imageDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(imageDestroy.Component);
                    break;
                }
                case ImageUpdate imageUpdate:
                {
                    var img = ctx.GetOrCreateComponentByIdentifier<Image>(imageUpdate.Component);

                    if (imageUpdate.HasType)
                    {
                        img.type = imageUpdate.Type.ToEngineType();
                    }

                    if (imageUpdate.Sprite != null)
                    {
                        img.sprite = ctx.GetOrDefaultAssetByIdentifier<Sprite>(imageUpdate.Sprite);
                        ctx.TryAddAssetIdentifierCorrespondence(imageUpdate.Sprite, img.sprite);
                    }

                    break;
                }
            }
        }
    }
}