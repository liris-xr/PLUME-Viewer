using PLUME.Sample;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using Vector2 = PLUME.Sample.Common.Vector2;

namespace PLUME.Viewer.Player.Module.Unity
{
    // TODO: Fix this module, it looks like when the parent is not updated yet applying the values results sizeDelta
    public class RectTransformPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case RectTransformCreate rectTransformCreate:
                {
                    ctx.GetOrCreateRectTransformByIdentifier(rectTransformCreate.Id);
                    break;
                }
                case RectTransformDestroy rectTransformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(rectTransformDestroy.Id.ParentId);
                    break;
                }
                case RectTransformUpdate rectTransformUpdate:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdate.Id);
                    
                    if (rectTransformUpdate.AnchorMin != null)
                    {
                        t.anchorMin = rectTransformUpdate.AnchorMin.ToEngineType();
                    }
                    
                    if (rectTransformUpdate.AnchorMax != null)
                    {
                        t.anchorMax = rectTransformUpdate.AnchorMax.ToEngineType();
                    }
                    
                    if (rectTransformUpdate.Pivot != null)
                    {
                        t.pivot = rectTransformUpdate.Pivot.ToEngineType();
                    }
                    
                    if(rectTransformUpdate.AnchoredPosition != null)
                    {
                        t.anchoredPosition = rectTransformUpdate.AnchoredPosition.ToEngineType();
                    }
                    
                    if (rectTransformUpdate.SizeDelta != null)
                    {
                        t.sizeDelta = rectTransformUpdate.SizeDelta.ToEngineType();
                    }
                
                    break;
                }
            }
        }
    }
}