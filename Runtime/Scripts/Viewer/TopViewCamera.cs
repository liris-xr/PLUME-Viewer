#define USE_INPUT_SYSTEM
using System;
using PLUME.Viewer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PLUME
{
    [RequireComponent(typeof(Camera))]
    public class TopViewCamera : PreviewCamera
    {
        [NonSerialized]
        public bool InputDisabled = true;
        [NonSerialized]
        public bool ZoomDisabled = true;

        private Camera _camera;

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

        private InputAction _moveAction;
        private InputAction _speedAction;
        private InputAction _yMoveAction;
        private InputAction _zoomAction;

        private float _inputChangeSpeed;
        private float _inputVertical, _inputHorizontal, _inputYAxis, _scrollYAxis;
        private bool _leftShiftBoost, _leftShift;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down);
        }
        
        public override void SetPreviewRenderTexture(RenderTexture previewRenderTexture)
        {
            base.SetPreviewRenderTexture(previewRenderTexture);
            _camera.targetTexture = previewRenderTexture;
        }

        private void OnEnable()
        {
            RegisterInputs();
        }

        private void RegisterInputs()
        {
            var map = new InputActionMap("Top View Camera");
            
            _moveAction = map.AddAction("move");
            _speedAction = map.AddAction("speed");
            _yMoveAction = map.AddAction("yMove");
            _zoomAction = map.AddAction("zoom", binding: "<Mouse>/scroll");

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
            _speedAction.Enable();
            _yMoveAction.Enable();
            _zoomAction.Enable();
        }

        private void UpdateInputs()
        {
            _leftShiftBoost = false;

            _leftShift = Keyboard.current?.leftShiftKey?.isPressed ?? false;
            _inputChangeSpeed = _speedAction.ReadValue<Vector2>().y;

            var moveDelta = _moveAction.ReadValue<Vector2>();
            _inputVertical = moveDelta.y;
            _inputHorizontal = moveDelta.x;
            _inputYAxis = _yMoveAction.ReadValue<Vector2>().y;
            _scrollYAxis = _zoomAction.ReadValue<Vector2>().y;
        }

        private void Update()
        {
            if (InputDisabled)
                return;

            UpdateInputs();

            if (_inputChangeSpeed != 0.0f)
            {
                moveSpeed += _inputChangeSpeed * moveSpeedIncrement;
                if (moveSpeed < moveSpeedIncrement) moveSpeed = moveSpeedIncrement;
            }

            var moved = _inputVertical != 0.0f || _inputHorizontal != 0.0f || _inputYAxis != 0.0f || (!ZoomDisabled && _scrollYAxis != 0.0f);
            if (moved)
            {
                var t = transform;

                if (!ZoomDisabled)
                {
                    var zoom = Math.Sign(_scrollYAxis) * -0.5f;
                    _camera.orthographicSize = Math.Max(_camera.orthographicSize + zoom, 1);
                }

                var speed = Time.deltaTime * moveSpeed;
                if (_leftShiftBoost && _leftShift)
                    speed *= turbo;
                
                var position = t.position;
                position.x += speed * _inputHorizontal;
                position.y += speed * _inputYAxis;
                position.z += speed * _inputVertical;
                t.position = position;
            }
        }
        
        public override Camera GetCamera()
        {
            return _camera;
        }
        
        public override void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            GetCamera().enabled = enabled;
        }

        public override PreviewCameraType GetCameraType()
        {
            return PreviewCameraType.TopView;
        }
    }
}