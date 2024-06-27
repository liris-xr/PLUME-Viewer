﻿using PLUME.Sample.Unity;
using UnityEngine;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class MeshFilterPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case MeshFilterCreate meshFilterCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<MeshFilter>(meshFilterCreate.Id);
                    break;
                }
                case MeshFilterDestroy meshFilterDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(meshFilterDestroy.Id);
                    break;
                }
                case MeshFilterUpdate meshFilterUpdate:
                {
                    var meshFilter = ctx.GetOrCreateComponentByIdentifier<MeshFilter>(meshFilterUpdate.Id);

                    if (meshFilterUpdate.MeshId != null)
                    {
                        meshFilter.sharedMesh = ctx.GetOrDefaultAssetByIdentifier<Mesh>(meshFilterUpdate.MeshId);
                        ctx.TryAddAssetIdentifierCorrespondence(meshFilterUpdate.MeshId, meshFilter.sharedMesh);
                    }

                    break;
                }
            }
        }
    }
}