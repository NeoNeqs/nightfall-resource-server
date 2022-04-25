namespace Nightfall.ResourceServer;

public static class StringExtensions
{
    public static bool IsSha256String(this string s)
    {
        return s.Length == 64 && s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    public static bool IsValidFileName(this string s)
    {
        return s.All(c => c is > '/' and < ':' or > '@' and < '\\' or > '`' and < '{' or ('_' or '.'));
    }
}