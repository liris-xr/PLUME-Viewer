using Google.Protobuf;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    public abstract class PlayerModule<T> : PlayerModule where T : IMessage
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            if (sample.Payload is T)
            {
                PlaySample(ctx, sample as UnpackedSample<T>);
            }
        }

        public abstract void PlaySample(PlayerContext ctx, UnpackedSample<T> sample);
    }

    public abstract class PlayerModule : MonoBehaviour
    {
        public abstract void PlaySample(PlayerContext ctx, UnpackedSample sample);

        public virtual void Reset()
        {
        }
    }
}