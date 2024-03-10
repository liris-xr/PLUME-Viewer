using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class AspectRatioContainerElement : VisualElement
    {
        [Preserve]
        public new class UxmlFactory : UxmlFactory<AspectRatioContainerElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription _aspectRatioWidth = new()
                { name = "aspect-ratio-width", defaultValue = 16 };

            private readonly UxmlIntAttributeDescription _aspectRatioHeight =
                new() { name = "aspect-ratio-height", defaultValue = 9 };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as AspectRatioContainerElement;
                ele.AspectRatioWidth = _aspectRatioWidth.GetValueFromBag(bag, cc);
                ele.AspectRatioHeight = _aspectRatioHeight.GetValueFromBag(bag, cc);
            }
        }

        public int AspectRatioWidth { get; set; }
        public int AspectRatioHeight { get; set; }

        public AspectRatioContainerElement()
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent e)
        {
            UpdateElements();
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent e)
        {
            UpdateElements();
        }

        private void UpdateElements()
        {
            var aspectRatio = AspectRatioWidth / (float)AspectRatioHeight;

            if (aspectRatio <= 0.0f)
            {
                style.paddingBottom = 0;
                style.paddingTop = 0;
                style.paddingLeft = 0;
                style.paddingRight = 0;
                Debug.LogError($"Invalid aspect ratio:{aspectRatio}");
                return;
            }

            if (float.IsNaN(resolvedStyle.width) || float.IsNaN(resolvedStyle.height))
            {
                return;
            }

            var currRatio = resolvedStyle.width / resolvedStyle.height;

            if (currRatio > aspectRatio)
            {
                var targetWidth = resolvedStyle.height * aspectRatio;
                style.paddingBottom = 0;
                style.paddingTop = 0;
                style.paddingLeft = (resolvedStyle.width - targetWidth) / 2;
                style.paddingRight = (resolvedStyle.width - targetWidth) / 2;
            }
            else
            {
                var targetHeight = resolvedStyle.width * 1 / aspectRatio;
                style.paddingLeft = 0;
                style.paddingRight = 0;
                style.paddingTop = (resolvedStyle.height - targetHeight) / 2;
                style.paddingBottom = (resolvedStyle.height - targetHeight) / 2;
            }
        }
    }
}