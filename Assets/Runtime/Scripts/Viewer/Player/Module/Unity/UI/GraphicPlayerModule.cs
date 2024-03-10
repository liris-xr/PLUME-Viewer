using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class GraphicPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case GraphicUpdate graphicUpdate:
                {
                    var graphic = ctx.GetOrCreateComponentByIdentifier<Graphic>(graphicUpdate.Id);

                    if (graphicUpdate.Color != null)
                    {
                        graphic.color = graphicUpdate.Color.ToEngineType();
                    }

                    if (graphicUpdate.MaterialId != null)
                    {
                        graphic.material = ctx.GetOrDefaultAssetByIdentifier<Material>(graphicUpdate.MaterialId);
                        ctx.TryAddAssetIdentifierCorrespondence(graphicUpdate.MaterialId, graphic.material);
                    }
                    
                    if (graphicUpdate.HasRaycastTarget)
                    {
                        graphic.raycastTarget = graphicUpdate.RaycastTarget;
                    }
                    
                    if (graphicUpdate.RaycastPadding != null)
                    {
                        graphic.raycastPadding = graphicUpdate.RaycastPadding.ToEngineType();
                    }

                    break;
                }
            }
        }
    }
}