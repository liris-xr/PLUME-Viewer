using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class TimelineMarkerElement : VisualElement
    {
        private static readonly Color DefaultColor = Color.cyan;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimelineMarkerElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlColorAttributeDescription _color = new()
                { name = "color", defaultValue = DefaultColor };

            private readonly UxmlUnsignedLongAttributeDescription _timeDivisionDuration = new()
                { name = "time-division-duration", defaultValue = TimeDivisionDurationDefault };

            private readonly UxmlFloatAttributeDescription _timeDivisionWidth = new()
                { name = "time-division-width", defaultValue = TimeDivisionWidthDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimelineMarkerElement;
                ele.SetColor(_color.GetValueFromBag(bag, cc));
                ele.TimeDivisionDuration = _timeDivisionDuration.GetValueFromBag(bag, cc);
                ele.TimeDivisionWidth = _timeDivisionWidth.GetValueFromBag(bag, cc);
            }
        }

        private readonly VisualElement _marker;
        private readonly VisualElement _markerStem;

        private ulong _time;
        private ulong _timeDivisionDuration;
        private float _timeDivisionWidth;

        public TimelineMarkerElement()
        {
            var uxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline_marker");
            _marker = uxml.Instantiate().Q("marker");
            _markerStem = _marker.Q("marker__stem");
            hierarchy.Add(_marker);
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.width = _marker.style.width;
            style.height = Length.Percent(100);
            style.flexGrow = 1;
            style.flexShrink = 0;
        }

        public void SetColor(Color color)
        {
            _markerStem.style.backgroundColor = color;
            _markerStem.MarkDirtyRepaint();
        }

        public void SetTime(ulong time)
        {
            _time = time;
            UpdateTimeOffset();
        }

        private void UpdateTimeOffset()
        {
            _marker.Q("time-offset").style.left = _time / (float)TimeDivisionDuration * TimeDivisionWidth;
        }

        public ulong TimeDivisionDuration
        {
            get => _timeDivisionDuration;
            set
            {
                _timeDivisionDuration = value;
                UpdateTimeOffset();
            }
        }

        public float TimeDivisionWidth
        {
            get => _timeDivisionWidth;
            set
            {
                _timeDivisionWidth = value;
                UpdateTimeOffset();
            }
        }

        public void SetScrollOffset(float scrollOffset)
        {
            _marker.Q("scroll-offset").style.left = -scrollOffset;
        }
    }
}