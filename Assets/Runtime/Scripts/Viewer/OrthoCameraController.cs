using UnityEngine;

namespace PLUME.Viewer
{
    [RequireComponent(typeof(Camera))]
    public class OrthoCameraController : MonoBehaviour
    {
        private Camera _cam;
        private Vector2 _screen;
        private Vector2 _translationFactor = new(1, 1);
        public float zoomFactor = 0.1f;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _screen = new Vector2(Screen.width, Screen.height);
            ComputeTranslationFactor();
        }

        private void OnGUI()
        {
            if (Event.current.displayIndex != _cam.targetDisplay) return;

            var t = transform;
            var move = Event.current.delta;

            if (Event.current.type == EventType.MouseDrag && Event.current.button != 0)
            {
                t.position += t.right * (-move.x * _translationFactor.x) + t.up * (move.y * _translationFactor.y);
            }
            else if (Event.current.type == EventType.ScrollWheel)
            {
                _cam.orthographicSize *= 1.0f + move.y * zoomFactor;
                ComputeTranslationFactor();
            }
        }

        private void ComputeTranslationFactor()
        {
            _translationFactor = new Vector2(_cam.orthographicSize * 2.0f * _cam.aspect, _cam.orthographicSize * 2.0f) /
                                 _screen;
        }
    }
}