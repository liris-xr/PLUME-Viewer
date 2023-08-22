using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.UI
{
    public class TextPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, UnpackedSample sample)
        {
            switch (sample.Payload)
            {
                case TextCreate textCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Text>(textCreate.Id);
                    break;
                }
                case TextDestroy textDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(textDestroy.Id);
                    break;
                }
                case TextUpdateColor textUpdateColor:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<Text>(textUpdateColor.Id);
                    t.color = textUpdateColor.Color.ToEngineType();
                    break;
                }
                case TextUpdateValue textUpdateValue:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<Text>(textUpdateValue.Id);
                    t.text = textUpdateValue.Text;
                    break;
                }
                case TextUpdateFont textUpdateFont:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<Text>(textUpdateFont.Id);
                    t.font = ctx.GetOrDefaultAssetByIdentifier<Font>(textUpdateFont.FontId);
                    t.fontStyle = (FontStyle)textUpdateFont.FontStyle;
                    t.fontSize = textUpdateFont.FontSize;
                    ctx.TryAddAssetIdentifierCorrespondence(textUpdateFont.FontId, t.font);
                    break;
                }
                case TextUpdateExtras textUpdateExtras:
                {
                    var t = ctx.GetOrCreateComponentByIdentifier<Text>(textUpdateExtras.Id);
                    t.lineSpacing = textUpdateExtras.LineSpacing;
                    t.supportRichText = textUpdateExtras.SupportRichText;
                    t.alignment = (TextAnchor)textUpdateExtras.Alignment;
                    t.alignByGeometry = textUpdateExtras.AlignByGeometry;
                    t.horizontalOverflow = (HorizontalWrapMode)textUpdateExtras.HorizontalOverflow;
                    t.verticalOverflow = (VerticalWrapMode)textUpdateExtras.VerticalOverflow;
                    t.resizeTextForBestFit = textUpdateExtras.ResizeTextForBestFit;
                    t.resizeTextMinSize = textUpdateExtras.ResizeTextMinSize;
                    t.resizeTextMaxSize = textUpdateExtras.ResizeTextMaxSize;
                    break;
                }
            }
        }
    }
}