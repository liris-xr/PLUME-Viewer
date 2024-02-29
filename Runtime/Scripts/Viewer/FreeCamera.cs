#define USE_INPUT_SYSTEM
using System;
using PLUME.Viewer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PLUME
{
    [RequireComponent(typeof(Camera))]
    public class FreeCamera : PreviewCamera
    {
        [NonSerialized] public bool InputDisabled = true;

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
        }

        private void OnEnable()
        {
            RegisterInputs();
        }

        private void RegisterInputs()
        {
            var map = new InputActionMap("Free Camera");

            _lookAction = map.AddAction("look", binding: "<Mouse>/delta");
            _moveAction = map.AddAction("move");
            _speedAction = map.AddAction("speed");
            _yMoveAction = map.AddAction("yMove");

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
                .With("Down", "<Keyboard>/q");

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

            var lookDelta = _lookAction.ReadValue<Vector2>();
            _inputRotateAxisX = lookDelta.x * lookSpeedMouse * MouseSensitivityMultiplier;
            _inputRotateAxisY = lookDelta.y * lookSpeedMouse * MouseSensitivityMultiplier;

            _leftShift = Keyboard.current?.leftShiftKey?.isPressed ?? false;
            _inputChangeSpeed = _speedAction.ReadValue<Vector2>().y;

            var moveDelta = _moveAction.ReadValue<Vector2>();
            _inputVertical = moveDelta.y;
            _inputHorizontal = moveDelta.x;
            _inputYAxis = _yMoveAction.ReadValue<Vector2>().y;
        }

        private void Update()
        {
            if (InputDisabled)
                return;

            // Disable inputs if the camera is not selected
            if (Player.Instance.GetCurrentPreviewCamera() != this)
                return;

            if (Mouse.current?.rightButton?.isPressed == false)
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
                if (_leftShiftBoost && _leftShift)
                    speed *= turbo;

                var position = t.position;
                position += t.forward * (speed * _inputVertical);
                position += t.right * (speed * _inputHorizontal);
                position += Vector3.up * (speed * _inputYAxis);
                t.position = position;
            }
        }

        public override Camera GetCamera()
        {
            return _camera;
        }

        public override void SetEnabled(bool enabled)
        {
            var cam = GetCamera();

            if (cam != null)
            {
                cam.targetTexture = enabled ? PreviewRenderTexture : null;
                cam.enabled = enabled;
            }
        }

        public override PreviewCameraType GetCameraType()
        {
            return PreviewCameraType.Free;
        }

        public override void ResetView()
        {
            var cam = Player.Instance.GetMainCamera().GetCamera();

            if (cam == null)
            {
                transform.position = new Vector3(-2.24f, 1.84f, 0.58f);
                transform.rotation = Quaternion.Euler(25f, -140f, 0f);
            }
            else
            {
                var t = transform;
                var camTransform = cam.transform;
                t.position = camTransform.position;
                var eulerAngles = camTransform.rotation.eulerAngles;
                eulerAngles.z = 0;
                t.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }
}