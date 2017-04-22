using System.Collections.Generic;

namespace PurgeDemoCommands.Core.Extensions
{
    public static class EnumerableExtension
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> list)
        {
            return new HashSet<T>(list);
        }
    }
}