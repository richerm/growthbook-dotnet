namespace GrowthBook
{
    public class ValidationRange
    {
        public ValidationRange(double start, double end)
        {
            Start = start;
            End = end;
        }
        public double Start { get; set; }
        public double End { get; set; }
    }
}
