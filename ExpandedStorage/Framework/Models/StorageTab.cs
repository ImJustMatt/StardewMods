using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.API;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    public class StorageTab : IStorageTab
    {
        /// <summary>The UniqueId of the Content Pack that storage data was loaded from.</summary>
        protected internal string ModUniqueId = "";

        /// <summary>The Asset path to the mod's Tab Image.</summary>
        internal string Path = "";

        [JsonConstructor]
        internal StorageTab(IStorageTab storageTab = null)
        {
            if (storageTab == null)
                return;

            TabName = storageTab.TabName;
            TabImage = storageTab.TabImage;
            AllowList = storageTab.AllowList;
            BlockList = storageTab.BlockList;
        }

        internal StorageTab(string tabImage, params string[] allowList)
        {
            TabImage = tabImage;
            AllowList = new HashSet<string>(allowList);
        }

        internal Texture2D Texture { get; set; }

        public string TabName { get; set; }
        public string TabImage { get; set; }
        public HashSet<string> AllowList { get; set; } = new();
        public HashSet<string> BlockList { get; set; } = new();

        private bool IsAllowed(Item item)
        {
            return !AllowList.Any() || AllowList.Any(item.MatchesTagExt);
        }

        private bool IsBlocked(Item item)
        {
            return BlockList.Any() && BlockList.Any(item.MatchesTagExt);
        }

        internal bool Filter(Item item)
        {
            return IsAllowed(item) && !IsBlocked(item);
        }
    }
}