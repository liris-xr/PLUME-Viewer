using PLUME.Sample.Unity.UI;
using TMPro;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class TMPTextPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case TMPTextCreate tmpTextCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextCreate.Id);
                    break;
                }
                case TMPTextDestroy tmpTextDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(tmpTextDestroy.Id);
                    break;
                }
                case TMPTextUpdate tmpTextUpdate:
                {
                    var tmpText = ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextUpdate.Id);

                    if (tmpTextUpdate.HasText) tmpText.text = tmpTextUpdate.Text;

                    if (tmpTextUpdate.Color != null) tmpText.color = tmpTextUpdate.Color.ToEngineType();

                    if (tmpTextUpdate.HasFontSize) tmpText.fontSize = tmpTextUpdate.FontSize;

                    if (tmpTextUpdate.HasFontStyle) tmpText.fontStyle = (FontStyles)tmpTextUpdate.FontStyle;

                    if (tmpTextUpdate.HasAutoSize) tmpText.enableAutoSizing = tmpTextUpdate.AutoSize;

                    if (tmpTextUpdate.HasFontSizeMin) tmpText.fontSizeMin = tmpTextUpdate.FontSizeMin;

                    if (tmpTextUpdate.HasFontSizeMax) tmpText.fontSizeMax = tmpTextUpdate.FontSizeMax;

                    if (tmpTextUpdate.HasCharacterSpacing) tmpText.characterSpacing = tmpTextUpdate.CharacterSpacing;

                    if (tmpTextUpdate.HasWordSpacing) tmpText.wordSpacing = tmpTextUpdate.WordSpacing;

                    if (tmpTextUpdate.HasLineSpacing) tmpText.lineSpacing = tmpTextUpdate.LineSpacing;

                    if (tmpTextUpdate.HasParagraphSpacing) tmpText.paragraphSpacing = tmpTextUpdate.ParagraphSpacing;

                    if (tmpTextUpdate.HasAlignment) tmpText.alignment = (TextAlignmentOptions)tmpTextUpdate.Alignment;

                    if (tmpTextUpdate.HasWrappingEnabled) tmpText.enableWordWrapping = tmpTextUpdate.WrappingEnabled;

                    if (tmpTextUpdate.HasOverflow) tmpText.overflowMode = (TextOverflowModes)tmpTextUpdate.Overflow;

                    if (tmpTextUpdate.HasHorizontalMapping)
                        tmpText.horizontalMapping = (TextureMappingOptions)tmpTextUpdate.HorizontalMapping;

                    if (tmpTextUpdate.HasVerticalMapping)
                        tmpText.verticalMapping = (TextureMappingOptions)tmpTextUpdate.VerticalMapping;

                    if (tmpTextUpdate.Margin != null) tmpText.margin = tmpTextUpdate.Margin.ToEngineType();

                    if (tmpTextUpdate.FontId != null)
                    {
                        tmpText.font = ctx.GetOrDefaultAssetByIdentifier<TMP_FontAsset>(tmpTextUpdate.FontId);
                        ctx.TryAddAssetIdentifierCorrespondence(tmpTextUpdate.FontId, tmpText.font);
                    }

                    break;
                }
            }
        }
    }
}