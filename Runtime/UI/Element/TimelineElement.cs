using System.Collections.Generic;
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

        private readonly TimeScaleElement _timeScale;
        
        private readonly VisualElement _timeCursor;

        private float _timeCursorPosition;
        
        private readonly Scroller _horizontalScroller;
        private readonly ScrollView _timeScaleScrollView;

        private readonly VisualElement _tracksPlaceholder;
        private readonly VisualElement _tracksContainer;
        private readonly List<TimelinePhysiologicalSignalTrackElement> _tracks = new();
        
        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
        private float _timeDivisionWidth;

        public TimelineElement()
        {
            var uxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline");
            var timeline = uxml.Instantiate().Q("timeline");
            hierarchy.Add(timeline);
            
            _horizontalScroller = timeline.Q<Scroller>("horizontal-scroller");
            _timeScaleScrollView = timeline.Q<ScrollView>("time-scale-scroll-view");

            _tracksPlaceholder = timeline.Q("tracks-placeholder");
            _tracksContainer = timeline.Q("tracks-container");
            
            _timeScale = timeline.Q<TimeScaleElement>("time-scale");
            _timeCursor = timeline.Q("time-cursor");
            
            _horizontalScroller.slider.RegisterValueChangedCallback(evt =>
            {
                _timeScaleScrollView.horizontalScroller.value = evt.newValue;
                
                _timeCursor.Q("scroll-offset").style.left = - evt.newValue;
                
                OnScroll(evt);
            });
            
            RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
        }

        public void AddTrack(TimelinePhysiologicalSignalTrackElement physiologicalSignalTrack)
        {
            _tracks.Add(physiologicalSignalTrack);

            _tracksPlaceholder.style.display = DisplayStyle.None;
            
            _tracksContainer.Add(physiologicalSignalTrack);
            _tracksContainer.style.display = DisplayStyle.Flex;
            
            _horizontalScroller.slider.RegisterValueChangedCallback(evt =>
            {
                physiologicalSignalTrack.GetHorizontalScroller().value = evt.newValue;
            });
            
            UpdateHorizontalScroller();
        }

        public void KeepTimeCursorInView()
        {
            var viewWidth = _timeScaleScrollView.contentViewport.contentRect.width;

            var cursorViewRelativePosition = _timeCursorPosition - _horizontalScroller.slider.value;
            var isOutsideOfView = cursorViewRelativePosition < 0 || cursorViewRelativePosition > viewWidth;
            
            if (isOutsideOfView)
            {
                _horizontalScroller.slider.value = _timeCursorPosition;
            }
        }
        
        public void SetCursorTime(ulong time)
        {
            _timeCursorPosition = time / (float)TimeDivisionDuration * TimeDivisionWidth;
            _timeCursor.Q("time-offset").style.left = _timeCursorPosition;
            
            foreach (var track in _tracks)
            {
                track.SetCurrentTime(time);
            }
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
                    var trackScroller = track.GetHorizontalScroller();
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
                    var trackScroller = track.GetHorizontalScroller();
                    trackScroller.lowValue = 0;
                    trackScroller.highValue = hiddenWidth;
                }
            }
        }

        public void Repaint()
        {
            UpdateTimeScale();
            
            foreach (var track in _tracks)
            {
                track.Repaint();
            }
        }

        private void OnScroll(ChangeEvent<float> evt)
        {
            UpdateTimeScale();
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
            
            foreach (var track in _tracks)
            {
                track.SetDuration(Duration);
                track.SetTimeDivisionDuration(TimeDivisionDuration);
                track.SetTimeDivisionWidth(TimeDivisionWidth);
                track.SetTicksPerDivision(TicksPerDivision);
            }
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
            }
        }
    }
}