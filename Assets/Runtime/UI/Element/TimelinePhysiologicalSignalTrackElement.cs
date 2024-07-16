using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace PLUME.UI.Element
{
    public class TimelinePhysiologicalSignalTrackElement : VisualElement
    {
        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimelinePhysiologicalSignalTrackElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlUnsignedLongAttributeDescription _duration = new()
                { name = "duration", defaultValue = DurationDefault };

            private readonly UxmlUnsignedLongAttributeDescription _timeDivisionDuration = new()
                { name = "time-division-duration", defaultValue = TimeDivisionDurationDefault };

            private readonly UxmlFloatAttributeDescription _timeDivisionWidth = new()
                { name = "time-division-width", defaultValue = TimeDivisionWidthDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimelinePhysiologicalSignalTrackElement;
                ele._duration = _duration.GetValueFromBag(bag, cc);
                ele._timeDivisionDuration = _timeDivisionDuration.GetValueFromBag(bag, cc);
                ele._timeDivisionWidth = _timeDivisionWidth.GetValueFromBag(bag, cc);
            }
        }

        private readonly Scroller _horizontalScroller;
        private readonly VisualElement _trackContent;
        private readonly VisualElement _trackContentContainer;
        private readonly VisualElement _colorPane;

        private readonly List<VisualElement> _curveCanvases = new();
        
        private Color _channelColor = Color.red;

        private readonly Label _nameLabel;
        private readonly Label _frequencyLabel;
        private readonly Label _channelLabel;

        private readonly Label _minValueLabel;
        private readonly Label _maxValueLabel;
        private readonly Label _currentValueLabel;

        private ulong _duration;
        private ulong _timeDivisionDuration;
        private float _timeDivisionWidth;

        private List<Vector2> _points = new();

        private Rect? _clippingRect;

        public TimelinePhysiologicalSignalTrackElement()
        {
            var uxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline_physio_track");
            var track = uxml.Instantiate().Q("track");
            hierarchy.Add(track);

            _horizontalScroller = track.Q<ScrollView>("track-content-container").horizontalScroller;

            _colorPane = track.Q("color");

            _trackContent = track.Q("track-content");
            _trackContentContainer = track.Q("track-content-container");
            _nameLabel = track.Q("track-header").Q<Label>("name");
            _frequencyLabel = track.Q("track-header").Q<Label>("frequency");
            _channelLabel = track.Q("track-header").Q<Label>("channel");

            _minValueLabel = track.Q("track-header").Q<Label>("min-value");
            _maxValueLabel = track.Q("track-header").Q<Label>("max-value");
            _currentValueLabel = track.Q("track-header").Q<Label>("current-value");
        }

        public void SetMinValue(float value)
        {
            _minValueLabel.text = $"Min: {value:0.000}";
        }

        public void SetMaxValue(float value)
        {
            _maxValueLabel.text = $"Max: {value:0.000}";
        }

        public void SetCurrentTime(ulong time)
        {
            Vector2? nearestValue;

            try
            {
                nearestValue = _points?.Last(v => v.x < time);
            }
            catch
            {
                nearestValue = null;
            }

            _currentValueLabel.text = nearestValue == null ? "Nearest: N/A" : $"Nearest: {nearestValue.Value.y:0.000}";
        }

        public void SetStreamColor(Color color)
        {
            _colorPane.style.backgroundColor = color;
        }

        public void SetChannelColor(Color color)
        {
            _channelColor = color;
            
            foreach (var canvas in _curveCanvases)
                canvas.MarkDirtyRepaint();
        }

        public void SetName(string name)
        {
            _nameLabel.text = name;
        }

        public void SetFrequency(float frequency)
        {
            _frequencyLabel.text = $"Frequency: {frequency:0.00}Hz";
        }

        public void SetChannel(int channel)
        {
            _channelLabel.text = $"(Channel {channel})";
        }

        public void SetPoints(List<Vector2> points)
        {
            _points = points;
            
            const int maxPointsPerCanvas = 5000;
            
            foreach (var t in _curveCanvases)
                _trackContent.Remove(t);
            
            for (var i = 0; i < _points.Count; i += maxPointsPerCanvas)
            {
                var canvas = new VisualElement();
                canvas.style.flexShrink = 0;
                canvas.style.flexGrow = 1;
                canvas.style.width = Length.Percent(100);
                canvas.style.height = Length.Percent(100);
                canvas.style.position = Position.Absolute;
                canvas.style.left = 0;
                canvas.style.top = 0;
                var offset = i;
                canvas.generateVisualContent += mgc => DrawPartialCurve(mgc, offset, Math.Min(_points.Count - offset, maxPointsPerCanvas));
                _curveCanvases.Add(canvas);
                _trackContent.Add(canvas);
            }
            
            Repaint();
        }

        private void RecalculateSize()
        {
            _trackContent.style.minWidth = Duration / (float)TimeDivisionDuration * TimeDivisionWidth;
            _horizontalScroller.lowValue = 0;
            _horizontalScroller.highValue = Duration / (float)TimeDivisionDuration * TimeDivisionWidth;
            Repaint();
        }

        public void Repaint()
        {
            foreach (var canvas in _curveCanvases)
                canvas.MarkDirtyRepaint();
        }

        private (long, long) GetVisibleTimeRange()
        {
            var lowTime = (long) (_horizontalScroller.value / _timeDivisionWidth * _timeDivisionDuration);
            var highTime = (long) (lowTime + _trackContentContainer.layout.width / _timeDivisionWidth * _timeDivisionDuration);
            return (lowTime, highTime);
        }
        
        private void DrawPartialCurve(MeshGenerationContext mgc, int offset, int nPoints)
        {
            if (_points.Count <= 1)
                return;
            
            // TODO: handle the case where minY == maxY
            var minY = _points.Min(v => v.y);
            var maxY = _points.Max(v => v.y);
            var canvasHeight = mgc.visualElement.layout.height;
            
            var (minTimeVisible, maxTimeVisible) = GetVisibleTimeRange();

            var curvePoints = _points.GetRange(offset, nPoints);
            var visiblePoints = curvePoints.Where(p => p.x >= minTimeVisible && p.x <= maxTimeVisible).ToList();
            
            if (visiblePoints.Count == 0)
                return;
            
            var painter2D = mgc.painter2D;
            painter2D.lineWidth = 2.0f;
            painter2D.strokeColor = _channelColor;
            painter2D.lineJoin = LineJoin.Round;
            painter2D.lineCap = LineCap.Round;
            
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(visiblePoints[0].x / _timeDivisionDuration * _timeDivisionWidth,
                canvasHeight - (visiblePoints[0].y - minY) / (maxY - minY) * canvasHeight));
            
            for (var i = 0; i < visiblePoints.Count; i++)
            {
                var x = visiblePoints[i].x / _timeDivisionDuration * _timeDivisionWidth;
                painter2D.LineTo(new Vector2(x,
                    canvasHeight - (visiblePoints[i].y - minY) / (maxY - minY) * canvasHeight));
            }
            
            painter2D.Stroke();
        }

        public void SetScrollOffset(float scrollOffset)
        {
            _horizontalScroller.value = scrollOffset;
        }

        public ulong Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                RecalculateSize();
            }
        }

        public ulong TimeDivisionDuration
        {
            get => _timeDivisionDuration;
            set
            {
                _timeDivisionDuration = value;
                RecalculateSize();
            }
        }

        public float TimeDivisionWidth
        {
            get => _timeDivisionWidth;
            set
            {
                _timeDivisionWidth = value;
                RecalculateSize();
            }
        }
    }
}