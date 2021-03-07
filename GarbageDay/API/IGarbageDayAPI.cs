using System.Collections.Generic;

namespace ImJustMatt.GarbageDay.API
{
    public interface IGarbageDayAPI
    {
        /// <summary>Whitelists Maps for Garbage Cans to be added</summary>
        /// <param name="paths">Target paths from GameContent folder</param>
        void AddMaps(IEnumerable<string> paths);

        /// <summary>Whitelists Map for Garbage Cans to be added</summary>
        /// <param name="path">Target path from GameContent folder</param>
        void AddMap(string path);

        /// <summary>Adds to global loot table for any Garbage Can.</summary>
        /// <param name="lootTable">Loot table of item context tags with their relative probability</param>
        void AddLoot(IDictionary<string, double> lootTable);

        /// <summary>Adds to loot table for a specific Garbage Can.</summary>
        /// <param name="whichCan">Unique ID for Garbage Can matching name from Map Action</param>
        /// <param name="lootTable">Loot table of item context tags with their relative probability</param>
        void AddLoot(string whichCan, IDictionary<string, double> lootTable);
    }
}