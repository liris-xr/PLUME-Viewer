using PLUME.Sample;
using PLUME.Sample.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PLUME.Viewer.Player.Module.Unity.UI
{
    public class TextPlayerModule : PlayerModule
    {
        public override void PlaySample(PlayerContext ctx, RawSample rawSample)
        {
            switch (rawSample.Payload)
            {
                case TextCreate textCreate:
                {
                    ctx.GetOrCreateComponentByIdentifier<Text>(textCreate.Component);
                    break;
                }
                case TextDestroy textDestroy:
                {
                    ctx.TryDestroyComponentByIdentifier(textDestroy.Component);
                    break;
                }
                case TextUpdate textUpdate:
                {
                    var text = ctx.GetOrCreateComponentByIdentifier<Text>(textUpdate.Component);

                    if (textUpdate.Color != null)
                    {
                        text.color = textUpdate.Color.ToEngineType();
                    }

                    if (textUpdate.HasText)
                    {
                        text.text = textUpdate.Text;
                    }

                    if (textUpdate.Font != null)
                    {
                        text.font = ctx.GetOrDefaultAssetByIdentifier<Font>(textUpdate.Font);
                        ctx.TryAddAssetIdentifierCorrespondence(textUpdate.Font, text.font);
                    }

                    if (textUpdate.HasFontStyle)
                    {
                        text.fontStyle = textUpdate.FontStyle.ToEngineType();
                    }

                    if (textUpdate.HasFontSize)
                    {
                        text.fontSize = textUpdate.FontSize;
                    }

                    if (textUpdate.HasLineSpacing)
                    {
                        text.lineSpacing = textUpdate.LineSpacing;
                    }

                    if (textUpdate.HasSupportRichText)
                    {
                        text.supportRichText = textUpdate.SupportRichText;
                    }

                    if (textUpdate.HasAlignment)
                    {
                        text.alignment = textUpdate.Alignment.ToEngineType();
                    }

                    if (textUpdate.HasAlignByGeometry)
                    {
                        text.alignByGeometry = textUpdate.AlignByGeometry;
                    }

                    if (textUpdate.HasHorizontalOverflow)
                    {
                        text.horizontalOverflow = textUpdate.HorizontalOverflow.ToEngineType();
                    }

                    if (textUpdate.HasVerticalOverflow)
                    {
                        text.verticalOverflow = textUpdate.VerticalOverflow.ToEngineType();
                    }

                    if (textUpdate.HasResizeTextForBestFit)
                    {
                        text.resizeTextForBestFit = textUpdate.ResizeTextForBestFit;
                    }

                    if (textUpdate.HasResizeTextMinSize)
                    {
                        text.resizeTextMinSize = textUpdate.ResizeTextMinSize;
                    }

                    if (textUpdate.HasResizeTextMaxSize)
                    {
                        text.resizeTextMaxSize = textUpdate.ResizeTextMaxSize;
                    }

                    break;
                }
            }
        }
    }
}