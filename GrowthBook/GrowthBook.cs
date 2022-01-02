using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace GrowthBook
{
    public class GrowthBook
    {
        private readonly Context ctx;

        private GrowthBook(Context ctx)
        {
            this.ctx = ctx;
        }

        public ExperimentResult Run(Experiment experiment)
        {
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));
            // If experiment.variations has fewer than 2 variations, return immediately (not in experiment, variationId 0)
            if (experiment.Variations.Length < 2)
                return experiment.NotInExperiment();
            // If context.enabled is false, return immediately (not in experiment, variationId 0)
            if (!ctx.Enabled)
                return experiment.NotInExperiment();

            // If context.overrides[experiment.key] is set, merge override properties into the experiment
            MergeOverrides(experiment);

            // If context.url contains a querystring {experiment.key}=[0-9]+, return immediately (not in experiment, variationId from querystring)
            if (!string.IsNullOrWhiteSpace(ctx.Url) && Uri.TryCreate(ctx.Url, UriKind.Absolute, out var uri))
            {
                var qsValues = HttpUtility.ParseQueryString(uri.Query);
                var qsValue = qsValues.Get(experiment.Key);
                if (!string.IsNullOrEmpty(qsValue) && Regex.IsMatch(qsValue, "^[0-9]+$"))
                    return ExperimentResult.Create(ctx, experiment, int.Parse(qsValue), inExperiment:false);   
            }

            // If context.forcedVariations[experiment.key] is defined, return immediately (not in experiment, forced variation)
            int forcedIndex = 0;
            var isForced = ctx.ForcedVariations?.TryGetValue(experiment.Key, out forcedIndex) ?? false;
            if (isForced)
                return experiment.NotInExperiment(forcedIndex);

            // If experiment.status is "draft", return immediately (not in experiment, variationId 0)
            if (experiment.Status == "draft")
                return experiment.NotInExperiment();

            // Get the user hash attribute and value (context.user[experiment.hashAttribute || "id"]) and if empty, return immediately (not in experiment, variationId 0)
            if (ctx.User.TryGetValue(experiment.HashAttribute ?? "id", out var hashValue) && string.IsNullOrEmpty(hashValue as string))
                return experiment.NotInExperiment();

            // If experiment.include is set, call the function and if "false" is returned or it throws, return immediately(not in experiment, variationId 0)
            if (experiment.Include != null)
            {
                try
                {
                    if (!experiment.Include())
                        return experiment.NotInExperiment();
                }
                catch (Exception)
                {
                    return experiment.NotInExperiment();
                }
            }
                
            // If experiment.groups is set and none of them are true in context.groups, return immediately(not in experiment, variationId 0)
            if (experiment.Groups?.Length > 0 &&
                experiment.Groups.None(grp => ctx.Groups.TryGetValue(grp, out var isGroupOn) && isGroupOn))
                return experiment.NotInExperiment();

            // If experiment.url is set, evaluate as a regex against context.url and if it doesn't match or throws, return immediately (not in experiment, variationId 0)
            if (!string.IsNullOrEmpty(experiment.Url) && 
                experiment.Url.TryParseRegex(out var regex) &&
                !regex.IsMatch(ctx.Url))
                return experiment.NotInExperiment();

            // If experiment.force is set, return immediately(not in experiment, variationId experiment.force)
            if (experiment.Force.HasValue)
                return ExperimentResult.Create(ctx, experiment, experiment.Force.Value, inExperiment:false);

            // If experiment.status is "stopped", return immediately(not in experiment, variationId 0)
            if (experiment.Status == "stopped")
                return experiment.NotInExperiment();

            // If context.qaMode is true, return immediately(not in experiment, variationId 0)
            if (ctx.QaMode)
                return experiment.NotInExperiment();

            // Default weights if not specified.
            var weights = experiment.Weights ??
                Enumerable.Repeat(1d / experiment.Variations.Length, experiment.Variations.Length).ToArray();

            // Default coverage
            var coverage = experiment.Coverage ?? 1;

            // Bucket ranges
            var buckets = GrowthBookUtil.GetBucketRanges(weights, coverage);

            // Compute a hash for variation assignment
            var computedHash = GrowthBookUtil.HashFnv32a(hashValue.ToString() + experiment.Key) % 1000d / 1000d;

            // Loop through weights until we reach the hash value
            int? assigned = null;
            for (var i=0; i<buckets.Length; i++)
            {
                var range = buckets[i];
                if (computedHash >= range.Start && computedHash < range.End)
                    assigned = i; 
            }
            
            // If not assigned, return immediately
            if (!assigned.HasValue)
                return ExperimentResult.Create(ctx, experiment, 0, inExperiment: false);

            var result = ExperimentResult.Create(ctx, experiment, assigned.Value);

            // Fire the tracking callback if set
            if (ctx.TrackingCallback != null)
            {
                try
                {
                    ctx.TrackingCallback(experiment, result);
                }
                catch (Exception)
                {
                    // Fail gracefully
                }
            }
                

            // Return the result
            return result;
        }

        private void MergeOverrides(Experiment experiment)
        {
            var expOverride = ctx.Overrides.FirstOrDefault(x => x.Key == experiment.Key);
            if (expOverride == null)
                return;
            
            // TODO: Shallow copy and uses reflection (non-performant under high load)
            experiment.MergeValues(expOverride);
        }

        public static ExperimentResult Run(Context ctx, Experiment experiment)
        {
            var gb = new GrowthBook(ctx);
            return gb.Run(experiment);
        }

        public static Task<ExperimentResult> RunAsync(Context ctx, Experiment experiment)
        {
            return Task.FromResult(Run(ctx, experiment));
        }
    }
}
