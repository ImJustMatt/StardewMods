using System.Collections.Generic;
using ImJustMatt.GarbageDay.API;

namespace ImJustMatt.GarbageDay
{
    public class GarbageDayAPI : IGarbageDayAPI
    {
        private readonly IDictionary<string, double> _globalLoot;
        private readonly IDictionary<string, IDictionary<string, double>> _localLoot;
        private readonly HashSet<string> _maps;

        internal GarbageDayAPI(HashSet<string> maps,
            IDictionary<string, double> globalLoot,
            IDictionary<string, IDictionary<string, double>> localLoot)
        {
            _maps = maps;
            _globalLoot = globalLoot;
            _localLoot = localLoot;
        }

        public void AddMaps(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                _maps.Add(path);
            }
        }

        public void AddMap(string path)
        {
            _maps.Add(path);
        }

        public void AddLoot(IDictionary<string, double> lootTable)
        {
            foreach (var loot in lootTable)
            {
                if (_globalLoot.ContainsKey(loot.Key))
                {
                    _globalLoot[loot.Key] = loot.Value;
                }
                else
                {
                    _globalLoot.Add(loot.Key, loot.Value);
                }
            }
        }

        public void AddLoot(string whichCan, IDictionary<string, double> lootTable)
        {
            if (!_localLoot.TryGetValue(whichCan, out var localLoot))
            {
                localLoot = new Dictionary<string, double>();
                _localLoot.Add(whichCan, localLoot);
            }

            foreach (var loot in lootTable)
            {
                if (localLoot.ContainsKey(loot.Key))
                {
                    localLoot[loot.Key] = loot.Value;
                }
                else
                {
                    localLoot.Add(loot.Key, loot.Value);
                }
            }
        }
    }
}