using System.Collections.Generic;

namespace ImJustMatt.GarbageDay.API
{
    public interface IContent
    {
        /// <summary>List of Maps for Garbage Day to scan</summary>
        HashSet<string> Maps { get; set; }

        /// <summary>Items to add to Global Loot table</summary>
        IDictionary<string, double> GlobalLoot { get; set; }

        /// <summary>Items to add to a Local Loot table for a specific Garbage Can</summary>
        IDictionary<string, IDictionary<string, double>> LocalLoot { get; set; }
    }
}