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