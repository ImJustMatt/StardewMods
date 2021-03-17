using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.ExpandedStorage.Framework.Models;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    public class TabController : TabModel
    {
        /// <summary>The UniqueId of the Content Pack that storage data was loaded from.</summary>
        protected internal string ModUniqueId = "";

        /// <summary>The Asset path to the mod's Tab Image.</summary>
        internal string Path = "";

        [JsonConstructor]
        internal TabController(ITab storageTab = null)
        {
            if (storageTab == null)
                return;

            TabName = storageTab.TabName;
            TabImage = storageTab.TabImage;
            AllowList = new HashSet<string>(storageTab.AllowList);
            BlockList = new HashSet<string>(storageTab.BlockList);
        }

        internal TabController(string tabImage, params string[] allowList)
        {
            TabImage = tabImage;
            AllowList = new HashSet<string>(allowList);
        }

        internal Func<Texture2D> Texture { get; set; }

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