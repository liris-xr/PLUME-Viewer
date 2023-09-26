using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimelineTrackElement : VisualElement
    {
        private static readonly VisualTreeAsset Uxml;

        private const ulong DurationDefault = 1_000_000_000u;
        private const ulong TimeDivisionDurationDefault = 100_000_000u;
        private const float TimeDivisionWidthDefault = 100;
        private const int TicksPerDivisionDefault = 10;

        static TimelineTrackElement()
        {
            Uxml = Resources.Load<VisualTreeAsset>("UI/Uxml/timeline_track");
        }

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimelineTrackElement, UxmlTraits>
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
                var ele = ve as TimelineTrackElement;
                ele.Duration = _duration.GetValueFromBag(bag, cc);
                ele.TimeDivisionDuration = _timeDivisionDuration.GetValueFromBag(bag, cc);
                ele.TicksPerDivision = _ticksPerDivision.GetValueFromBag(bag, cc);
                ele.TimeDivisionWidth = _timeDivisionWidth.GetValueFromBag(bag, cc);
            }
        }

        private readonly Scroller _horizontalScroller;

        public ulong Duration;
        public ulong TimeDivisionDuration;
        public int TicksPerDivision;
        public float TimeDivisionWidth;
        
        public TimelineTrackElement()
        {
            var track = Uxml.Instantiate().Q("track");
            hierarchy.Add(track);

            _horizontalScroller = track.Q<Scroller>("horizontal-scroller");
        }
    }
}