using System.Collections.Generic;

namespace ImJustMatt.GarbageDay.API
{
    public interface IGarbageDayAPI
    {
        /// <summary>Adds to loot table</summary>
        /// <param name="whichCan">Unique ID for Garbage Can matching name from Map Action</param>
        /// <param name="lootTable">Loot table of item context tags with their relative probability</param>
        void AddLoot(string whichCan, IDictionary<string, double> lootTable);
    }
}