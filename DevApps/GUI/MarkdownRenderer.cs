using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;
using System.Windows;
using System.Windows.Media;


namespace DevApps.GUI
{
    internal class MarkdownRenderer
    {
        private Typeface normalFont = new Typeface("Segoe UI");
        private Typeface boldFont = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private Typeface italicFont = new Typeface(new FontFamily("Segoe UI"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
        private double lineHeight = 22;

        public void DrawMarkdown(DrawingContext dc, string markdown, Point origin, Point max)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return;

            var doc = Markdown.Parse(markdown);
            double y = origin.Y;

            if (max.X - origin.X < 0)
                return;

            foreach (var block in doc)
            {
                if (max.Y - y < 0)
                    break;

                switch (block)
                {
                    case HeadingBlock heading:
                        y = DrawHeading(dc, heading, origin.X, y, max.X - origin.X, max.Y - y);
                        break;

                    case ParagraphBlock paragraph:
                        y = DrawParagraph(dc, paragraph, origin.X, y, max.X - origin.X, max.Y - y);
                        break;

                    case ListBlock list:
                        y = DrawList(dc, list, origin.X, y, max.X - origin.X, max.Y - y);
                        break;

                    default:
                        y = DrawText(dc, markdown.Substring(block.Span.Start, block.Span.Length), origin.X, y, max.X - origin.X, max.Y - y);
                        break;
                }
            }
        }

        private double DrawText(DrawingContext dc, string text, double x, double y, double w, double h)
        {
            double startY = y;

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                normalFont,
                16,
                Brushes.Black,
                DesignerWindow.PixelsPerDip)
            {
                MaxTextWidth = w,
                MaxTextHeight = h,
                //TextWrapping = TextWrapping.Wrap
            };

            dc.DrawText(ft, new Point(x, y));
            return y + ft.Height + 8;
        }

        private double DrawHeading(DrawingContext dc, HeadingBlock heading, double x, double y, double w, double h)
        {
            string text = GetInlineText(heading.Inline);
            double fontSize = heading.Level switch
            {
                1 => 28,
                2 => 22,
                3 => 18,
                _ => 16
            };

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                boldFont,
                fontSize,
                Brushes.SteelBlue,
                DesignerWindow.PixelsPerDip);

            ft.MaxTextWidth = w;
            ft.MaxTextHeight = h;

            dc.DrawText(ft, new Point(x, y));
            return y + ft.Height + 10;
        }

        private double DrawParagraph(DrawingContext dc, ParagraphBlock paragraph, double x, double y, double w, double h)
        {
            double startY = y;
            string text = GetInlineText(paragraph.Inline);

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                normalFont,
                16,
                Brushes.Black,
                DesignerWindow.PixelsPerDip)
            {
                MaxTextWidth = w,
                MaxTextHeight = h,
                //TextWrapping = TextWrapping.Wrap
            };

            dc.DrawText(ft, new Point(x, y));
            return y + ft.Height + 8;
        }

        private double DrawList(DrawingContext dc, ListBlock list, double x, double y, double w, double h)
        {
            foreach (ListItemBlock item in list)
            {
                string text = "";
              /*  if (item.Inline != null)
                    text = GetInlineText(item.Inline);
                else*/ if (item.LastChild is ParagraphBlock para)
                    text = GetInlineText(para.Inline);

                var ft = new FormattedText(
                    "• " + text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    normalFont,
                    16,
                    Brushes.DimGray,
                    DesignerWindow.PixelsPerDip)
                {
                    MaxTextWidth = w,
                    MaxTextHeight = h,
                };


                dc.DrawText(ft, new Point(x + 10, y));
                y += ft.Height + 4;
            }
            return y + 6;
        }

        private string GetInlineText(ContainerInline inline)
        {
            if (inline == null) return string.Empty;
            string text = "";

            foreach (var child in inline)
            {
                switch (child)
                {
                    case LiteralInline literal:
                        text += literal.Content.Text.Substring(literal.Content.Start, literal.Content.Length);
                        break;

                    case EmphasisInline em when em.DelimiterCount == 2:
                        text += "**" + GetInlineText(em) + "**";
                        break;

                    case EmphasisInline em when em.DelimiterCount == 1:
                        text += "_" + GetInlineText(em) + "_";
                        break;

                    default:
                        text += GetInlineText(child as ContainerInline);
                        break;
                }
            }
            return text.Trim();
        }
    }

}
