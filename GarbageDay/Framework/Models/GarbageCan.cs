using System;
using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Extensions;
using ImJustMatt.ExpandedStorage.Common.Helpers.ItemData;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.GarbageDay.Framework.Models
{
    internal class GarbageCan
    {
        private readonly ModConfig _config;
        private readonly IContentHelper _contentHelper;
        private readonly Multiplayer _multiplayer;
        private Chest _chest;
        private bool _doubleMega;
        private bool _dropQiBeans;
        private bool _garbageChecked = true;
        private IEnumerable<SearchableItem> _items;
        private bool _mega;
        private NPC _npc;
        internal GameLocation Location;
        internal string MapName;
        internal Vector2 Tile;

        internal string WhichCan;

        internal GarbageCan(IContentHelper contentHelper, IModEvents modEvents, IReflectionHelper reflection, ModConfig config)
        {
            _contentHelper = contentHelper;
            _config = config;
            _multiplayer = reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Events
            modEvents.Display.MenuChanged += OnMenuChanged;
        }

        internal Chest Chest => _chest ??= Location.Objects.TryGetValue(Tile, out var obj) && obj is Chest chest ? chest : null;

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (_npc == null || e.OldMenu is not ItemGrabMenu || e.NewMenu != null)
                return;
            Game1.drawDialogue(_npc);
            _npc = null;
        }

        internal bool OpenCan()
        {
            // NPC Reaction
            _npc = null;
            var character = Utility.isThereAFarmerOrCharacterWithinDistance(new Vector2(Tile.X, Tile.Y), 7, Location);
            if (character is NPC npc && character is not Horse)
            {
                _npc = npc;
                _multiplayer.globalChatInfoMessage("TrashCan", Game1.player.Name, npc.Name);
                if (npc.Name.Equals("Linus"))
                {
                    npc.doEmote(32);
                    npc.setNewDialogue(Game1.content.LoadString("Data\\ExtraDialogue:Town_DumpsterDiveComment_Linus"), true, true);
                    Game1.player.changeFriendship(5, npc);
                    _multiplayer.globalChatInfoMessage("LinusTrashCan");
                }
                else
                    switch (npc.Age)
                    {
                        case 2:
                            npc.doEmote(28);
                            npc.setNewDialogue(Game1.content.LoadString("Data\\ExtraDialogue:Town_DumpsterDiveComment_Child"), true, true);
                            Game1.player.changeFriendship(-25, npc);
                            break;
                        case 1:
                            npc.doEmote(8);
                            npc.setNewDialogue(Game1.content.LoadString("Data\\ExtraDialogue:Town_DumpsterDiveComment_Teen"), true, true);
                            Game1.player.changeFriendship(-25, npc);
                            break;
                        default:
                            npc.doEmote(12);
                            npc.setNewDialogue(Game1.content.LoadString("Data\\ExtraDialogue:Town_DumpsterDiveComment_Adult"), true, true);
                            Game1.player.changeFriendship(-25, npc);
                            break;
                    }
            }

            if (_garbageChecked) return true;
            _garbageChecked = true;
            Game1.stats.incrementStat("trashCansChecked", 1);

            // Drop Item
            if (_dropQiBeans)
            {
                var origin = new Vector2(Tile.X + 0.5f, Tile.Y - 1) * 64f;
                Game1.createItemDebris(new Object(890, 1), origin, 2, Location, (int) origin.Y + 64);
                return false;
            }

            // Give Hat
            if (_doubleMega)
            {
                Location!.playSound("explosion");
                Chest.playerChoiceColor.Value = Color.Black; // Remove Lid
                Game1.player.addItemByMenuIfNecessary(new Hat(66));
                return false;
            }

            if (_mega)
            {
                Location!.playSound("crit");
            }

            return true;
        }

        internal void DayStart()
        {
            // Reset State
            _garbageChecked = false;
            _dropQiBeans = false;
            Chest.playerChoiceColor.Value = Color.Gray;

            if (Game1.dayOfMonth % 7 == _config.GarbageDay)
            {
                Chest.items.Clear();
            }

            // Seed Random
            if (!int.TryParse(WhichCan, out var whichCan)) whichCan = 0;
            var garbageRandom = SeedRandom(whichCan);

            // Mega/Double-Mega
            _mega = Game1.stats.getStat("trashCansChecked") > 20 && garbageRandom.NextDouble() < 0.01;
            _doubleMega = Game1.stats.getStat("trashCansChecked") > 20 && garbageRandom.NextDouble() < 0.002;
            if (_doubleMega || !_mega && !(garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck))
                return;

            // Qi Beans
            if (Game1.random.NextDouble() <= 0.25 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
            {
                _dropQiBeans = true;
                return;
            }

            // Vanilla Local Loot
            if (whichCan >= 3 && whichCan <= 7)
            {
                var localLoot = GetVanillaLocalLoot(garbageRandom, whichCan);
                if (localLoot != -1)
                {
                    Chest.addItem(new Object(localLoot, 1));
                    return;
                }
            }

            // Custom Local Loot
            if (garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck)
            {
                var localItem = GetLocalLoot(garbageRandom, WhichCan);
                if (localItem != null)
                {
                    Chest.addItem(localItem.CreateItem());
                    return;
                }
            }

            // Global Loot
            if (garbageRandom.NextDouble() < _config.GetRandomItemFromSeason)
            {
                var globalLoot = Utility.getRandomItemFromSeason(Game1.currentSeason, (int) (Tile.X * 653 + Tile.Y * 777), false);
                if (globalLoot != -1)
                {
                    Chest.addItem(new Object(globalLoot, 1));
                    return;
                }
            }

            var globalItem = GetGlobalLoot(garbageRandom);
            if (globalItem != null)
            {
                Chest.addItem(globalItem.CreateItem());
            }
        }

        private SearchableItem GetGlobalLoot(Random randomizer)
        {
            return RandomLoot(randomizer, "Mods/furyx639.GarbageDay/GlobalLoot");
        }

        private SearchableItem GetLocalLoot(Random randomizer, string whichCan)
        {
            return RandomLoot(randomizer, $"Mods/furyx639.GarbageDay/Loot/{whichCan}");
        }

        private SearchableItem RandomLoot(Random randomizer, string path)
        {
            path = PathUtilities.NormalizePath(path);
            var lootTable = _contentHelper.Load<Dictionary<string, double>>(path, ContentSource.GameContent);
            if (lootTable == null || !lootTable.Any())
                return null;
            var totalWeight = lootTable.Values.Sum();
            var targetIndex = randomizer.NextDouble() * totalWeight;
            double currentIndex = 0;
            foreach (var lootItem in lootTable)
            {
                currentIndex += lootItem.Value;
                if (currentIndex < targetIndex)
                    continue;
                return (_items ??= new ItemRepository().GetAll())
                    .Where(entry => entry.Item.MatchesTagExt(lootItem.Key))
                    .Shuffle()
                    .FirstOrDefault();
            }

            return null;
        }

        private static int GetVanillaLocalLoot(Random garbageRandom, int whichCan)
        {
            var item = -1;
            switch (whichCan)
            {
                case 3 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                    return garbageRandom.NextDouble() < 0.05 ? 749 : 535;
                case 4 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                    return 378 + garbageRandom.Next(3) * 2;
                case 5 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck && Game1.dishOfTheDay != null:
                    return Game1.dishOfTheDay.ParentSheetIndex != 217 ? Game1.dishOfTheDay.ParentSheetIndex : 216;
                case 6 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                    return 223;
                case 7 when garbageRandom.NextDouble() < 0.2:
                    if (!Utility.HasAnyPlayerSeenEvent(191393)) item = 167;
                    if (Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater")
                        && !Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja"))
                    {
                        item = !(garbageRandom.NextDouble() < 0.25) ? 270 : 809;
                    }

                    break;
            }

            return item;
        }

        private static Random SeedRandom(int whichCan)
        {
            var garbageRandom = new Random((int) Game1.uniqueIDForThisGame / 2 + (int) Game1.stats.DaysPlayed + 777 + whichCan * 77);
            var prewarm = garbageRandom.Next(0, 100);
            for (var k = 0; k < prewarm; k++)
            {
                garbageRandom.NextDouble();
            }

            prewarm = garbageRandom.Next(0, 100);
            for (var j = 0; j < prewarm; j++)
            {
                garbageRandom.NextDouble();
            }

            return garbageRandom;
        }
    }
}