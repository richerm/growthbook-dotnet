using System.Linq;

namespace GrowthBook
{
    public class GrowthBookUtil
    {
        public static uint HashFnv32a(string value)
        {
            var hash = 0x811c9dc5;
            uint prime = 0x01000193;

            foreach (var c in value.ToCharArray())
            {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }

        public static ValidationRange[] GetBucketRanges(double[] weights, double coverage)
        {
            var cumulative = 0d;
            return weights.Select(w =>
            {
                var start = cumulative;
                cumulative += w;
                return new ValidationRange(start, start + coverage * w);
            }).ToArray();
        }

    }
}
