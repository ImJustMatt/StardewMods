using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class VacuumChestController
    {
        private const string ChestsAnywhereOrderKey = "Pathoschild.ChestsAnywhere/Order";

        private readonly AssetController _assetController;
        private readonly bool _firstRow;
        private readonly IMonitor _monitor;

        /// <summary>Tracks all chests that may be used for vacuum items.</summary>
        private readonly PerScreen<IDictionary<Chest, StorageController>> Chests = new();

        public VacuumChestController(AssetController assetController, IMonitor monitor, IModEvents events, bool firstRow)
        {
            _assetController = assetController;
            _monitor = monitor;
            _firstRow = firstRow;

            events.GameLoop.SaveLoaded += OnSaveLoaded;
            events.Player.InventoryChanged += OnInventoryChanged;
        }

        public bool Any()
        {
            return Chests.Value != null && Chests.Value.Any();
        }

        public bool TryGetPrioritized(Item item, out IList<Chest> storages)
        {
            if (Chests.Value == null)
            {
                storages = new List<Chest>();
                return false;
            }

            storages = Chests.Value
                .Where(s => s.Value.Filter(item))
                .Select(s => s.Key)
                .OrderByDescending(s => s.modData.TryGetValue(ChestsAnywhereOrderKey, out var order) ? Convert.ToInt32(order) : 0)
                .ToList();
            return storages.Any();
        }

        /// <summary>Raised after loading a save (including the first day after creating a new save), or connecting to a multiplayer world.</summary>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.IsLocalPlayer)
                return;
            RefreshChests(Game1.player);
        }

        /// <summary>Raised after items are added or removed from the player inventory.</summary>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;
            RefreshChests(e.Player);
        }

        private void RefreshChests(Farmer who)
        {
            Chests.Value = who.Items
                .Take(_firstRow ? 12 : who.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, i => _assetController.TryGetStorage(i, out var storage) ? storage : null)
                .Where(s => s.Value?.Config.Option("VacuumItems", true) == StorageConfigController.Choice.Enable)
                .ToDictionary(s => s.Key, s => s.Value);
            _monitor.VerboseLog($"Found {Chests.Value.Count} For Vacuum:\n" + string.Join("\n", Chests.Value.Select(s => $"\t{s.Key}")));
        }
    }
}