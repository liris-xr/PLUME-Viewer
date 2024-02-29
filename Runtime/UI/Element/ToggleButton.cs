using System;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class ToggleButton : Button
    {
        [Preserve]
        public new class UxmlFactory : UxmlFactory<ToggleButton, UxmlTraits>
        {
        }

        private bool _state;
        public Action<bool> toggled;

        public ToggleButton()
        {
            clicked += Toggle;
            UpdateClass();
        }

        public void Toggle()
        {
            _state = !_state;
            UpdateClass();
            toggled?.Invoke(_state);
        }

        private void UpdateClass()
        {
            RemoveFromClassList("toggle-btn--state1");
            RemoveFromClassList("toggle-btn--state2");
            AddToClassList(_state ? "toggle-btn--state2" : "toggle-btn--state1");
        }

        public bool GetState()
        {
            return _state;
        }

        public void SetState(bool state)
        {
            _state = state;
            UpdateClass();
            toggled?.Invoke(_state);
        }

        public void SetStateWithoutNotify(bool state)
        {
            _state = state;
            UpdateClass();
        }
    }
}