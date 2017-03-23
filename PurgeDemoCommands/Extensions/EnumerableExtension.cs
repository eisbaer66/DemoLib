using System.Collections.Generic;

namespace PurgeDemoCommands.Extensions
{
    public static class EnumerableExtension
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> list)
        {
            return new HashSet<T>(list);
        }
    }
}