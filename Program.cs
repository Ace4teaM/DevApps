using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using PdfSharp.UniversalAccessibility.Drawing;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

internal class Program
{
    internal enum ContentType
    {
        Undefined,
        Text_UTF8,
        Image_PNG,
        Image_JPEG
    }

    internal static readonly int Indentation  = 5;

    internal static void PrintItem(ref string indent, PdfItem item)
    {
        if (item is PdfDictionary)
            PrintPdfDictionnary(ref indent, item as PdfDictionary);
        else if (item is PdfName)
            PrintPdfName(ref indent, item as PdfName);
        else if (item is PdfContents)
            PrintPdfContents(ref indent, item as PdfContents);
        else if (item is PdfRectangle)
            PrintPdfRectangle(ref indent, item as PdfRectangle);
        else if (item is PdfArray)
            PrintPdfArray(ref indent, item as PdfArray);
        else if (item is PdfReference)
            PrintPdfReference(ref indent, item as PdfReference);
        else if (item is PdfInteger)
            PrintPdfInteger(ref indent, item as PdfInteger);
        else
            Console.WriteLine("unsupported " + item.GetType());
    }

    internal static void PrintPdfDictionnary(ref string indent, PdfDictionary item)
    {
        if (item is PdfDictionaryWithContentStream)
        {
            Console.WriteLine(indent + "Dictionary as stream content");
        }
        else
        {
            Console.WriteLine(indent + "Dictionary");
        }
        Console.WriteLine(indent + "{");

        indent += new String(' ', Indentation);

        foreach (var c in item)
        {
            PrintItem(ref indent, c.Value);
        }

        indent = indent.Substring(0, indent.Length - Indentation);

        Console.WriteLine(indent + "}");
    }

    internal static void PrintPdfArray(ref string indent, PdfArray item)
    {
        foreach (var c in item)
        {
            PrintItem(ref indent, c);
        }
    }

    internal static void PrintPdfName(ref string indent, PdfName item)
    {
        Console.WriteLine(indent+item.Value);
    }

    internal static void PrintPdfContents(ref string indent, PdfContents item)
    {
        foreach (var c in item)
        {
            foreach (var cc in c)
            {
                Console.WriteLine(indent + cc.Key + ": " + indent + cc.Value.GetType());
            }
        }
    }

    internal static void PrintPdfRectangle(ref string indent, PdfRectangle item)
    {
        Console.WriteLine(indent + item.Location);
        Console.WriteLine(indent + item.Size);
    }

    internal static void PrintPdfInteger(ref string indent, PdfInteger item)
    {
        Console.WriteLine(indent + item.ToString());
    }


    internal static List<PdfReference> alreadyPrintedRef = new List<PdfReference>();
    internal static void PrintPdfReference(ref string indent, PdfReference item)
    {
        Console.WriteLine(indent + item.Value.GetType());
        Console.WriteLine(indent + item.Value);

        if(alreadyPrintedRef.Contains(item) == false)
        {
            alreadyPrintedRef.Add(item);

            if (item.Value is PdfItem)
            {
                PrintItem(ref indent, item.Value);
            }
            else
            {
                Console.WriteLine("unsupported PdfObject " + item.Value.GetType());
            }
        }
    }

