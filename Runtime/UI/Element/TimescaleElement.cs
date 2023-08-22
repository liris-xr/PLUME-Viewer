using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimeScaleElement : VisualElement
    {
        private const int MinorTickWidthDefault = 1;
        private const int MinorTickHeightDefault = 10;
        private const int MajorTickWidthDefault = 1;
        private const int MajorTickHeightDefault = 20;
        private static readonly Color MajorTickColorDefault = new(0.6f, 0.6f, 0.6f, 1);
        private static readonly Color MinorTickColorDefault = new(0.4f, 0.4f, 0.4f, 1);
        private static readonly Color DisabledTickColorDefault = new(0.25f, 0.25f, 0.25f, 1);

        private readonly int _minorTickWidth;
        private readonly int _minorTickHeight;
        private readonly int _majorTickWidth;
        private readonly int _majorTickHeight;
        private readonly Color _minorTickColor;
        private readonly Color _majorTickColor;
        private readonly Color _disabledTickColor;

        private ulong _duration;
        private ulong _timeDivisionDuration;
        private int _ticksPerDivision;
        private float _timeDivisionWidth;

        private readonly VisualElement _timeScaleTicks;
        private readonly VisualElement _timeLabelsContainer;

        private Rect? _ticksClippingRect;

        public Action<ulong> clicked;
        public Action<ulong> dragged;
        
        private static readonly StyleSheet StyleSheet;

        static TimeScaleElement()
        {
            StyleSheet = Resources.Load<StyleSheet>("UI/Styles/time_scale");
        }

        public TimeScaleElement(ulong duration, ulong timeDivisionDuration, int ticksPerDivision,
            float timeDivisionWidth) : this(
            MinorTickWidthDefault, MinorTickHeightDefault,
            MajorTickWidthDefault, MajorTickHeightDefault,
            MinorTickColorDefault, MajorTickColorDefault, DisabledTickColorDefault,
            duration, timeDivisionDuration, ticksPerDivision, timeDivisionWidth)
        {
        }

        public TimeScaleElement(int minorTickWidth, int minorTickHeight, int majorTickWidth, int majorTickHeight,
            Color minorTickColor, Color majorTickColor, Color disabledTickColor, ulong duration,
            ulong timeDivisionDuration, int ticksPerDivision, float timeDivisionWidth)
        {
            _minorTickWidth = minorTickWidth;
            _minorTickHeight = minorTickHeight;
            _majorTickWidth = majorTickWidth;
            _majorTickHeight = majorTickHeight;
            _minorTickColor = minorTickColor;
            _majorTickColor = majorTickColor;
            _disabledTickColor = disabledTickColor;
            _duration = duration;
            _timeDivisionDuration = timeDivisionDuration;
            _ticksPerDivision = ticksPerDivision;
            _timeDivisionWidth = timeDivisionWidth;

            styleSheets.Add(StyleSheet);

            _timeLabelsContainer = new VisualElement();
            _timeScaleTicks = new VisualElement();
            _timeScaleTicks.generateVisualContent += GenerateTimescaleTicks;

            Add(_timeLabelsContainer);
            Add(_timeScaleTicks);

            _timeLabelsContainer.AddToClassList("time-scale__labels");
            _timeScaleTicks.AddToClassList("time-scale__ticks");
            AddToClassList("time-scale");

            RegisterCallback<GeometryChangedEvent>(_ => Repaint());

            RegisterCallback<PointerMoveEvent>(OnMouseMove);
            
            this.AddManipulator(new Clickable(evt =>
            {
                if (evt is IMouseEvent e)
                {
                    OnMouseClick(e);
                }
            }));
        }

        private void OnMouseMove(PointerMoveEvent evt)
        {
            var leftButtonPressed = (evt.pressedButtons & 0x1) > 0;
            
            if (leftButtonPressed) // left button
            {
                var mousePos = evt.localPosition;
                var pixelsToDuration = _timeDivisionDuration / _timeDivisionWidth;
                var time = (ulong) ((mousePos.x - resolvedStyle.paddingLeft) * pixelsToDuration);
                dragged?.Invoke(time);
            }
        }

        private void OnMouseClick(IMouseEvent evt)
        {
            var leftButtonPressed = evt.button == 0;
            
            if (leftButtonPressed) // left button
            {
                var mousePos = evt.localMousePosition;
                var pixelsToDuration = _timeDivisionDuration / _timeDivisionWidth;
                var time = (ulong) ((mousePos.x - resolvedStyle.paddingLeft) * pixelsToDuration);
                clicked?.Invoke(time);
            }
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

        public void SetTicksClippingRect(Rect ticksClippingRect)
        {
            _ticksClippingRect = ticksClippingRect;
        }

        public void Repaint()
        {
            GenerateTimescaleLabels();
            _timeScaleTicks.MarkDirtyRepaint();
        }

        private void GenerateTimescaleLabels()
        {
            _timeLabelsContainer.Clear();

            var timeScaleTicksRect = _timeScaleTicks.contentRect;

            var nDivisions = timeScaleTicksRect.width / _timeDivisionWidth;
            var nTicks = Mathf.CeilToInt(nDivisions * _ticksPerDivision);
            var tickSpacing = _timeDivisionWidth / _ticksPerDivision;

            for (var tickIndex = 0u; tickIndex <= nTicks; ++tickIndex)
            {
                var isMajorTick = tickIndex % _ticksPerDivision == 0;

                if (!isMajorTick)
                    continue;

                var divisionIdx = tickIndex / _ticksPerDivision;

                var tickRect = new Rect();
                tickRect.x = tickIndex * tickSpacing - _majorTickWidth / 2f;
                tickRect.y = timeScaleTicksRect.height - _majorTickHeight;
                tickRect.width = _majorTickWidth;
                tickRect.height = _majorTickHeight;

                if (!IsTickVisible(tickRect))
                {
                    continue;
                }

                var time = _timeDivisionDuration * (ulong) divisionIdx; // time in nanoseconds
                var disabled = time > _duration;
                var timeStr = TimeSpan.FromMilliseconds(time / 1_000_000.0).ToString(@"hh\:mm\:ss\.fff");

                var timeLabelContainer = new VisualElement();
                timeLabelContainer.AddToClassList("time-label-container");

                var timeLabel = new Label();
                timeLabel.AddToClassList("time-label");
                timeLabel.text = timeStr;

                if (disabled)
                {
                    timeLabel.AddToClassList("time-label--disabled");
                }

                timeLabelContainer.style.position = new StyleEnum<Position>(Position.Absolute);
                timeLabelContainer.style.left = tickIndex * tickSpacing;
                timeLabelContainer.Add(timeLabel);

                _timeLabelsContainer.Add(timeLabelContainer);
            }
        }

        private bool IsTickVisible(Rect tickRect)
        {
            return !_ticksClippingRect.HasValue || _ticksClippingRect.Value.Overlaps(tickRect);
        }

        private void GenerateTimescaleTicks(MeshGenerationContext ctx)
        {
            try
            {
                var timeScaleTicksRect = _timeScaleTicks.contentRect;

                var nDivisions = timeScaleTicksRect.width / _timeDivisionWidth;
                var nTicks = Mathf.CeilToInt(nDivisions * _ticksPerDivision);
                var tickSpacing = _timeDivisionWidth / _ticksPerDivision;
                var timePerTick = _timeDivisionDuration / (double) _ticksPerDivision;

                var verticesList = new List<Vertex>();
                var indicesList = new List<ushort>();

                for (var tickIndex = 0u; tickIndex <= nTicks; ++tickIndex)
                {
                    var isMajorTick = tickIndex % _ticksPerDivision == 0;

                    // Time in nanoseconds
                    var tickTime = tickIndex * timePerTick;
                    var tickColor = tickTime > _duration
                        ? _disabledTickColor
                        : (isMajorTick ? _majorTickColor : _minorTickColor);
                    var tickWidth = isMajorTick ? _majorTickWidth : _minorTickWidth;
                    var tickHeight = isMajorTick ? _majorTickHeight : _minorTickHeight;

                    var tickRect = new Rect();
                    tickRect.x = tickIndex * tickSpacing - tickWidth / 2f;
                    tickRect.y = timeScaleTicksRect.height - tickHeight;
                    tickRect.width = tickWidth;
                    tickRect.height = tickHeight;

                    if (!IsTickVisible(tickRect))
                    {
                        continue;
                    }

                    var vertexIndexOffset = verticesList.Count;
                    var v0 = new Vertex();
                    var v1 = new Vertex();
                    var v2 = new Vertex();
                    var v3 = new Vertex();
                    v0.position = tickRect.position + new Vector2(0, 0);
                    v1.position = tickRect.position + new Vector2(0, tickRect.height);
                    v2.position = tickRect.position + new Vector2(tickRect.width, tickRect.height);
                    v3.position = tickRect.position + new Vector2(tickRect.width, 0);
                    v0.tint = tickColor;
                    v1.tint = tickColor;
                    v2.tint = tickColor;
                    v3.tint = tickColor;
                    verticesList.Add(v0);
                    verticesList.Add(v1);
                    verticesList.Add(v2);
                    verticesList.Add(v3);
                    indicesList.Add((ushort) (vertexIndexOffset + 2));
                    indicesList.Add((ushort) (vertexIndexOffset + 1));
                    indicesList.Add((ushort) (vertexIndexOffset + 0));
                    indicesList.Add((ushort) (vertexIndexOffset + 3));
                    indicesList.Add((ushort) (vertexIndexOffset + 2));
                    indicesList.Add((ushort) (vertexIndexOffset + 0));
                }

                if (verticesList.Count > 0)
                {
                    var mesh = ctx.Allocate(verticesList.Count, indicesList.Count);
                    mesh.SetAllVertices(verticesList.ToArray());
                    mesh.SetAllIndices(indicesList.ToArray());
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}