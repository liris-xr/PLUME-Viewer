using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimelineElement : VisualElement
    {
        private static readonly VisualTreeAsset Uxml;
        
        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;
        private const int TicksPerDivisionDefault = 10;
        
        static TimelineElement()
        {
            Uxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline");
        }
        
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

        public override VisualElement contentContainer => _contentContainer;

        private readonly VisualElement _contentContainer;

        private readonly TimeScaleElement _timeScale;
        private readonly VisualElement _timeCursor;
        private readonly ScrollView _scrollView;

        private readonly Scroller _horizontalScroller;
        private readonly ScrollView _timeScaleScrollView;

        private readonly VisualElement _tracksPlaceholder;
        private readonly VisualElement _tracksContainer;
        private readonly List<TimelineTrackElement> _tracks = new();
        
        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
        private float _timeDivisionWidth;

        public TimelineElement()
        {
            var timeline = Uxml.Instantiate().Q("timeline");
            hierarchy.Add(timeline);
            
            _horizontalScroller = timeline.Q<Scroller>("horizontal-scroller");
            _timeScaleScrollView = timeline.Q<ScrollView>("time-scale-scroll-view");

            _tracksPlaceholder = timeline.Q("tracks-placeholder");
            _tracksContainer = timeline.Q("tracks-container");
            
            _horizontalScroller.slider.RegisterValueChangedCallback(evt =>
            {
                _timeScaleScrollView.horizontalScroller.value = evt.newValue;
            });
            
            _scrollView = timeline.Q<ScrollView>();
            _contentContainer = timeline.Q("timeline-content-container");
            _timeScale = timeline.Q<TimeScaleElement>("time-scale");
            _timeCursor = timeline.Q("time-cursor");
            
            _scrollView.horizontalScroller.slider.pageSize = TimeDivisionWidthDefault;
            _scrollView.horizontalScroller.slider.RegisterValueChangedCallback(OnScroll);
            RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
        }

        public void AddTrack(TimelineTrackElement track)
        {
            _tracks.Add(track);

            _tracksPlaceholder.style.display = DisplayStyle.None;
            
            _tracksContainer.Add(track);
            _tracksContainer.style.display = DisplayStyle.Flex;

            var trackScroller = track.Q<Scroller>("horizontal-scroller");
            
            _horizontalScroller.slider.RegisterValueChangedCallback(evt =>
            {
                trackScroller.value = evt.newValue;
            });

            UpdateHorizontalScroller();
        }
        
        public void SetCursorTime(ulong time)
        {
            _timeCursor.style.left = time / (float) TimeDivisionDuration * TimeDivisionWidth;
        }

        private void OnGeometryChanged()
        {
            UpdateHorizontalScroller();
            Repaint();
        }

        private void UpdateHorizontalScroller()
        {
            var visibleDuration = _timeScaleScrollView.contentViewport.contentRect.width / _timeDivisionWidth * _timeDivisionDuration;
            var hiddenDuration = _duration - visibleDuration;
            var hiddenWidth = hiddenDuration / _timeDivisionDuration * _timeDivisionWidth;

            var scrollerDragger = _horizontalScroller.Q("unity-slider").Q("unity-dragger");
            
            if (visibleDuration >= _duration)
            {
                scrollerDragger.visible = false;
                _horizontalScroller.SetEnabled(false);
                _horizontalScroller.lowValue = 0;
                _horizontalScroller.highValue = 0;

                _timeScaleScrollView.horizontalScroller.lowValue = 0;
                _timeScaleScrollView.horizontalScroller.highValue = 0;
                
                foreach (var track in _tracks)
                {
                    var trackScroller = track.Q<Scroller>("horizontal-scroller");
                    trackScroller.lowValue = 0;
                    trackScroller.highValue = 0;
                }
            }
            else
            {
                scrollerDragger.visible = true;
                _horizontalScroller.lowValue = 0;
                _horizontalScroller.highValue = hiddenWidth;
            
                _timeScaleScrollView.horizontalScroller.lowValue = 0;
                _timeScaleScrollView.horizontalScroller.highValue = hiddenWidth;

                foreach (var track in _tracks)
                {
                    var trackScroller = track.Q<Scroller>("horizontal-scroller");
                    trackScroller.lowValue = 0;
                    trackScroller.highValue = hiddenWidth;
                }
            }
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

            var clippingRect = _timeScaleScrollView.contentViewport.contentRect;
            clippingRect.x += _timeScaleScrollView.scrollOffset.x;
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