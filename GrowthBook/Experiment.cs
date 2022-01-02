using System;
using System.Text.RegularExpressions;

namespace GrowthBook
{
    public class Experiment
    {
        public string Key { get; set; } 
        public object[] Variations { get; set; } 
        public double[] Weights { get; set; }
        public string Status { get; set; }
        public double? Coverage { get; set; }
        public string Url { get; set; }
        public Func<bool> Include { get; set; }
        public string[] Groups { get; set; }
        public int? Force { get; set; }
        public string HashAttribute { get; set; }
    }
}
