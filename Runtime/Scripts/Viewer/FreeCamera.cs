#define USE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PLUME
{
    [RequireComponent(typeof(Camera))]
    public class FreeCamera : MonoBehaviour
    {
        [NonSerialized]
        public bool Disabled = true;

        private Camera _camera;
        
        private const float MouseSensitivityMultiplier = 0.01f;

        /// <summary>
        /// Rotation speed when using the mouse.
        /// </summary>
        public float lookSpeedMouse = 4.0f;

        /// <summary>
        /// Movement speed.
        /// </summary>
        public float moveSpeed = 10.0f;

        /// <summary>
        /// Value added to the speed when incrementing.
        /// </summary>
        public float moveSpeedIncrement = 2.5f;

        /// <summary>
        /// Scale factor of the turbo mode.
        /// </summary>
        public float turbo = 10.0f;

        private InputAction _lookAction;
        private InputAction _moveAction;
        private InputAction _speedAction;
        private InputAction _yMoveAction;

        private float _inputRotateAxisX, _inputRotateAxisY;
        private float _inputChangeSpeed;
        private float _inputVertical, _inputHorizontal, _inputYAxis;
        private bool _leftShiftBoost, _leftShift, _fire1;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.targetTexture = RenderTexture.GetTemporary(1920, 1080);
        }

        private void OnDestroy()
        {
            _camera.targetTexture.Release();
        }

        public RenderTexture GetRenderTexture()
        {
            return _camera.targetTexture;
        }

        private void OnEnable()
        {
            RegisterInputs();
        }

        private void RegisterInputs()
        {
            var map = new InputActionMap("Free Camera");

            _lookAction = map.AddAction("look", binding: "<Mouse>/delta");
            _moveAction = map.AddAction("move", binding: "<Gamepad>/leftStick");
            _speedAction = map.AddAction("speed", binding: "<Gamepad>/dpad");
            _yMoveAction = map.AddAction("yMove");

            _lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");
            _moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            _speedAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/home")
                .With("Down", "<Keyboard>/end");
            _yMoveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/pageUp")
                .With("Down", "<Keyboard>/pageDown")
                .With("Up", "<Keyboard>/e")
                .With("Down", "<Keyboard>/q")
                .With("Up", "<Gamepad>/rightshoulder")
                .With("Down", "<Gamepad>/leftshoulder");

            _moveAction.Enable();
            _lookAction.Enable();
            _speedAction.Enable();
            _yMoveAction.Enable();
        }

        private void UpdateInputs()
        {
            _inputRotateAxisX = 0.0f;
            _inputRotateAxisY = 0.0f;
            _leftShiftBoost = false;
            _fire1 = false;

            var lookDelta = _lookAction.ReadValue<Vector2>();
            _inputRotateAxisX = lookDelta.x * lookSpeedMouse * MouseSensitivityMultiplier;
            _inputRotateAxisY = lookDelta.y * lookSpeedMouse * MouseSensitivityMultiplier;

            _leftShift = Keyboard.current?.leftShiftKey?.isPressed ?? false;
            _fire1 = Mouse.current?.leftButton?.isPressed == true || Gamepad.current?.xButton?.isPressed == true;

            _inputChangeSpeed = _speedAction.ReadValue<Vector2>().y;

            var moveDelta = _moveAction.ReadValue<Vector2>();
            _inputVertical = moveDelta.y;
            _inputHorizontal = moveDelta.x;
            _inputYAxis = _yMoveAction.ReadValue<Vector2>().y;
        }

        private void Update()
        {
            if (Disabled)
                return;

            UpdateInputs();

            if (_inputChangeSpeed != 0.0f)
            {
                moveSpeed += _inputChangeSpeed * moveSpeedIncrement;
                if (moveSpeed < moveSpeedIncrement) moveSpeed = moveSpeedIncrement;
            }

            var moved = _inputRotateAxisX != 0.0f || _inputRotateAxisY != 0.0f || _inputVertical != 0.0f ||
                        _inputHorizontal != 0.0f || _inputYAxis != 0.0f;
            if (moved)
            {
                var localEulerAngles = transform.localEulerAngles;
                var rotationX = localEulerAngles.x;
                var newRotationY = localEulerAngles.y + _inputRotateAxisX;

                // Weird clamping code due to weird Euler angle mapping...
                var newRotationX = (rotationX - _inputRotateAxisY);
                if (rotationX <= 90.0f && newRotationX >= 0.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
                if (rotationX >= 270.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);

                var t = transform;
                t.localRotation = Quaternion.Euler(newRotationX, newRotationY, t.localEulerAngles.z);

                var speed = Time.deltaTime * this.moveSpeed;
                if (_fire1 || _leftShiftBoost && _leftShift)
                    speed *= turbo;

                var position = t.position;
                position += t.forward * (speed * _inputVertical);
                position += t.right * (speed * _inputHorizontal);
                position += Vector3.up * (speed * _inputYAxis);
                t.position = position;
            }
        }
    }
}