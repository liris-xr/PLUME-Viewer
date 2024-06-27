using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class TimeRangeElement : VisualElement
    {
        private static readonly StyleSheet StyleSheet;
        private readonly TimeFieldElement _endTimeField;

        private readonly MinMaxSlider _slider;
        private readonly TimeFieldElement _startTimeField;
        private ulong _endTime;
        private ulong _highLimit;

        private ulong _lowLimit;
        private ulong _startTime;
        public EventCallback<ChangeEvent<ulong>> endTimeChanged;

        public EventCallback<ChangeEvent<ulong>> startTimeChanged;

        static TimeRangeElement()
        {
            StyleSheet = Resources.Load<StyleSheet>("UI/Styles/time_range");
        }

        public TimeRangeElement()
        {
            styleSheets.Add(StyleSheet);

            var mainContainer = new VisualElement { name = "container" };
            var timeFields = new VisualElement { name = "time-fields" };

            _startTimeField = new TimeFieldElement { name = "time-fields__start" };
            _endTimeField = new TimeFieldElement { name = "time-fields__end" };
            var toLabel = new Label("to");
            toLabel.AddToClassList(".unity-base-field__label");

            timeFields.Add(_startTimeField);
            timeFields.Add(toLabel);
            timeFields.Add(_endTimeField);

            _slider = new MinMaxSlider("") { name = "time-slider" };

            mainContainer.Add(timeFields);
            mainContainer.Add(_slider);

            Add(mainContainer);

            _startTimeField.timeChanged += OnStartTimeFieldValueChanged;
            _endTimeField.timeChanged += OnEndTimeFieldValueChanged;
            _slider.RegisterValueChangedCallback(OnSliderValueChanged);
        }

        public ulong LowLimit
        {
            get => _lowLimit;
            set
            {
                _lowLimit = value <= _highLimit
                    ? value
                    : throw new ArgumentException("lowLimit is greater than highLimit");

                _startTime = Math.Clamp(_startTime, value, HighLimit);
                _endTime = Math.Clamp(_endTime, value, HighLimit);

                _slider.lowLimit = value;
                _startTimeField.SetTimeWithoutNotify(_startTime);
                _endTimeField.SetTimeWithoutNotify(_endTime);
            }
        }

        public ulong HighLimit
        {
            get => _highLimit;
            set
            {
                _highLimit = value >= _lowLimit
                    ? value
                    : throw new ArgumentException("highLimit is lower than lowLimit");

                _startTime = Math.Clamp(_startTime, LowLimit, value);
                _endTime = Math.Clamp(_endTime, LowLimit, value);

                _slider.highLimit = value;
                _startTimeField.SetTimeWithoutNotify(_startTime);
                _endTimeField.SetTimeWithoutNotify(_endTime);
            }
        }

        public ulong StartTime
        {
            get => _startTime;
            set
            {
                _startTime = Math.Clamp(value, LowLimit, _endTime);
                _slider.SetValueWithoutNotify(new Vector2(_startTime, _endTime));
                _startTimeField.SetTimeWithoutNotify(_startTime);
            }
        }

        public ulong EndTime
        {
            get => _endTime;
            set
            {
                _endTime = Math.Clamp(value, _startTime, HighLimit);
                _slider.SetValueWithoutNotify(new Vector2(_startTime, _endTime));
                _endTimeField.SetTimeWithoutNotify(_endTime);
            }
        }

        private void OnStartTimeFieldValueChanged(ChangeEvent<ulong> evt)
        {
            StartTime = evt.newValue;
        }

        private void OnEndTimeFieldValueChanged(ChangeEvent<ulong> evt)
        {
            EndTime = evt.newValue;
        }

        private void OnSliderValueChanged(ChangeEvent<Vector2> evt)
        {
            StartTime = (ulong)evt.newValue.x;
            EndTime = (ulong)evt.newValue.y;
        }

        public void Reset()
        {
            StartTime = LowLimit;
            EndTime = HighLimit;
        }

        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimeRangeElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlUnsignedLongAttributeDescription _endValue = new()
                { name = "end-value", defaultValue = 1_000_000_000u };

            private readonly UxmlUnsignedLongAttributeDescription _highLimit = new()
                { name = "high-limit", defaultValue = 1_000_000_000u };

            private readonly UxmlUnsignedLongAttributeDescription _lowLimit = new()
                { name = "low-limit", defaultValue = 0u };

            private readonly UxmlUnsignedLongAttributeDescription _startValue = new()
                { name = "start-value", defaultValue = 0u };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as TimeRangeElement;
                ele.HighLimit = _highLimit.GetValueFromBag(bag, cc);
                ele.LowLimit = _lowLimit.GetValueFromBag(bag, cc);
                ele.EndTime = _endValue.GetValueFromBag(bag, cc);
                ele.StartTime = _startValue.GetValueFromBag(bag, cc);
            }
        }
    }
}