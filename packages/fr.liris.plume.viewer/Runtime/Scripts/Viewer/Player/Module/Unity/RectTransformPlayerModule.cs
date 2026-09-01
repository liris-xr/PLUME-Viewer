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
                    ctx.GetOrCreateRectTransformByIdentifier(rectTransformCreate.Component);
                    break;
                }
                case RectTransformDestroy rectTransformDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(rectTransformDestroy.Component.GameObject);
                    break;
                }
                case RectTransformUpdate rectTransformUpdate:
                {
                    var t = ctx.GetOrCreateRectTransformByIdentifier(rectTransformUpdate.Component);
                    
                    if (rectTransformUpdate.ParentTransform != null)
                    {
                        ctx.SetParent(rectTransformUpdate.Component, rectTransformUpdate.ParentTransform);
                    }

                    if (rectTransformUpdate.HasSiblingIdx)
                    {
                        ctx.SetSiblingIndex(rectTransformUpdate.Component, rectTransformUpdate.SiblingIdx);
                    }

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