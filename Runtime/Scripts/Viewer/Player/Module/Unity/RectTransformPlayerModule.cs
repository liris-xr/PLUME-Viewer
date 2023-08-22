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
                    ctx.GetOrCreateGameObjectByIdentifier(rectTransformCreate.Id);
                    break;
                }
                case RectTransformDestroy rectTransformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(rectTransformDestroy.Id);
                    break;
                }
                case RectTransformUpdateParent rectTransformUpdateParent:
                {
                    ctx.SetParent(rectTransformUpdateParent.Id, rectTransformUpdateParent.ParentId);
                    break;
                }
                case RectTransformUpdateSiblingIndex rectTransformUpdateSiblingIndex:
                {
                    ctx.SetSiblingIndex(rectTransformUpdateSiblingIndex.Id,
                        rectTransformUpdateSiblingIndex.SiblingIndex);
                    break;
                }
                case RectTransformUpdatePosition rectTransformUpdatePosition:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdatePosition.Id);
                    t.localPosition = rectTransformUpdatePosition.LocalPosition.ToEngineType();
                    break;
                }
                case RectTransformUpdateRotation rectTransformUpdateRotation:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdateRotation.Id);
                    t.localRotation = rectTransformUpdateRotation.LocalRotation.ToEngineType();
                    break;
                }
                case RectTransformUpdateScale rectTransformUpdateScale:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdateScale.Id);
                    t.localScale = rectTransformUpdateScale.LocalScale.ToEngineType();
                    break;
                }
                case RectTransformUpdateAnchors rectTransformUpdateAnchors:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdateAnchors.Id);
                    t.anchorMin = rectTransformUpdateAnchors.AnchorMin.ToEngineType();
                    t.anchorMax = rectTransformUpdateAnchors.AnchorMax.ToEngineType();
                    break;
                }
                case RectTransformUpdateSize rectTransformUpdateSize:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdateSize.Id);
                    t.sizeDelta = rectTransformUpdateSize.SizeDelta.ToEngineType();
                    break;
                }
                case RectTransformUpdatePivot rectTransformUpdatePivot:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdatePivot.Id);
                    t.pivot = rectTransformUpdatePivot.Pivot.ToEngineType();
                    break;
                }
            }
        }
    }
}