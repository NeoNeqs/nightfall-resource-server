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

    public static bool IsValidFileName(this string s)
    {
        foreach (char c in s)
        {
            if (c is (< '0' or > '9') and (< 'a' or > 'z') and (< 'A' or > 'Z') and not '.')
            {
                return false;
            }
        }
        
        return true;
    }
}