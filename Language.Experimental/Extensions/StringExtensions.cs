
namespace Language.Experimental.Extensions;


internal static class StringExtensions
{
    public static string Indent(this string s, int indentLevel = 1)
    {
        return $"{new string('\t', indentLevel)}{s}";
    }
}