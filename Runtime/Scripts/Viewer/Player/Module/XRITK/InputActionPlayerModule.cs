using System;
using Google.Protobuf;
using PLUME.Sample;
using PLUME.Sample.Unity.XRITK;
using UnityEngine;

namespace PLUME
{
    public class InputActionPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            var payload = sample.Payload;
            var time = sample.Header.Time;

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