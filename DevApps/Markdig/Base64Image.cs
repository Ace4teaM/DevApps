using Markdig.Parsers;
using Markdig.Syntax;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Markdig.Renderers.Wpf.Extensions
{
    public class Base64Image : Markdig.Syntax.ContainerBlock
    {
        internal Image img;

        public Base64Image(BlockParser parser, byte[] content) : base(parser)
        {
            img = new Image();

            using (var stream = new MemoryStream(content))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                img.Source = bmp;
                img.Stretch = Stretch.None;
            }
        }
    }

    public class Base64Drawing : Markdig.Syntax.ContainerBlock
    {
        internal DrawingGroup group;

        public Base64Drawing(BlockParser parser, DrawingGroup group) : base(parser)
        {
            this.group = group;
        }
    }

    public class Base64Parser : BlockParser
    {
        public Base64Parser()
        {
            OpeningCharacters = new[] { '!' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            var line = processor.Line.ToString().Trim();

            // Vérifie si c’est un bloc de type image base64
            var match = new Regex(@"^\!\[.*\]\(data:image/(png|svg\+xml);base64,([A-Za-z0-9+/=]+)\)$").Match(line);
            if (match.Success && line.EndsWith(')'))
            {
                var type = match.Groups[1].Value;
                var data = match.Groups[2].Value;
                if (data.Length >= 1)
                {
                    var content = Convert.FromBase64String(data);

                    if (type == "png")
                    {
                        var block = new Base64Image(this, content)
                        {
                            Column = processor.Column,
                            Span = new SourceSpan(processor.Start, processor.Line.End),
                            Line = processor.LineIndex
                        };

                        processor.NewBlocks.Push(block);
                        return BlockState.BreakDiscard;
                    }
                    else//svg
                    {
                        try
                        {
                            var settings = new WpfDrawingSettings();
                            settings.IncludeRuntime = true;
                            settings.TextAsGeometry = false;

                            var svgReader = new FileSvgReader(settings);
                            var drawing = svgReader.Read(new MemoryStream(content));

                            var maxSize = 500;//todo gérer la taille en fonction du ratio largeur/hauteur
                            var fHeight = drawing.Bounds.Height > drawing.Bounds.Width ? (1.0 / drawing.Bounds.Height) * maxSize : (1.0 / drawing.Bounds.Width) * maxSize;

                            var mx = new Matrix();
                            mx.Translate(-drawing.Bounds.X, -drawing.Bounds.Y);
                            mx.Scale(fHeight, fHeight);

                            drawing.Transform = new MatrixTransform(mx);

                            var block = new Base64Drawing(this, drawing)
                            {
                                Column = processor.Column,
                                Span = new SourceSpan(processor.Start, processor.Line.End),
                                Line = processor.LineIndex
                            };

                            processor.NewBlocks.Push(block);
                            return BlockState.BreakDiscard;
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.WriteLine(ex.Message);
                        }

                        return BlockState.None;
                    }
                }
            }

            return BlockState.None;
        }
    }
    public class Base64Extension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<Base64Parser>())
            {
                pipeline.BlockParsers.Insert(0, new Base64Parser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is WpfRenderer wpfRenderer)
            {
                if (!wpfRenderer.ObjectRenderers.Contains<Base64ImageRenderer>())
                {
                    wpfRenderer.ObjectRenderers.Add(new Base64ImageRenderer());
                }
                if (!wpfRenderer.ObjectRenderers.Contains<Base64DrawingRenderer>())
                {
                    wpfRenderer.ObjectRenderers.Add(new Base64DrawingRenderer());
                }
            }
        }
    }

    public class Base64ImageRenderer : WpfObjectRenderer<Base64Image>
    {
        protected override void Write(WpfRenderer renderer, Base64Image image)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (image == null) throw new ArgumentNullException(nameof(image));

            var p = new Paragraph();
            p.Inlines.Add(new InlineUIContainer(image.img));
            renderer.WriteBlock(p);
        }
    }

    public class Base64DrawingRenderer : WpfObjectRenderer<Base64Drawing>
    {
        protected override void Write(WpfRenderer renderer, Base64Drawing drawing)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (drawing == null) throw new ArgumentNullException(nameof(drawing));

            var drawingImage  = new System.Windows.Media.DrawingImage(drawing.group);

            // l’élément visuel qui pourra être inséré dans le document
            Image imageControl = new Image
            {
                Source = drawingImage,
                Stretch = Stretch.None
            };

            var p = new Paragraph();
            p.Inlines.Add(new InlineUIContainer(imageControl));
            renderer.WriteBlock(p);
        }
    }
}