using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.API;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class StorageModel : StorageConfigModel, IStorage
    {
        public string SpecialChestType { get; set; } = "None";
        public bool IsFridge { get; set; }
        public bool HeldStorage { get; set; }
        public float OpenNearby { get; set; }
        public string OpenNearbySound { get; set; } = "doorCreak";
        public string CloseNearbySound { get; set; } = "doorCreakReverse";
        public string OpenSound { get; set; } = "openChest";
        public string PlaceSound { get; set; } = "axe";
        public string CarrySound { get; set; } = "pickUpItem";
        public bool IsPlaceable { get; set; } = true;
        public string Image { get; set; }
        public int Frames { get; set; } = 5;
        public string Animation { get; set; } = "None";
        public int Delay { get; set; } = 5;
        public bool PlayerColor { get; set; } = true;
        public bool PlayerConfig { get; set; } = true;
        public int Depth { get; set; }
        public IDictionary<string, string> ModData { get; set; } = new Dictionary<string, string>();
        public HashSet<string> AllowList { get; set; } = new();
        public HashSet<string> BlockList { get; set; } = new();
    }
}