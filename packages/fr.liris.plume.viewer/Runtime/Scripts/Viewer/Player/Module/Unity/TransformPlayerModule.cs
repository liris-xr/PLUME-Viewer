using PLUME.Sample;
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
                    ctx.GetOrCreateTransformByIdentifier(transformCreate.Component);
                    break;
                }
                case TransformDestroy transformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(transformDestroy.Component.GameObject);
                    break;
                }
                case TransformUpdate transformUpdate:
                {
                    if (transformUpdate.ParentTransform != null)
                    {
                        ctx.SetParent(transformUpdate.Component, transformUpdate.ParentTransform);
                    }

                    if (transformUpdate.HasSiblingIdx)
                    {
                        ctx.SetSiblingIndex(transformUpdate.Component, transformUpdate.SiblingIdx);
                    }

                    var t = ctx.GetOrCreateTransformByIdentifier(transformUpdate.Component);

                    if (transformUpdate.LocalPosition != null)
                    {
                        t.localPosition = transformUpdate.LocalPosition.ToEngineType();
                    }

                    if (transformUpdate.LocalRotation != null)
                    {
                        t.localRotation = transformUpdate.LocalRotation.ToEngineType();
                    }

                    if (transformUpdate.LocalScale != null)
                    {
                        t.localScale = transformUpdate.LocalScale.ToEngineType();
                    }

                    break;
                }
            }
        }
    }
}