﻿using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class ConnectorController : IAutomatable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location which contains the machine.</summary>
        public GameLocation Location { get; }

        /// <summary>The tile area covered by the machine.</summary>
        public Rectangle TileArea { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="location">The location which contains the machine.</param>
        /// <param name="tileArea">The tile area covered by the machine.</param>
        public ConnectorController(GameLocation location, Rectangle tileArea)
        {
            Location = location;
            TileArea = tileArea;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="location">The location which contains the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public ConnectorController(GameLocation location, Vector2 tile)
            : this(location, new Rectangle((int)tile.X, (int)tile.Y, 1, 1)) { }
    }
}