using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimeScaleElement : VisualElement
    {
        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;
        private const int TicksPerDivisionDefault = 10;

        private const int MinorTickWidthDefault = 1;
        private const int MinorTickHeightDefault = 10;
        private const int MajorTickWidthDefault = 1;
        private const int MajorTickHeightDefault = 20;
        private static readonly Color MajorTickColorDefault = new(0.6f, 0.6f, 0.6f, 1);
        private static readonly Color MinorTickColorDefault = new(0.4f, 0.4f, 0.4f, 1);
        private static readonly Color DisabledTickColorDefault = new(0.25f, 0.25f, 0.25f, 1);

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

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimeScaleElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlUnsignedLongAttributeDescription Duration = new()
                { name = "duration", defaultValue = DurationDefault };

            private readonly UxmlUnsignedLongAttributeDescription TimeDivisionDuration = new()
                { name = "time-division-duration", defaultValue = TimeDivisionDurationDefault };

            private readonly UxmlIntAttributeDescription TicksPerDivision = new()
                { name = "ticks-per-division", defaultValue = TicksPerDivisionDefault };

            private readonly UxmlFloatAttributeDescription TimeDivisionWidth = new()
                { name = "time-division-width", defaultValue = TimeDivisionWidthDefault };

            private readonly UxmlIntAttributeDescription MinorTickWidth = new()
                { name = "minor-tick-width", defaultValue = MinorTickWidthDefault };

            private readonly UxmlIntAttributeDescription MinorTickHeight = new()
                { name = "minor-tick-height", defaultValue = MinorTickHeightDefault };

            private readonly UxmlIntAttributeDescription MajorTickWidth = new()
                { name = "major-tick-width", defaultValue = MajorTickWidthDefault };

            private readonly UxmlIntAttributeDescription MajorTickHeight = new()
                { name = "major-tick-height", defaultValue = MajorTickHeightDefault };

            private readonly UxmlColorAttributeDescription MinorTickColor = new()
                { name = "minor-tick-color", defaultValue = MinorTickColorDefault };

            private readonly UxmlColorAttributeDescription MajorTickColor = new()
                { name = "major-tick-color", defaultValue = MajorTickColorDefault };

            private readonly UxmlColorAttributeDescription DisabledTickColor = new()
                { name = "disabled-tick-color", defaultValue = DisabledTickColorDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimeScaleElement;
                ele.Duration = Duration.GetValueFromBag(bag, cc);
                ele.TimeDivisionDuration = TimeDivisionDuration.GetValueFromBag(bag, cc);
                ele.TicksPerDivision = TicksPerDivision.GetValueFromBag(bag, cc);
                ele.TimeDivisionWidth = TimeDivisionWidth.GetValueFromBag(bag, cc);

                ele.MinorTickWidth = MinorTickWidth.GetValueFromBag(bag, cc);
                ele.MinorTickHeight = MinorTickHeight.GetValueFromBag(bag, cc);
                ele.MajorTickWidth = MajorTickWidth.GetValueFromBag(bag, cc);
                ele.MajorTickHeight = MajorTickHeight.GetValueFromBag(bag, cc);
                ele.MinorTickColor = MinorTickColor.GetValueFromBag(bag, cc);
                ele.MajorTickColor = MajorTickColor.GetValueFromBag(bag, cc);
                ele.DisabledTickColor = DisabledTickColor.GetValueFromBag(bag, cc);
            }
        }

        private ulong Duration;
        private ulong TimeDivisionDuration;
        private int TicksPerDivision;
        private float TimeDivisionWidth;

        private int MinorTickWidth;
        private int MinorTickHeight;
        private int MajorTickWidth;
        private int MajorTickHeight;
        private Color MinorTickColor;
        private Color MajorTickColor;
        private Color DisabledTickColor;

        public TimeScaleElement()
        {
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
                var pixelsToDuration = TimeDivisionDuration / TimeDivisionWidth;
                var time = (ulong)((mousePos.x - resolvedStyle.paddingLeft) * pixelsToDuration);
                dragged?.Invoke(time);
            }
        }

        private void OnMouseClick(IMouseEvent evt)
        {
            var leftButtonPressed = evt.button == 0;

            if (leftButtonPressed) // left button
            {
                var mousePos = evt.localMousePosition;
                var pixelsToDuration = TimeDivisionDuration / TimeDivisionWidth;
                var time = (ulong)((mousePos.x - resolvedStyle.paddingLeft) * pixelsToDuration);
                clicked?.Invoke(time);
            }
        }

        public void SetDuration(ulong duration)
        {
            Duration = duration;
        }

        public void SetTimeDivisionDuration(ulong timeDivisionDuration)
        {
            TimeDivisionDuration = timeDivisionDuration;
        }

        public void SetTicksPerDivision(int ticksPerDivision)
        {
            TicksPerDivision = ticksPerDivision;
        }

        public void SetTimeDivisionWidth(float timeDivisionWidth)
        {
            TimeDivisionWidth = timeDivisionWidth;
        }

        public void SetTicksClippingRect(Rect ticksClippingRect)
        {
            _ticksClippingRect = ticksClippingRect;
        }

        public void Repaint()
        {
            style.minWidth = Duration / (float) TimeDivisionDuration * TimeDivisionWidth
                                        + resolvedStyle.paddingLeft + resolvedStyle.paddingRight;
            
            GenerateTimescaleLabels();
            _timeScaleTicks.MarkDirtyRepaint();
        }

        private void GenerateTimescaleLabels()
        {
            _timeLabelsContainer.Clear();

            var timeScaleTicksRect = _timeScaleTicks.contentRect;

            var nDivisions = timeScaleTicksRect.width / TimeDivisionWidth;
            var nTicks = Mathf.CeilToInt(nDivisions * TicksPerDivision);
            var tickSpacing = TimeDivisionWidth / TicksPerDivision;

            for (var tickIndex = 0u; tickIndex <= nTicks; ++tickIndex)
            {
                var isMajorTick = tickIndex % TicksPerDivision == 0;

                if (!isMajorTick)
                    continue;

                var divisionIdx = tickIndex / TicksPerDivision;

                var tickRect = new Rect();
                tickRect.x = tickIndex * tickSpacing - MajorTickWidth / 2f;
                tickRect.y = timeScaleTicksRect.height - MajorTickHeight;
                tickRect.width = MajorTickWidth;
                tickRect.height = MajorTickHeight;

                if (!IsTickVisible(tickRect))
                {
                    continue;
                }

                var time = TimeDivisionDuration * (ulong)divisionIdx; // time in nanoseconds
                var disabled = time > Duration;
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

                var nDivisions = timeScaleTicksRect.width / TimeDivisionWidth;
                var nTicks = Mathf.CeilToInt(nDivisions * TicksPerDivision);
                var tickSpacing = TimeDivisionWidth / TicksPerDivision;
                var timePerTick = TimeDivisionDuration / (double)TicksPerDivision;

                var verticesList = new List<Vertex>();
                var indicesList = new List<ushort>();

                for (var tickIndex = 0u; tickIndex <= nTicks; ++tickIndex)
                {
                    var isMajorTick = tickIndex % TicksPerDivision == 0;

                    // Time in nanoseconds
                    var tickTime = tickIndex * timePerTick;
                    var tickColor = tickTime > Duration
                        ? DisabledTickColor
                        : (isMajorTick ? MajorTickColor : MinorTickColor);
                    var tickWidth = isMajorTick ? MajorTickWidth : MinorTickWidth;
                    var tickHeight = isMajorTick ? MajorTickHeight : MinorTickHeight;

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
                    indicesList.Add((ushort)(vertexIndexOffset + 2));
                    indicesList.Add((ushort)(vertexIndexOffset + 1));
                    indicesList.Add((ushort)(vertexIndexOffset + 0));
                    indicesList.Add((ushort)(vertexIndexOffset + 3));
                    indicesList.Add((ushort)(vertexIndexOffset + 2));
                    indicesList.Add((ushort)(vertexIndexOffset + 0));
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