using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.API;

namespace ImJustMatt.GarbageDay.Framework.Models
{
    public class Storage : IStorage
    {
        public int Capacity { get; set; } = 7;
        public HashSet<string> EnabledFeatures { get; set; }
        public HashSet<string> DisabledFeatures { get; set; } = new() {"CanCarry", "ShowSearchBar", "ShowColorPicker", "ShowTabs", "VacuumItems"};
        public IList<string> Tabs { get; set; }
        public string SpecialChestType { get; set; }
        public bool IsFridge { get; set; }
        public string OpenSound { get; set; } = "trashcan";
        public string PlaceSound { get; set; }
        public string CarrySound { get; set; }
        public bool IsPlaceable { get; set; }
        public string Image { get; set; }
        public int Frames { get; set; } = 1;
        public bool PlayerColor { get; set; } = true;
        public int Depth { get; set; }
        public IDictionary<string, string> ModData { get; set; }
        public HashSet<string> AllowList { get; set; }
        public HashSet<string> BlockList { get; set; }
    }
}