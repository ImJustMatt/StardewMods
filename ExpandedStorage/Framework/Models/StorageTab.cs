﻿using System;
using System.Collections.Generic;
using System.Linq;
using ExpandedStorage.Framework.Extensions;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ExpandedStorage.Framework.Models
{
    internal class StorageTab
    {
        /// <summary>Tab Name must match the name from Json Assets.</summary>
        public string TabName { get; set; }
        
        /// <summary>Image to display for tab, will search asset folder first and default next.</summary>
        public string TabImage { get; set; }
        
        /// <summary>When specified, tab will only show the listed item/category IDs.</summary>
        public IList<string> AllowList { get; set; }
        
        /// <summary>When specified, tab will show all/allowed items except for listed item/category IDs.</summary>
        public IList<string> BlockList { get; set; }

        /// <summary>Texture to draw for tab.</summary>
        internal Texture2D Texture => _texture ??= LoadTexture();
        private Texture2D _texture;
        protected internal Func<Texture2D> LoadTexture;
        
        /// <summary>The UniqueId of the Content Pack that storage data was loaded from.</summary>
        internal string ModUniqueId;
        
        private bool IsAllowed(Item item) => AllowList == null || !AllowList.Any() || AllowList.Any(item.MatchesTagExt);
        private bool IsBlocked(Item item) => BlockList != null && BlockList.Any() && BlockList.Any(item.MatchesTagExt);
        internal bool Filter(Item item) => IsAllowed(item) && !IsBlocked(item);
    }
}