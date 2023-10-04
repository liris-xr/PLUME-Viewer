using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UnityEngine.UI;
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
                { name = "duration", defaultValue = DurationDefault };

            private readonly UxmlUnsignedLongAttributeDescription _timeDivisionDuration = new()
                { name = "time-division-duration", defaultValue = TimeDivisionDurationDefault };

            private readonly UxmlIntAttributeDescription _ticksPerDivision = new()
                { name = "ticks-per-division", defaultValue = TicksPerDivisionDefault };

            private readonly UxmlFloatAttributeDescription _timeDivisionWidth = new()
                { name = "time-division-width", defaultValue = TimeDivisionWidthDefault };

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

        private readonly VisualElement _markersContainer;
        private readonly List<TimelineMarkerElement> _markers = new();

        private readonly MinMaxSlider _timelineScroller;
        private readonly ScrollView _timeScaleScrollView;

        private readonly VisualElement _tracksPlaceholder;
        private readonly VisualElement _tracksContainer;
        private readonly List<TimelinePhysiologicalSignalTrackElement> _tracks = new();

        private ulong _time;
        
        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
        private float _timeDivisionWidth;

        private const ulong MinimumVisibleDuration = 1_000_000_000u; // in ns

        public TimelineElement()
        {
            var timelineUxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline");
            var timeline = timelineUxml.Instantiate().Q("timeline");
            hierarchy.Add(timeline);
            
            _timeScaleScrollView = timeline.Q<ScrollView>("time-scale-scroll-view");

            _tracksPlaceholder = timeline.Q("tracks-placeholder");
            _tracksContainer = timeline.Q("tracks-container");

            _timeScale = timeline.Q<TimeScaleElement>("time-scale");

            _markersContainer = timeline.Q("markers-container");

            _timeCursor = timeline.Q("time-cursor");
            
            _timelineScroller = timeline.Q<MinMaxSlider>("timeline-scroller");
            _timelineScroller.lowLimit = 0;
            _timelineScroller.highLimit = 1;
            _timelineScroller.RegisterValueChangedCallback(evt =>
            {
                EnsureTimelineScrollerMinimumRange(evt);
                UpdateTimelineScroller();
            });
            
            UpdateTimelineScroller();
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void ShowTimePeriod(ulong from, ulong to)
        {
            if (to < from)
                throw new Exception($"{nameof(from)} can't be greater or equal to {nameof(to)}");
            _timelineScroller.minValue = from / (float) _duration;
            _timelineScroller.maxValue = to / (float) _duration;
        }

        public void AddMarker(TimelineMarkerElement markerElement)
        {
            markerElement.TimeDivisionWidth = TimeDivisionWidth;
            markerElement.TimeDivisionDuration = TimeDivisionDuration;
            
            _markers.Add(markerElement);
            _markersContainer.Add(markerElement);
        }
        
        public void ClearMarkers()
        {
            _markers.Clear();
            _markersContainer.Clear();
        }

        public void AddTrack(TimelinePhysiologicalSignalTrackElement physiologicalSignalTrack)
        {
            physiologicalSignalTrack.Duration = Duration;
            physiologicalSignalTrack.TimeDivisionWidth = TimeDivisionWidth;
            physiologicalSignalTrack.TimeDivisionDuration = TimeDivisionDuration;
            
            _tracks.Add(physiologicalSignalTrack);
            _tracksContainer.Add(physiologicalSignalTrack);
            _tracksPlaceholder.style.display = DisplayStyle.None;
            _tracksContainer.style.display = DisplayStyle.Flex;
        }
        
        public void ClearTracks()
        {
            _tracks.Clear();
            _tracksContainer.Clear();
            _tracksPlaceholder.style.display = DisplayStyle.Flex;
            _tracksContainer.style.display = DisplayStyle.None;
        }

        public void KeepTimeCursorInView()
        {
            var relTime = _time / (float) _duration;

            var relTimeRange = _timelineScroller.maxValue - _timelineScroller.minValue;
            if (relTime < _timelineScroller.minValue)
            {
                _timelineScroller.minValue = relTime;
                _timelineScroller.maxValue = relTime + relTimeRange;
            } else if (relTime > _timelineScroller.maxValue)
            {
                _timelineScroller.maxValue = relTime + relTimeRange;
                _timelineScroller.minValue = relTime;
            }
        }

        private void EnsureTimelineScrollerMinimumRange(ChangeEvent<Vector2> evt)
        {
            var min = evt.newValue.x;
            var max = evt.newValue.y;
            var range = max - min;

            if (range * _duration > MinimumVisibleDuration) return;
            
            var minimumRange = (float)MinimumVisibleDuration / _duration;

            // Scaling min
            if (Math.Abs(evt.previousValue.x - evt.newValue.x) > float.Epsilon)
            {
                min = max - minimumRange;
                _timelineScroller.minValue = min;
            }
            // Scaling max
            else if (Math.Abs(evt.previousValue.y - evt.newValue.y) > float.Epsilon)
            {
                max = min + minimumRange;
                _timelineScroller.maxValue = max;
            }
        }

        private void UpdateTimelineScroller()
        {
            var range = _timelineScroller.maxValue - _timelineScroller.minValue;
            var minTime = (ulong)(_timelineScroller.minValue * _duration);
            var timeRange = Math.Max(MinimumVisibleDuration, (ulong) (range * _duration));
            
            var timelineContentWidth = _timeScaleScrollView.contentViewport.contentRect.width;

            ulong roundingMagnitude;
            
            switch (timeRange)
            {
                // Round to 0.1ms when timeRange is in [0s, 10s[
                case < 10_000_000_000:
                    roundingMagnitude = 100_000_000;
                    break;
                // Round to 1s when timeRange is in [10s, 30min[
                case < 1_800_000_000_000:
                    roundingMagnitude = 1_000_000_000;
                    break;
                // Round to 10s when timeRange is in [30min, 1h[
                case < 3_600_000_000_000:
                    roundingMagnitude = 10_000_000_000;
                    break;
                // Round to 1min when timeRange is in [1h, 12h[
                case < 43_200_000_000_000:
                    roundingMagnitude = 60_000_000_000;
                    break;
                // Round to 10min when timeRange is in [12h, 1d[
                case < 86_400_000_000_000:
                    roundingMagnitude = 600_000_000_000;
                    break;
                default:
                    roundingMagnitude = 3_600_000_000_000;
                    break;
            }
            
            TimeDivisionDuration = timeRange / 10 - timeRange / 10 % roundingMagnitude;
            TimeDivisionWidth = timelineContentWidth / timeRange * TimeDivisionDuration;
            
            var scrollOffset = minTime / (float)TimeDivisionDuration * TimeDivisionWidth;
            _timeScaleScrollView.horizontalScroller.value = scrollOffset;
            _timeCursor.Q("scroll-offset").style.left = -scrollOffset;
            
            foreach (var marker in _markers)
            {
                marker.SetScrollOffset(scrollOffset);
            }

            foreach (var track in _tracks)
            {
                track.SetScrollOffset(scrollOffset);
            }

            UpdateTimescaleTicksClippingRect();
        }

        public void SetCurrentTime(ulong time)
        {
            _time = time;
            _timeCursor.Q("time-offset").style.left = time / (float)TimeDivisionDuration * TimeDivisionWidth;

            foreach (var track in _tracks)
            {
                track.SetCurrentTime(time);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateTimescaleTicksClippingRect();
            UpdateTimelineScroller();
        }

        private void UpdateTimescaleScrollerLimits()
        {
            _timeScaleScrollView.horizontalScroller.lowValue = 0;
            _timeScaleScrollView.horizontalScroller.highValue = Duration / (float) TimeDivisionDuration * TimeDivisionWidth;
        }
        
        private void UpdateTimescaleTicksClippingRect()
        {
            var clippingRect = _timeScaleScrollView.contentViewport.contentRect;
            clippingRect.x += _timeScaleScrollView.scrollOffset.x;
            _timeScale.TicksClippingRect = clippingRect;
        }

        public ulong Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                _timeScale.Duration = value;

                foreach (var track in _tracks)
                {
                    track.Duration = value;
                }
                
                UpdateTimescaleScrollerLimits();
            }
        }

        public ulong TimeDivisionDuration
        {
            get => _timeDivisionDuration;
            set
            {
                _timeDivisionDuration = value;
                _timeScale.TimeDivisionDuration = value;

                foreach (var track in _tracks)
                {
                    track.TimeDivisionDuration = value;
                }

                foreach (var marker in _markers)
                {
                    marker.TimeDivisionDuration = value;
                }
                
                UpdateTimescaleScrollerLimits();
            }
        }

        public float TimeDivisionWidth
        {
            get => _timeDivisionWidth;
            set
            {
                _timeDivisionWidth = value;
                _timeScale.TimeDivisionWidth = value;

                foreach (var track in _tracks)
                {
                    track.TimeDivisionWidth = value;
                }

                foreach (var marker in _markers)
                {
                    marker.TimeDivisionWidth = value;
                }
                
                UpdateTimescaleScrollerLimits();
            }
        }
        
        public int TicksPerDivision
        {
            get => _ticksPerDivision;
            set
            {
                _ticksPerDivision = value;
                _timeScale.TicksPerDivision = value;
            }
        }
    }
}