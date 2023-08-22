using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.UI
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
                case ImageUpdateColor imageUpdateColor:
                {
                    var i = ctx.GetOrCreateComponentByIdentifier<Image>(imageUpdateColor.Id);
                    i.color = imageUpdateColor.Color.ToEngineType();
                    break;
                }
                case ImageUpdateSprite imageUpdateSprite:
                {
                    var i = ctx.GetOrCreateComponentByIdentifier<Image>(imageUpdateSprite.Id);
                    i.sprite = ctx.GetOrDefaultAssetByIdentifier<Sprite>(imageUpdateSprite.SpriteId);
                    ctx.TryAddAssetIdentifierCorrespondence(imageUpdateSprite.SpriteId, i.sprite);
                    break;
                }
                case ImageDestroy imageDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(imageDestroy.Id);
                    break;
                }
            }
        }
    }
}