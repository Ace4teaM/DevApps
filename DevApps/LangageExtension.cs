using System.Windows.Controls;
using System.Windows;
using DevApps.GUI;

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