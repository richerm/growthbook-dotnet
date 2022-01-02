using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GrowthBook
{
    public static class Extensions
    {
        public static ExperimentResult NotInExperiment(this Experiment experiment, int variationIndex = 0)
        {
            return ExperimentResult.Create(null, experiment, variationIndex, false);
        }

        public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return !source.Any(predicate);
        }

        public static bool TryParseRegex(this string regexValue, out Regex regex)
        {
            try
            {
                regex = new Regex(regexValue);
                return true;
            }
            catch (ArgumentException)
            {
                regex = null;
                return false;
            }
        }

        public static void MergeValues<T>(this T target, T source)
        {
            var type = typeof(T);

            var properties = type.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);
            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }
    }
}
