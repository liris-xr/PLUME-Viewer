using PLUME.Sample.Unity;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class GameObjectPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case GameObjectCreate gameObjectCreate:
                {
                    ctx.GetOrCreateGameObjectByIdentifier(gameObjectCreate.Id);
                    break;
                }
                case GameObjectDestroy gameObjectDestroy:
                {
                    ctx.TryDestroyGameObjectByIdentifier(gameObjectDestroy.Id);
                    break;
                }
                case GameObjectUpdate gameObjectUpdate:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(gameObjectUpdate.Id);

                    if (gameObjectUpdate.HasName) ctx.SetName(gameObjectUpdate.Id, gameObjectUpdate.Name);

                    if (gameObjectUpdate.HasActive) ctx.SetActive(gameObjectUpdate.Id, gameObjectUpdate.Active);

                    if (gameObjectUpdate.HasLayer) go.layer = gameObjectUpdate.Layer;

                    if (gameObjectUpdate.HasTag) ctx.SetGameObjectTag(gameObjectUpdate.Id, gameObjectUpdate.Tag);

                    if (gameObjectUpdate.HasSceneId)
                    {
                        // TODO handle multiple scenes
                    }

                    break;
                }
            }
        }
    }
}