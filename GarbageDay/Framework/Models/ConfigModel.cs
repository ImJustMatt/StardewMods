namespace ImJustMatt.GarbageDay.Framework.Models
{
    internal class ConfigModel
    {
        /// <summary>Global change that a random item from season is collected</summary>
        public double GetRandomItemFromSeason { get; set; } = 0.1;

        /// <summary>Day of week that trash is emptied out</summary>
        public int GarbageDay { get; set; } = 1;

        /// <summary>Adds IsIgnored to all Garbage Cans every day</summary>
        public bool HideFromChestsAnywhere { get; set; } = true;

        /// <summary>Edit all Maps instead of specific maps</summary>
        public bool Debug { get; set; } = false;

        /// <summary>Log Level used when loading in garbage cans</summary>
        public string LogLevel { get; set; } = "Trace";
    }
}