using System;
using PLUME.Sample;
using PLUME.Sample.Unity.XRITK;

namespace PLUME.Viewer.Player.Module.XRITK
{
    public class InputActionPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            var payload = sample.Payload;
            var time = sample.Timestamp;

            switch (payload)
            {
                case InputAction inputAction:
                {
                    switch (inputAction.ValueCase)
                    {
                        case InputAction.ValueOneofCase.None:
                            break;
                        case InputAction.ValueOneofCase.Boolean:
                            break;
                        case InputAction.ValueOneofCase.Integer:
                            break;
                        case InputAction.ValueOneofCase.Float:
                            break;
                        case InputAction.ValueOneofCase.Double:
                            break;
                        case InputAction.ValueOneofCase.Vector2:
                            break;
                        case InputAction.ValueOneofCase.Vector3:
                            break;
                        case InputAction.ValueOneofCase.Quaternion:
                            break;
                        case InputAction.ValueOneofCase.Button:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            }
        }
    }
}