using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimelineElement : VisualElement
    {
        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;
        private const int TicksPerDivisionDefault = 10;

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimelineElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlUnsignedLongAttributeDescription _duration = new()
                {name = "duration", defaultValue = DurationDefault};

            private readonly UxmlUnsignedLongAttributeDescription _timeDivisionDuration = new()
                {name = "time-division-duration", defaultValue = TimeDivisionDurationDefault};

            private readonly UxmlIntAttributeDescription _ticksPerDivision = new()
                {name = "ticks-per-division", defaultValue = TicksPerDivisionDefault};

            private readonly UxmlFloatAttributeDescription _timeDivisionWidth = new()
                {name = "time-division-width", defaultValue = TimeDivisionWidthDefault};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimelineElement;
                ele.Duration = _duration.GetValueFromBag(bag, cc);
                ele.TimeDivisionDuration = _timeDivisionDuration.GetValueFromBag(bag, cc);
                ele.TicksPerDivision = _ticksPerDivision.GetValueFromBag(bag, cc);
                ele.TimeDivisionWidth = _timeDivisionWidth.GetValueFromBag(bag, cc);
            }
        }

        private static readonly StyleSheet StyleSheet;

        public override VisualElement contentContainer => _contentContainer;

        private VisualElement _contentContainer;

        private readonly TimeScaleElement _timeScale;
        private VisualElement _timeCursor;
        private ScrollView _scrollView;

        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
        private float _timeDivisionWidth;

        static TimelineElement()
        {
            StyleSheet = Resources.Load<StyleSheet>("UI/Styles/timeline");
        }

        public TimelineElement()
        {
            styleSheets.Add(StyleSheet);

            var infoPanel = new VisualElement {name = "time-info-panel"};
            infoPanel.AddToClassList("panel-container-primary");

            var timeIndicator = new TimeFieldElement {name = "time-field"};
            infoPanel.Add(timeIndicator);

            _scrollView = new ScrollView();
            _scrollView.AddToClassList("panel-container-secondary");

            _contentContainer = new VisualElement {name = "timeline-content-container"};

            _timeScale =
                new TimeScaleElement(DurationDefault, TimeDivisionDurationDefault, TicksPerDivisionDefault,
                    TimeDivisionWidthDefault) {name = "time-scale"};

            CreateTimeCursor();

            _scrollView.horizontalScroller.slider.pageSize = TimeDivisionWidthDefault;
            _scrollView.Add(_timeScale);
            _scrollView.Add(_timeCursor);
            _scrollView.Add(_contentContainer);

            hierarchy.Add(infoPanel);
            hierarchy.Add(_scrollView);

            _scrollView.RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
            _scrollView.horizontalScroller.slider.RegisterValueChangedCallback(OnScroll);

            AddToClassList("timeline");
        }

        private void CreateTimeCursor()
        {
            _timeCursor = new VisualElement {name = "time-cursor"};
            var timeCursorHandle = new VisualElement {name = "time-cursor__handle"};
            var timeCursorStem = new VisualElement {name = "time-cursor__stem"};
            _timeCursor.style.position = new StyleEnum<Position>(Position.Absolute);
            _timeCursor.pickingMode = PickingMode.Ignore;
            timeCursorHandle.pickingMode = PickingMode.Position;
            timeCursorStem.pickingMode = PickingMode.Ignore;
            _timeCursor.Add(timeCursorHandle);
            _timeCursor.Add(timeCursorStem);
        }

        public void SetCursorTime(ulong time)
        {
            _timeCursor.style.left = time / (float) TimeDivisionDuration * TimeDivisionWidth;
        }

        private void OnGeometryChanged()
        {
            Repaint();
        }

        public void Repaint()
        {
            UpdateTimeScale();
            UpdateContentContainerSize();
        }

        private void OnScroll(ChangeEvent<float> evt)
        {
            UpdateTimeScale();
        }

        private void UpdateContentContainerSize()
        {
            _contentContainer.style.width = Duration / (float) TimeDivisionDuration * TimeDivisionWidth;
        }

        private void UpdateTimeScale()
        {
            _timeScale.SetDuration(Duration);
            _timeScale.SetTimeDivisionDuration(TimeDivisionDuration);
            _timeScale.SetTimeDivisionWidth(TimeDivisionWidth);
            _timeScale.SetTicksPerDivision(TicksPerDivision);
            _timeScale.style.minWidth = Duration / (float) TimeDivisionDuration * TimeDivisionWidth
                                        + _timeScale.resolvedStyle.paddingLeft + _timeScale.resolvedStyle.paddingRight;

            var clippingRect = _scrollView.contentViewport.contentRect;
            clippingRect.x += _scrollView.scrollOffset.x;
            _timeScale.SetTicksClippingRect(clippingRect);
            _timeScale.Repaint();
        }

        public ulong Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                _timeScale.SetDuration(value);
            }
        }

        public ulong TimeDivisionDuration
        {
            get => _timeDivisionDuration;
            set
            {
                _timeDivisionDuration = value;
                _timeScale.SetTimeDivisionDuration(value);
            }
        }

        public int TicksPerDivision
        {
            get => _ticksPerDivision;
            set
            {
                _ticksPerDivision = value;
                _timeScale.SetTicksPerDivision(value);
            }
        }

        public float TimeDivisionWidth
        {
            get => _timeDivisionWidth;
            set
            {
                _timeDivisionWidth = value;
                _timeScale.SetTimeDivisionWidth(value);
                _scrollView.horizontalScroller.slider.pageSize = _timeDivisionWidth;
            }
        }
    }
}