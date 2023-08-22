using PLUME.Sample;
using PLUME.Sample.Unity;

namespace PLUME
{
    public class TransformPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case TransformCreate transformCreate:
                {
                    ctx.GetOrCreateGameObjectByIdentifier(transformCreate.Id);
                    break;
                }
                case TransformDestroy transformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(transformDestroy.Id);
                    break;
                }
                case TransformUpdateParent transformUpdateParent:
                {
                    ctx.SetParent(transformUpdateParent.Id, transformUpdateParent.ParentId);
                    break;
                }
                case TransformUpdateSiblingIndex transformUpdateSiblingIndex:
                {
                    ctx.SetSiblingIndex(transformUpdateSiblingIndex.Id, transformUpdateSiblingIndex.SiblingIndex);
                    break;
                }
                case TransformUpdatePosition transformUpdatePosition:
                {
                    var t = ctx.GetOrCreateTransformByIdentifier(transformUpdatePosition.Id);
                    t.localPosition = transformUpdatePosition.LocalPosition.ToEngineType();
                    break;
                }
                case TransformUpdateRotation transformUpdateRotation:
                {
                    var t = ctx.GetOrCreateTransformByIdentifier(transformUpdateRotation.Id);
                    t.localRotation = transformUpdateRotation.LocalRotation.ToEngineType();
                    break;
                }
                case TransformUpdateScale transformUpdateScale:
                {
                    var t = ctx.GetOrCreateTransformByIdentifier(transformUpdateScale.Id);
                    t.localScale = transformUpdateScale.LocalScale.ToEngineType();
                    break;
                }
            }
        }
    }
}