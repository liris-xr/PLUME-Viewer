using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class TimeFieldElement : VisualElement
    {
        [Preserve]
        public new class UxmlFactory : UxmlFactory<TimeFieldElement, UxmlTraits>
        {
        }

        private readonly TextField _timeTextField;
        private ulong _time;

        private static readonly string[] TimeFormats =
        {
            @"hh\:mm\:ss\.fff",
            @"hh\:mm\:ss\.ff",
            @"hh\:mm\:ss\.f",
            @"hh\:mm\:ss\.",
            @"hh\:mm\:ss",
            @"hh\:mm\:s",
            @"hh\:mm\"
        };

        public EventCallback<ChangeEvent<ulong>> timeChanged;

        public TimeFieldElement()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("UI/Styles/time_field"));

            _timeTextField = new TextField();
            _timeTextField.isDelayed = true;
            _timeTextField.RegisterValueChangedCallback(OnTimeValueChanged);
            Add(_timeTextField);
            AddToClassList("time-field");
        }

        public ulong GetTime()
        {
            return _time;
        }

        public void SetTimeWithoutNotify(ulong time)
        {
            var timeStr = TimeSpan.FromMilliseconds(time / 1_000_000.0).ToString(TimeFormats[0]);
            _timeTextField.SetValueWithoutNotify(timeStr);
            _time = time;
        }

        public bool IsFocused()
        {
            return focusController?.focusedElement == _timeTextField;
        }

        private void OnTimeValueChanged(ChangeEvent<string> evt)
        {
            if (TimeSpan.TryParseExact(evt.newValue, TimeFormats, null, out var timeSpan))
            {
                TimeSpan.TryParseExact(evt.previousValue, TimeFormats, null, out var prevTimeSpan);
                var prevTime = (ulong)(prevTimeSpan.TotalMilliseconds * 1_000_000);
                var newTime = (ulong)(timeSpan.TotalMilliseconds * 1_000_000);
                var timeChangedEvt = ChangeEvent<ulong>.GetPooled(prevTime, newTime);
                timeChanged?.Invoke(timeChangedEvt);

                if (!timeChangedEvt.isDefaultPrevented)
                {
                    _time = newTime;
                }
                else
                {
                    _timeTextField.SetValueWithoutNotify(evt.previousValue);
                    evt.PreventDefault();
                }
            }
            else
            {
                _timeTextField.SetValueWithoutNotify(evt.previousValue);
                evt.PreventDefault();
            }

            // Release focus
            _timeTextField.Blur();
        }
    }
}