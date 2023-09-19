using PLUME.Sample;
using PLUME.Sample.Unity;
using UnityEngine.SceneManagement;

namespace PLUME
{
    public class GameObjectPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
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
                case GameObjectUpdateName gameObjectUpdateName:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(gameObjectUpdateName.Id);
                    go.name = gameObjectUpdateName.Name;
                    break;
                }
                case GameObjectUpdateActiveSelf gameObjectUpdateActiveSelf:
                {
                    ctx.SetActive(gameObjectUpdateActiveSelf.Id, gameObjectUpdateActiveSelf.Active);
                    break;
                }
                case GameObjectUpdateScene gameObjectUpdateScene:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(gameObjectUpdateScene.Id);
                    // TODO handle multiple scenes
                    break;
                }
                case GameObjectUpdateLayer gameObjectUpdateLayer:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(gameObjectUpdateLayer.Id);
                    go.layer = gameObjectUpdateLayer.Layer;
                    break;
                }
                case GameObjectUpdateTag gameObjectUpdateTag:
                {
                    var go = ctx.GetOrCreateGameObjectByIdentifier(gameObjectUpdateTag.Id);
                    // go.tag = gameObjectUpdateTag.Tag;
                    break;
                }
            }
        }
    }
}