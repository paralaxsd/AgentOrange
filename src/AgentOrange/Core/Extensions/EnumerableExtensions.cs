namespace AgentOrange.Core.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> ExceptDefault<T>(this IEnumerable<T?>? that) =>
        that.OrEmpty()
            .Where(el => !EqualityComparer<T?>.Default.Equals(el, default))
            .Select(item => item!);
    public static IEnumerable<T> ExceptDefault<T>(this IEnumerable<T?>? that) where T : struct =>
        that.OrEmpty()
            .Where(el => el.HasValue)
            .Select(item => item!.Value);

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? that) => that ?? [];

    public static string JoinedBy<T>(this IEnumerable<T> elements, char separator) =>
        string.Join(separator, elements);

    public static string JoinedBy<T>(this IEnumerable<T> elements, string separator) =>
        string.Join(separator, elements);
}