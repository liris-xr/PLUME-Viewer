using System.Globalization;
using UnityEngine.UIElements;

namespace PLUME.UI.Element
{
    public class UxmlUnsignedLongAttributeDescription : TypedUxmlAttributeDescription<ulong>
    {
        public UxmlUnsignedLongAttributeDescription()
        {
            type = "ulong";
            typeNamespace = "http://www.w3.org/2001/XMLSchema";
            defaultValue = 0u;
        }

        public override string defaultValueAsString => defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat);

        public override ulong GetValueFromBag(IUxmlAttributes bag, CreationContext cc) =>
            GetValueFromBag(bag, cc, ConvertValueToUnsignedLong, defaultValue);

        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref ulong value) =>
            TryGetValueFromBag(bag, cc, ConvertValueToUnsignedLong, defaultValue, ref value);

        private static ulong ConvertValueToUnsignedLong(string v, ulong defaultValue)
        {
            return v == null || !ulong.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? defaultValue
                : result;
        }
    }
}