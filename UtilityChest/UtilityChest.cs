using System.Diagnostics.CodeAnalysis;
using ImJustMatt.Common.Integrations.JsonAssets;
using ImJustMatt.Common.Patches;
using ImJustMatt.ExpandedStorage.API;
using ImJustMatt.UtilityChest.Framework.Extensions;
using ImJustMatt.UtilityChest.Framework.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.UtilityChest
{
    public class UtilityChest : Mod
    {
        internal static int ObjectId;
        internal readonly PerScreen<Chest> CurrentChest = new();
        private IExpandedStorageAPI _expandedStorageAPI;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;

            new Patcher(this).ApplyAll(
                typeof(Game1Patches),
                typeof(FarmerPatches),
                typeof(ChestPatches)
            );
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Load Expanded Storage content
            _expandedStorageAPI = Helper.ModRegistry.GetApi<IExpandedStorageAPI>("furyx639.ExpandedStorage");
            //_expandedStorageAPI.LoadContentPack(Path.Combine(Helper.DirectoryPath, "assets", "UtilityChest"));

            // Get ParentSheetIndex for object
            var jsonAssets = new JsonAssetsIntegration(Helper.ModRegistry);
            if (jsonAssets.IsLoaded)
                jsonAssets.API.IdsAssigned += delegate { ObjectId = jsonAssets.API.GetBigCraftableId("Garbage Can"); };
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsPlayerFree) return;
            if (Game1.player.CurrentItem is not Chest chest)
            {
                CurrentChest.Value = null;
                return;
            }
            if (ReferenceEquals(chest, CurrentChest.Value)) return;
            CurrentChest.Value = chest;
        }

        /// <summary>Raised after the player pressed a keyboard, mouse, or controller button.</summary>
        [SuppressMessage("ReSharper", "InvertIf")]
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (CurrentChest.Value is not { } chest) return;
            var location = Game1.player.currentLocation;
            var pos = e.Cursor.Tile;
            if (!Utility.withinRadiusOfPlayer((int) (64 * pos.X), (int) (64 * pos.Y), 1, Game1.player)) return;

            if (e.Button.IsUseToolButton() && chest.CurrentItem() is { } item)
            {
                switch (item)
                {
                    case Tool:
                        Game1.player.BeginUsingTool();
                        break;
                    case Object obj when obj.placementAction(location, (int) pos.X * 64, (int) pos.Y * 64, Game1.player):
                        if (--item.Stack <= 0) chest.items.Remove(item);
                        break;
                }
                Helper.Input.Suppress(e.Button);
            }
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (!Helper.Input.IsDown(SButton.LeftShift) && !Helper.Input.IsDown(SButton.RightShift)) return;
            CurrentChest.Value?.Scroll(e.Delta);
            if (CurrentChest.Value?.CurrentItem() is { } item)
            {
                Monitor.Log($"{item.Name}");
            }
        }
    }
}