namespace coursework
{
     internal class Settings
    {
        public double Volume;
        public double PackingCost;
        public double FreePackagingAmount;
        public double TransportationCost;
        public double Probability;
        public double MaxLength;
        public double MaxWidth;
        public double MaxHeight;

        // Deltas
        public double VolumeDelta;
        public double PackingCostDelta;
        public double TransportationCostDelta;

        // Pessimistic settings
        public Settings Pess;

        // Optimistic settings
        public Settings Opt;

    }
}
