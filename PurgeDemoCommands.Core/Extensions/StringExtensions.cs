namespace PurgeDemoCommands.Core.Extensions
{
    public static class StringExtensions
    {
        public static string TillFirst(this string s, char c)
        {
            int index = s.IndexOf(c);

            if (index < 0)
                return s;

            return s.Substring(0, index);
        }
    }
}