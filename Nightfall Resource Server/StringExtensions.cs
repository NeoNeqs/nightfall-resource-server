namespace Nightfall.ResourceServer;

public static class StringExtensions
{
    public static bool IsSha256String(this string s)
    {
        if (s.Length != 64)
        {
            return false;
        }

        foreach (char c in s)
        {
            if (c is (< '0' or > '9') and (< 'a' or > 'f'))
            {
                return false;
            }
        }

        return true;
    }
}