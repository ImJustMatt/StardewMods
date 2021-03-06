using System.Collections;
using System.Collections.Generic;

namespace ImJustMatt.GarbageDay.Framework
{
    internal class ModConfig
    {
        public int GarbageDay { get; set; } = 0;

        public bool GetRandomItemFromSeason = true;

        public IList<string> GlobalLoot { get; set; } = new List<string>
        {
            "item_trash",
            "item_joja_cola",
            "item_broken_glasses",
            "item_broken_cd",
            "item_soggy_newspaper",
            "item_bread",
            ""
        };

        public IDictionary<string, IList<string>> LocalLoot { get; set; } = new Dictionary<string, IList<string>>
        {
            
        };
    }
}