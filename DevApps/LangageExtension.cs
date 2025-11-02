using DevApps.GUI;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

public static class FileExtensions
{
    /// <summary>
    /// Compare la différence entre 2 fichiers
    /// </summary>
    public static bool IsDifferent(string filename, string filename2)
    {
        var fi1 = new FileInfo(filename);
        var fi2 = new FileInfo(filename2);

        if (fi1.Length != fi2.Length)
            return true;

        using var sha256 = SHA256.Create();
        using var fs1 = File.OpenRead(filename);
        using var fs2 = File.OpenRead(filename2);

        var hash1 = sha256.ComputeHash(fs1);
        var hash2 = sha256.ComputeHash(fs2);

        return hash1.SequenceEqual(hash2) == false;
    }

    /// <summary>
    /// Compare la différence entre le flux et le fichier
    /// </summary>
    public static bool IsDifferent(this Stream stream, string filename)
    {
        var fi1 = new FileInfo(filename);

        if (fi1.Length != stream.Length)
            return true;

        stream.Position = 0;

        using var sha256 = SHA256.Create();
        using var fs1 = File.OpenRead(filename);

        var hash1 = sha256.ComputeHash(fs1);
        var hash2 = sha256.ComputeHash(stream);

        stream.Position = 0;

        return hash1.SequenceEqual(hash2) == false;
    }

    /// <summary>
    /// Compare la différence entre 2 flux 
    /// </summary>
    public static bool IsDifferent(this Stream stream, Stream stream2)
    {
        if (stream2.Length != stream.Length)
            return true;

        stream.Position = 0;
        stream2.Position = 0;

        using var sha256 = SHA256.Create();

        var hash1 = sha256.ComputeHash(stream2);
        var hash2 = sha256.ComputeHash(stream);

        stream.Position = 0;
        stream2.Position = 0;

        return hash1.SequenceEqual(hash2) == false;
    }
}

public static class EnumerableExtensions
{
    public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> destination)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        // Vérifie si tous les éléments de la séquence destination sont présents dans la séquence source
        return destination.All(item => source.Contains(item));
    }
}

public static class CanvasExtensions
{
    public static Rect GetChildrenBoundingBox(this Canvas canvas)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));

        Rect boundingBox = Rect.Empty;

        foreach (UIElement child in canvas.Children)
        {
            if (child is FrameworkElement fe)
            {
                double left = Canvas.GetLeft(fe);
                double top = Canvas.GetTop(fe);

                // Défaut à 0 si NaN
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                // Mesure si nécessaire (utile si layout non encore fait)
                fe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                Rect childRect = new Rect(left, top, fe.DesiredSize.Width, fe.DesiredSize.Height);
                boundingBox.Union(childRect);
            }
        }

        return boundingBox;
    }
}