using System.Diagnostics.CodeAnalysis;

namespace AgentOrange.Core.Extensions;

static class StringExtensions
{
    public static string? OrDefault(this string? text) =>
        text.HasContent ? text : null;

    extension([NotNullWhen(false)] string? text)
    {
        public bool IsEmpty => string.IsNullOrWhiteSpace(text);
    }

    extension([NotNullWhen(true)] string? text)
    {
        public bool HasContent => !text.IsEmpty;
    }

    public static string WithArgs(this string format, params ReadOnlySpan<object?> args) =>
        string.Format(format, args);
}