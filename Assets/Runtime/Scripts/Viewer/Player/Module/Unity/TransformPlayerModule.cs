using PLUME.Sample.Unity;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class TransformPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case TransformCreate transformCreate:
                {
                    ctx.GetOrCreateTransformByIdentifier(transformCreate.Id);
                    break;
                }
                case TransformDestroy transformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(transformDestroy.Id.ParentId);
                    break;
                }
                case TransformUpdate transformUpdate:
                {
                    if (transformUpdate.ParentTransformId != null)
                        ctx.SetParent(transformUpdate.Id, transformUpdate.ParentTransformId);

                    if (transformUpdate.HasSiblingIdx)
                        ctx.SetSiblingIndex(transformUpdate.Id, transformUpdate.SiblingIdx);

                    var t = ctx.GetOrCreateTransformByIdentifier(transformUpdate.Id);

                    if (transformUpdate.LocalPosition != null)
                        t.localPosition = transformUpdate.LocalPosition.ToEngineType();

                    if (transformUpdate.LocalRotation != null)
                        t.localRotation = transformUpdate.LocalRotation.ToEngineType();

                    if (transformUpdate.LocalScale != null) t.localScale = transformUpdate.LocalScale.ToEngineType();

                    break;
                }
            }
        }
    }
}