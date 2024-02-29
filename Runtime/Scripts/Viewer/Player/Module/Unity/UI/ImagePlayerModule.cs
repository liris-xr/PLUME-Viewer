using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class ImagePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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
                    var i = ctx.GetOrCreateComponentByIdentifier<Image>(imageUpdate.Id);

                    if (imageUpdate.Color != null)
                    {
                        i.color = imageUpdate.Color.ToEngineType();
                    }

                    if (imageUpdate.SpriteId != null)
                    {
                        i.sprite = ctx.GetOrDefaultAssetByIdentifier<Sprite>(imageUpdate.SpriteId);
                        ctx.TryAddAssetIdentifierCorrespondence(imageUpdate.SpriteId, i.sprite);
                    }

                    break;
                }
            }
        }
    }
}