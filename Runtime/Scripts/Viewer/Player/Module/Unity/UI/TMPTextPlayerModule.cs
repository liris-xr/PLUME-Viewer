using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using TMPro;

namespace PLUME.UI
{
    public class TMPTextPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case TMPTextCreate tmpTextCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextCreate.Id);
                    break;
                }
                case TMPTextUpdateColor tmpTextUpdateColor:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextUpdateColor.Id);
                    t.color = tmpTextUpdateColor.Color.ToEngineType();
                    break;
                }
                case TMPTextUpdateValue tmpTextUpdateValue:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextUpdateValue.Id);
                    t.text = tmpTextUpdateValue.Text;
                    break;
                }
                case TMPTextUpdateFont tmpTextUpdateFont:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextUpdateFont.Id);
                    t.font = ctx.GetOrDefaultAssetByIdentifier<TMP_FontAsset>(tmpTextUpdateFont.FontId);
                    t.fontStyle = (FontStyles) tmpTextUpdateFont.FontStyle;
                    t.fontSize = tmpTextUpdateFont.FontSize;
                    t.enableAutoSizing = tmpTextUpdateFont.AutoSize;
                    t.fontSizeMin = tmpTextUpdateFont.FontSizeMin;
                    t.fontSizeMax = tmpTextUpdateFont.FontSizeMax;
                    ctx.TryAddAssetIdentifierCorrespondence(tmpTextUpdateFont.FontId, t.font);
                    

                    break;
                }
                case TMPTextUpdateExtras tmpTextUpdateExtras:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<TextMeshProUGUI>(tmpTextUpdateExtras.Id);
                    t.characterSpacing = tmpTextUpdateExtras.CharacterSpacing;
                    t.wordSpacing = tmpTextUpdateExtras.WordSpacing;
                    t.lineSpacing = tmpTextUpdateExtras.LineSpacing;
                    t.paragraphSpacing = tmpTextUpdateExtras.ParagraphSpacing;
                    t.alignment = (TextAlignmentOptions) tmpTextUpdateExtras.Alignment;
                    t.enableWordWrapping = tmpTextUpdateExtras.WrappingEnabled;
                    t.overflowMode = (TextOverflowModes) tmpTextUpdateExtras.Overflow;
                    t.horizontalMapping = (TextureMappingOptions) tmpTextUpdateExtras.HorizontalMapping;
                    t.verticalMapping = (TextureMappingOptions) tmpTextUpdateExtras.VerticalMapping;
                    t.margin = tmpTextUpdateExtras.Margin.ToEngineType();
                    break;
                }
            }
        }
    }
}