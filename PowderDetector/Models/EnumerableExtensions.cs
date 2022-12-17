using System.Collections.Generic;
using System.Linq;

namespace PowderDetector.Models
{
    internal static class EnumerableExtensions
    {
        public static double Median(this IEnumerable<double> enumerable)
        {
            var count = enumerable.Count();
            var ordered = enumerable.OrderBy(v => v).ToList();

            return count % 2 == 1 ? ordered.ElementAt(count / 2) : (ordered.ElementAt(count / 2) + ordered.ElementAt(count / 2 + 1)) / 2;
        }
    }
}
