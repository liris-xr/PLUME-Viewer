using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimelinePhysiologicalSignalTrackElement : VisualElement
    {
        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;
        private const int TicksPerDivisionDefault = 10;

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

            private readonly UxmlIntAttributeDescription _ticksPerDivision = new()
                { name = "ticks-per-division", defaultValue = TicksPerDivisionDefault };

            private readonly UxmlFloatAttributeDescription _timeDivisionWidth = new()
                { name = "time-division-width", defaultValue = TimeDivisionWidthDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimelinePhysiologicalSignalTrackElement;
                ele._duration = _duration.GetValueFromBag(bag, cc);
                ele._timeDivisionDuration = _timeDivisionDuration.GetValueFromBag(bag, cc);
                ele._ticksPerDivision = _ticksPerDivision.GetValueFromBag(bag, cc);
                ele._timeDivisionWidth = _timeDivisionWidth.GetValueFromBag(bag, cc);
            }
        }

        private readonly Scroller _horizontalScroller;
        private readonly VisualElement _canvas;
        private readonly VisualElement _trackContent;
        private readonly VisualElement _colorPane;

        private Color _channelColor = Color.red;
        
        private readonly Label _nameLabel;
        private readonly Label _frequencyLabel;
        private readonly Label _channelLabel;
        
        private readonly Label _minValueLabel;
        private readonly Label _maxValueLabel;
        private readonly Label _currentValueLabel;
        
        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
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
            _nameLabel = track.Q("track-header").Q<Label>("name");
            _frequencyLabel = track.Q("track-header").Q<Label>("frequency");
            _channelLabel = track.Q("track-header").Q<Label>("channel");
            
            _minValueLabel = track.Q("track-header").Q<Label>("min-value");
            _maxValueLabel = track.Q("track-header").Q<Label>("max-value");
            _currentValueLabel = track.Q("track-header").Q<Label>("current-value");
            
            _canvas = track.Q("canvas");
            _canvas.generateVisualContent += DrawCanvas;
        }

        public Scroller GetHorizontalScroller()
        {
            return _horizontalScroller;
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
            _canvas.MarkDirtyRepaint();
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
        
        public void SetDuration(ulong duration)
        {
            _duration = duration;
        }

        public void SetTimeDivisionDuration(ulong timeDivisionDuration)
        {
            _timeDivisionDuration = timeDivisionDuration;
        }

        public void SetTicksPerDivision(int ticksPerDivision)
        {
            _ticksPerDivision = ticksPerDivision;
        }

        public void SetTimeDivisionWidth(float timeDivisionWidth)
        {
            _timeDivisionWidth = timeDivisionWidth;
        }

        public void SetClippingRect(Rect clippingRect)
        {
            _clippingRect = clippingRect;
        }
        
        public void SetPoints(List<Vector2> points)
        {
            _points = points;
            _canvas.MarkDirtyRepaint();
        }

        public void Repaint()
        {
            _trackContent.style.minWidth = _duration / (float) _timeDivisionDuration * _timeDivisionWidth;
            _canvas.MarkDirtyRepaint();
        }
        
        private void DrawCanvas(MeshGenerationContext mgc)
        {
            if (_points.Count <= 1)
                return;
            
            // TODO: handle the case where minY == maxY
            var minY = _points.Min(v => v.y);
            var maxY = _points.Max(v => v.y);
            var canvasHeight = mgc.visualElement.layout.height;

            var painter2D = mgc.painter2D;
            painter2D.lineWidth = 2.0f;
            painter2D.strokeColor = _channelColor;
            painter2D.lineJoin = LineJoin.Round;
            painter2D.lineCap = LineCap.Round;
            
            painter2D.BeginPath();
            painter2D.MoveTo(new Vector2(_points[0].x / _timeDivisionDuration * _timeDivisionWidth,
                canvasHeight - (_points[0].y - minY) / (maxY - minY) * canvasHeight));

            for (var i = 0; i < _points.Count; i++)
            {
                var x = _points[i].x / _timeDivisionDuration * _timeDivisionWidth;
                painter2D.LineTo(new Vector2(x,
                    canvasHeight - (_points[i].y - minY) / (maxY - minY) * canvasHeight));
            }
            
            painter2D.Stroke();
        }
    }
}