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

        private readonly ExpandedStorage _mod;

        /// <summary>Tracks all chests that may be used for vacuum items.</summary>
        private readonly PerScreen<IDictionary<Chest, StorageController>> _chests = new();

        public VacuumChestController(ExpandedStorage mod)
        {
            _mod = mod;
            _mod.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            _mod.Helper.Events.Player.InventoryChanged += OnInventoryChanged;
        }

        public bool Any()
        {
            return _chests.Value != null && _chests.Value.Any();
        }

        public bool TryGetPrioritized(Item item, out IList<Chest> storages)
        {
            if (_chests.Value == null)
            {
                storages = new List<Chest>();
                return false;
            }

            storages = _chests.Value
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
            _chests.Value = who.Items
                .Take(_mod.Config.VacuumToFirstRow ? 12 : who.MaxItems)
                .OfType<Chest>()
                .ToDictionary(i => i, i => _mod.AssetController.TryGetStorage(i, out var storage) ? storage : null)
                .Where(s => s.Value?.Config.Option("VacuumItems", true) == StorageConfigController.Choice.Enable)
                .ToDictionary(s => s.Key, s => s.Value);
            _mod.Monitor.VerboseLog($"Found {_chests.Value.Count} For Vacuum:\n" + string.Join("\n", _chests.Value.Select(s => $"\t{s.Key}")));
        }
    }
}