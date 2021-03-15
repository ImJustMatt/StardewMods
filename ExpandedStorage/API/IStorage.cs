using System.Collections.Generic;

namespace ImJustMatt.ExpandedStorage.API
{
    public interface IStorage : IStorageConfig
    {
        /// <summary>One of the special chest types (None, MiniShippingBin, JunimoChest).</summary>
        string SpecialChestType { get; set; }

        /// <summary>Enable storage to function as a mini-fridge.</summary>
        bool IsFridge { get; set; }

        /// <summary>Play opening animation when player is nearby.</summary>
        bool OpenNearby { get; set; }

        /// <summary>The sound that will play when the storage opens while approached.</summary>
        string OpenNearbySound { get; set; }

        /// <summary>The sound that will play when the storage closes.</summary>
        string CloseNearbySound { get; set; }

        /// <summary>The sound that will play when the storage is opened.</summary>
        string OpenSound { get; set; }

        /// <summary>The sound that will play when the storage is placed.</summary>
        string PlaceSound { get; set; }

        /// <summary>The sound that will play when the storage is picked up.</summary>
        string CarrySound { get; set; }

        /// <summary>Allow storage to be placed.</summary>
        bool IsPlaceable { get; set; }

        /// <summary>The spritesheet to use for drawing this storage.</summary>
        string Image { get; set; }

        /// <summary>The number of animation frames in the spritesheet.</summary>
        int Frames { get; set; }

        /// <summary>Set to true for storage to constantly animate.</summary>
        string Animation { get; set; }

        /// <summary>The number of ticks between each animation frame.</summary>
        int Delay { get; set; }

        /// <summary>Enables the player color choice and overlay layers.</summary>
        bool PlayerColor { get; set; }

        /// <summary>Allows config to override capacity and toggleable features.</summary>
        bool PlayerConfig { get; set; }

        /// <summary>The depth from the bottom for the obstruction bounds.</summary>
        int Depth { get; set; }

        /// <summary>Add modData to placed chests (if key does not already exist).</summary>
        IDictionary<string, string> ModData { get; set; }

        /// <summary>When specified, storage may only hold items with allowed context tags.</summary>
        HashSet<string> AllowList { get; set; }

        /// <summary>When specified, storage may hold allowed items except for those with blocked context tags.</summary>
        HashSet<string> BlockList { get; set; }
    }
}