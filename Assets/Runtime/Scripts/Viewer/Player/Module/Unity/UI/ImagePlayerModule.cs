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
                    ctx.GetOrCreateComponentByIdentifier<Image>(imageCreate.Id);
                    break;
                }
                case ImageDestroy imageDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(imageDestroy.Id);
                    break;
                }
                case ImageUpdate imageUpdate:
                {
                    var img = ctx.GetOrCreateComponentByIdentifier<Image>(imageUpdate.Id);

                    if (imageUpdate.HasType)
                    {
                        img.type = imageUpdate.Type.ToEngineType();
                    }

                    if (imageUpdate.SpriteId != null)
                    {
                        img.sprite = ctx.GetOrDefaultAssetByIdentifier<Sprite>(imageUpdate.SpriteId);
                        ctx.TryAddAssetIdentifierCorrespondence(imageUpdate.SpriteId, img.sprite);
                    }

                    break;
                }
            }
        }
    }
}