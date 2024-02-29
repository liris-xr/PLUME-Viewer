using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME
{
    public class CollapseBarElement : VisualElement
    {
        public enum CollapseBarElementOrientation
        {
            Vertical,
            ReversedVertical,
            Horizontal,
            ReversedHorizontal
        }

        [Preserve]
        public new class UxmlFactory : UxmlFactory<CollapseBarElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<CollapseBarElementOrientation> _orientation = new()
                { name = "orientation", defaultValue = CollapseBarElementOrientation.Vertical };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as CollapseBarElement;
                ele.Orientation = _orientation.GetValueFromBag(bag, cc);
                ele.UpdateToggleButton();
                ele.UpdateDirectionClass();
            }
        }

        public CollapseBarElementOrientation Orientation { get; set; }

        private readonly Button _toggleBtn;

        private static readonly StyleSheet StyleSheet;

        private bool _isCollapsed;

        public Action<bool> toggledCollapse;

        static CollapseBarElement()
        {
            StyleSheet = Resources.Load<StyleSheet>("UI/Styles/collapse_bar");
        }

        public CollapseBarElement()
        {
            styleSheets.Add(StyleSheet);

            _toggleBtn = new Button();
            _toggleBtn.AddToClassList("collapse-bar__btn");
            _toggleBtn.RegisterCallback<ClickEvent>(OnToggleButtonClick);

            AddToClassList("collapse-bar");
            Add(_toggleBtn);

            UpdateToggleButton();
            UpdateDirectionClass();
        }

        private void UpdateDirectionClass()
        {
            RemoveFromClassList("collapse-bar--vertical");
            RemoveFromClassList("collapse-bar--horizontal");
            RemoveFromClassList("collapse-bar--reversed-vertical");
            RemoveFromClassList("collapse-bar--reversed-horizontal");

            switch (Orientation)
            {
                case CollapseBarElementOrientation.Horizontal:
                    AddToClassList("collapse-bar--horizontal");
                    break;
                case CollapseBarElementOrientation.ReversedHorizontal:
                    AddToClassList("collapse-bar--reversed-horizontal");
                    break;
                case CollapseBarElementOrientation.Vertical:
                    AddToClassList("collapse-bar--vertical");
                    break;
                case CollapseBarElementOrientation.ReversedVertical:
                    AddToClassList("collapse-bar--reversed-vertical");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Collapse()
        {
            _isCollapsed = true;
            UpdateToggleButton();
            toggledCollapse?.Invoke(true);
        }

        public void Inflate()
        {
            _isCollapsed = false;
            UpdateToggleButton();
            toggledCollapse?.Invoke(false);
        }

        private void OnToggleButtonClick(ClickEvent evt)
        {
            _isCollapsed = !_isCollapsed;
            UpdateToggleButton();
            toggledCollapse?.Invoke(_isCollapsed);
        }

        private void UpdateToggleButton()
        {
            switch (Orientation)
            {
                case CollapseBarElementOrientation.Horizontal when !_isCollapsed:
                case CollapseBarElementOrientation.ReversedHorizontal when _isCollapsed:
                    _toggleBtn.text = "▲";
                    break;
                case CollapseBarElementOrientation.Horizontal when _isCollapsed:
                case CollapseBarElementOrientation.ReversedHorizontal when !_isCollapsed:
                    _toggleBtn.text = "▼";
                    break;
                case CollapseBarElementOrientation.Vertical when !_isCollapsed:
                case CollapseBarElementOrientation.ReversedVertical when _isCollapsed:
                    _toggleBtn.text = "◀";
                    break;
                case CollapseBarElementOrientation.Vertical when _isCollapsed:
                case CollapseBarElementOrientation.ReversedVertical when !_isCollapsed:
                    _toggleBtn.text = "▶";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}