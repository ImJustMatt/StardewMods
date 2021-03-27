using System.Collections.Generic;
using ImJustMatt.GarbageDay.API;
using StardewModdingAPI.Utilities;

namespace ImJustMatt.GarbageDay
{
    public class GarbageDayAPI : IGarbageDayAPI
    {
        private readonly Dictionary<string, Dictionary<string, double>> _loot;

        internal GarbageDayAPI(Dictionary<string, Dictionary<string, double>> loot)
        {
            _loot = loot;
        }

        public void AddLoot(string whichCan, IDictionary<string, double> lootTable)
        {
            if (!_loot.TryGetValue(whichCan, out var loot))
            {
                loot = new Dictionary<string, double>();
                _loot.Add(whichCan, loot);
            }

            foreach (var lootItem in lootTable)
            {
                loot[lootItem.Key] = lootItem.Value;
            }
        }
    }
}