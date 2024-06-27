using Google.Protobuf;
using UnityEngine;

namespace PLUME.Viewer.Player
{
    public abstract class PlayerModule<T> : PlayerModule where T : IMessage
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            if (rawSample.Payload is T) PlaySample(ctx, rawSample as RawSample<T>);
        }

        public abstract void PlaySample(PlayerContext ctx, RawSample<T> rawSample);
    }

    public abstract class PlayerModule : MonoBehaviour
    {
        public abstract void PlaySample(PlayerContext ctx, RawSample rawSample);

        public virtual void Reset()
        {
        }
    }
}