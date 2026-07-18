using PLUME.Sample.Unity;

namespace PLUME.Viewer.Player.Module.Unity
{
    public class ScenePlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case LoadScene loadScene:
                {
                    ctx.GetOrCreateSceneByIdentifier(loadScene.Scene);
                    break;
                }
                case UnloadScene unloadScene:
                {
                    ctx.DestroyScene(unloadScene.Scene);
                    break;
                }
            }
        }
    }
}