    private static void Main(string[] args)
    {
        PdfDocument doc;


        doc = PdfReader.Open(args[0]);

        foreach(var page in doc.Pages)
        {
            var internals = page.Internals;
            var box = page.MediaBox;
            var elements = page.Elements;
            var contents = page.Contents;
            var anno = page.Annotations;
            var content = ContentReader.ReadContent(page);
            string indent = "  ";
            foreach (var element in elements)
            {

                Console.WriteLine(element.Key);
                PrintItem(ref indent, element.Value);

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        /*
        var content = args.Length >= 2 ? File.OpenRead(args[1]) : Console.OpenStandardInput();
        if (File.Exists(args[0]) && args.Contains("-o") == false)
        {
            doc = PdfReader.Open(args[0]);
        }
        else
        {
            doc = new PdfDocument();
            doc.PageLayout = PdfPageLayout.SinglePage;
        }

        int pageCount = -1;
        ContentType contentType = ContentType.Undefined;
        XRect xRect = new XRect(0, 0, 0, 0);
        XFont xFont = new XFont("Arial", 20);

        if (args.Contains("-p"))
        {
            pageCount = int.Parse(args[Array.IndexOf(args, "-p") + 1]);
        }

        PdfPage page = pageCount == -1 ? doc.AddPage() : doc.Pages[pageCount];
        if (args.Contains("-l"))
        {
            page.Orientation = Enum.Parse<PdfSharp.PageOrientation>(args[Array.IndexOf(args, "-l") + 1]);
        }

        if (args.Contains("-t"))
        {
            contentType = Enum.Parse<ContentType>(args[Array.IndexOf(args, "-t") + 1]);
        }

        if (args.Contains("-r"))
        {
            try
            {
                xRect = XRect.Parse(args[Array.IndexOf(args, "-r") + 1]);
            }
            catch
            {
                xRect = new XRect();
                var parse = args[Array.IndexOf(args, "-r") + 1].Split(',');

                xRect.X = (parse[0].EndsWith("%")) ? ParsePercentHorizontal(page, parse[0]) : ParseMesure(parse[0]);
                xRect.Y = (parse[1].EndsWith("%")) ? ParsePercentVertical(page, parse[1]) : ParseMesure(parse[1]);
                xRect.Width = (parse[2].EndsWith("%")) ? ParsePercentHorizontal(page, parse[2]) : ParseMesure(parse[2]);
                xRect.Height = (parse[3].EndsWith("%")) ? ParsePercentVertical(page, parse[3]) : ParseMesure(parse[3]);
            }
        }

        if (contentType == ContentType.Undefined && IsJPEG(content))
        {
            contentType = ContentType.Image_JPEG;
        }

        if (contentType == ContentType.Undefined && IsPNG(content))
        {
            contentType = ContentType.Image_PNG;
        }

        if (contentType == ContentType.Undefined && IsUTF8(content))
        {
            contentType = ContentType.Text_UTF8;
        }

        try
        {
            switch (contentType)
            {
                case ContentType.Undefined:
                    Console.WriteLine("Can't detect content type ! Please specify in command, ie: -t image");
                    break;

                case ContentType.Text_UTF8:
                    var stream = new MemoryStream();
                    content.CopyTo(stream);
                    var text = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

                    using (XGraphics g = XGraphics.FromPdfPage(page))
                    {
                        g.DrawString(text, xFont, XBrushes.Black, xRect, XStringFormats.Center);
                    }
                    break;

                case ContentType.Image_PNG:
                case ContentType.Image_JPEG:
                    using (XGraphics g = XGraphics.FromPdfPage(page))
                    {
                        XImage image = XImage.FromStream(content);
                        g.DrawImage(image, xRect);
                    }
                    break;
            }

            if (args.Contains("-o") && File.Exists(args[0]))
                File.Delete(args[0]);

            doc.Save(args[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }*/
    }

    internal static double ParsePercentVertical(PdfPage page, string value)
    {
        if (value.EndsWith("%"))
        {
            return (page.Height.Value / 100.0) * double.Parse(value.Substring(0, value.Length - 1));
        }

        return (page.Height.Value / 100.0) * double.Parse(value);
    }
    internal static double ParsePercentHorizontal(PdfPage page, string value)
    {
        if (value.EndsWith("%"))
        {
            return (page.Width.Value / 100.0) * double.Parse(value.Substring(0, value.Length - 1));
        }

        return (page.Width.Value / 100.0) * double.Parse(value);
    }
    internal static double ParseMesure(string value)
    {
        if (value.EndsWith("cm"))
        {
            return XUnit.FromCentimeter(double.Parse(value.Substring(0, value.Length - 1))).Value;
        }
        else if (value.EndsWith("mm"))
        {
            return XUnit.FromMillimeter(double.Parse(value.Substring(0, value.Length - 1))).Value;
        }
        else
        {
            return double.Parse(value);
        }
    }
    internal static bool IsUTF8(Stream stream)
    {
        var expected_header = new byte[] { 0xEF, 0xBB, 0xBF };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
    internal static bool IsPNG(Stream stream)
    {
        var expected_header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
    internal static bool IsJPEG(Stream stream)
    {
        var expected_header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
}