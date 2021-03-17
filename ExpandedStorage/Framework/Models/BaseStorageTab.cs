using System.Collections.Generic;
using ImJustMatt.ExpandedStorage.API;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class BaseStorageTab : IStorageTab
    {
        public string TabName { get; set; }
        public string TabImage { get; set; }
        public HashSet<string> AllowList { get; set; } = new();
        public HashSet<string> BlockList { get; set; } = new();
    }
}