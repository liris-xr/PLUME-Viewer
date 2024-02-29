using PLUME.Sample;
using PLUME.Sample.Unity;

namespace PLUME
{
    public class RectTransformPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case RectTransformCreate rectTransformCreate:
                {
                    ctx.GetOrCreateGameObjectByIdentifier(rectTransformCreate.Id.ParentId);
                    break;
                }
                case RectTransformDestroy rectTransformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(rectTransformDestroy.Id.ParentId);
                    break;
                }
                case RectTransformUpdate rectTransformUpdate:
                {
                    if (rectTransformUpdate.ParentId != null)
                    {
                        ctx.SetParent(rectTransformUpdate.Id, rectTransformUpdate.ParentId);
                    }

                    if (rectTransformUpdate.HasSiblingIdx)
                    {
                        ctx.SetSiblingIndex(rectTransformUpdate.Id, rectTransformUpdate.SiblingIdx);
                    }

                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdate.Id);

                    if (rectTransformUpdate.LocalPosition != null)
                    {
                        t.localPosition = rectTransformUpdate.LocalPosition.ToEngineType();
                    }

                    if (rectTransformUpdate.LocalRotation != null)
                    {
                        t.localRotation = rectTransformUpdate.LocalRotation.ToEngineType();
                    }

                    if (rectTransformUpdate.LocalScale != null)
                    {
                        t.localScale = rectTransformUpdate.LocalScale.ToEngineType();
                    }

                    if (rectTransformUpdate.AnchorMin != null)
                    {
                        t.anchorMin = rectTransformUpdate.AnchorMin.ToEngineType();
                    }

                    if (rectTransformUpdate.AnchorMax != null)
                    {
                        t.anchorMax = rectTransformUpdate.AnchorMax.ToEngineType();
                    }

                    if (rectTransformUpdate.SizeDelta != null)
                    {
                        t.sizeDelta = rectTransformUpdate.SizeDelta.ToEngineType();
                    }

                    if (rectTransformUpdate.Pivot != null)
                    {
                        t.pivot = rectTransformUpdate.Pivot.ToEngineType();
                    }

                    break;
                }
            }
        }
    }
}