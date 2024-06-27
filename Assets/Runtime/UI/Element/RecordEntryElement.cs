using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class RecordEntryElement : VisualElement
    {
        public string RecordName { get; set; }
        public string RecordDuration { get; set; }
        public string RecordCreationDate { get; set; }

        [Preserve]
        public new class UxmlFactory : UxmlFactory<RecordEntryElement, UxmlTraits>
        {
        }

        [Preserve]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _recordCreationDate =
                new() { name = "record-created-at", defaultValue = "N/A" };

            private readonly UxmlStringAttributeDescription _recordDuration = new()
                { name = "record-duration", defaultValue = "N/A" };

            private readonly UxmlStringAttributeDescription _recordName = new()
                { name = "record-name", defaultValue = "N/A" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ele = ve as RecordEntryElement;
                ele.RecordName = _recordName.GetValueFromBag(bag, cc);
                ele.RecordDuration = _recordDuration.GetValueFromBag(bag, cc);
                ele.RecordCreationDate = _recordCreationDate.GetValueFromBag(bag, cc);
            }
        }
    }
}