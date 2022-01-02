using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrowthBook
{
    public class Context
    {
        public Context()
        {
            Enabled = true;
            Overrides = new List<Experiment>();
        }
        
        public bool Enabled { get; set; }
        public Dictionary<string, object> User { get; set; }
        public Dictionary<string, bool> Groups { get; set; }
        public string Url { get; set; }
        public IList<Experiment> Overrides { get;  }
        public Dictionary<string, int> ForcedVariations { get; set; }
        public bool QaMode { get; set; }
        public Func<Experiment, ExperimentResult, Task> TrackingCallback { get; set; }
    }
}
