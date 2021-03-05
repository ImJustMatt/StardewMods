using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.GarbageDay.Framework.Models
{
    internal class GarbageCan
    {
        private static IReflectionHelper _reflection;
        private Chest _chest;
        private bool _doubleMega;
        private bool _dropQiBeans;
        private bool _garbageChecked = true;
        private bool _mega;
        private Multiplayer _multiplayer;
        internal GameLocation Location;
        internal string MapName;
        internal Vector2 Tile;

        internal int WhichCan;

        internal GarbageCan()
        {
            _multiplayer = _reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
        }

        internal Chest Chest => _chest ??= Location.Objects.TryGetValue(Tile, out var obj) && obj is Chest chest ? chest : null;

        internal static void Init(IReflectionHelper reflection)
        {
            _reflection = reflection;
        }

        internal bool OpenCan()
        {
            if (_garbageChecked) return true;
            _garbageChecked = true;
            Game1.stats.incrementStat("trashCansChecked", 1);

            // NPC Reaction
            var character = Utility.isThereAFarmerOrCharacterWithinDistance(new Vector2(Tile.X, Tile.Y), 7, Location);
            if (character is NPC npc && character is not Horse)
            {
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

                Game1.drawDialogue(npc);
            }

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
                Chest.playerChoiceColor.Value = Color.White; // Remove Lid
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
            Chest.playerChoiceColor.Value = Color.Black;

            var garbageRandom = new Random((int) Game1.uniqueIDForThisGame / 2 + (int) Game1.stats.DaysPlayed + 777 + WhichCan * 77);
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

            _mega = Game1.stats.getStat("trashCansChecked") > 20 && garbageRandom.NextDouble() < 0.01;
            _doubleMega = Game1.stats.getStat("trashCansChecked") > 20 && garbageRandom.NextDouble() < 0.002;
            if (_doubleMega || !_mega && !(garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck))
                return;

            var item = garbageRandom.Next(10) switch
            {
                0 => 168,
                1 => 167,
                2 => 170,
                3 => 171,
                4 => 172,
                5 => 216,
                6 => Utility.getRandomItemFromSeason(Game1.currentSeason, (int) (Tile.X * 653 + Tile.Y * 777), false),
                7 => 403,
                8 => 309 + garbageRandom.Next(3),
                9 => 153,
                _ => 168
            };

            switch (WhichCan)
            {
                case 3 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                {
                    item = 535;
                    if (garbageRandom.NextDouble() < 0.05)
                    {
                        item = 749;
                    }

                    break;
                }
                case 4 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                    item = 378 + garbageRandom.Next(3) * 2;
                    garbageRandom.Next(1, 5);
                    break;
                case 5 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck && Game1.dishOfTheDay != null:
                    item = Game1.dishOfTheDay.ParentSheetIndex != 217 ? Game1.dishOfTheDay.ParentSheetIndex : 216;
                    break;
                case 6 when garbageRandom.NextDouble() < 0.2 + Game1.player.DailyLuck:
                    item = 223;
                    break;
                case 7 when garbageRandom.NextDouble() < 0.2:
                {
                    if (!Utility.HasAnyPlayerSeenEvent(191393))
                    {
                        item = 167;
                    }

                    if (Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater")
                        && !Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja"))
                    {
                        item = !(garbageRandom.NextDouble() < 0.25) ? 270 : 809;
                    }

                    break;
                }
            }

            if (Game1.random.NextDouble() <= 0.25 && Game1.player.team.SpecialOrderRuleActive("DROP_QI_BEANS"))
            {
                _dropQiBeans = true;
            }
            else
            {
                Chest.addItem(new Object(item, 1));
            }
        }
    }
}