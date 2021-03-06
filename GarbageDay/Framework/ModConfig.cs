namespace ImJustMatt.GarbageDay.Framework
{
    internal class ModConfig
    {
        /// <summary>Global change that a random item from season is collected</summary>
        public double GetRandomItemFromSeason { get; set; } = 0.1;

        /// <summary>Day of week that trash is emptied out</summary>
        public int GarbageDay { get; set; } = 1;
    }
}