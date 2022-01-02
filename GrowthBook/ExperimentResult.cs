using System;
using System.Collections.Generic;
using System.Text;

namespace GrowthBook
{
    public class ExperimentResult
    {
        private ExperimentResult()
        {

        }

        public bool InExperiment { get; set; }
        public int VariationIndex { get; set; }
        public object Value { get; set; }
        public string HashAttribute { get; set; }
        public string HashValue { get; set; }

        public static ExperimentResult Create(Context ctx, Experiment experiment, int variationIndex = 0, bool inExperiment = true)
        {
            if (variationIndex < 0 || experiment?.Variations?.Length == 0 || variationIndex > experiment?.Variations.Length - 1)
                variationIndex = 0;

            var hashAttribute = experiment?.HashAttribute ?? "id";
            object hashValue = null;
            if (!ctx?.User?.TryGetValue(hashAttribute, out hashValue) ?? true)
                hashValue = null;

            return new ExperimentResult()
            {
                HashAttribute = hashAttribute,
                HashValue = hashValue as string,
                VariationIndex = variationIndex,
                Value = experiment?.Variations[variationIndex],
                InExperiment = inExperiment
            };
        }
    }
}
