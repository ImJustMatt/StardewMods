using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace ImJustMatt.GarbageDay.Framework.Controllers
{
    internal static class CommandsController
    {
        public static readonly IList<Command> Commands = new List<Command>
        {
            new()
            {
                Name = "fill_garbage_cans",
                Documentation = "Adds loot to all Garbage Cans.\n\nUsage: fill_garbage_cans <luck>\n- luck: Adds to player luck",
                Callback = FillGarbageCans
            },
            new()
            {
                Name = "remove_garbage_cans",
                Documentation = "Remove all Garbage Cans. Run before saving to safely uninstall mod.",
                Callback = RemoveGarbageCans
            },
            new()
            {
                Name = "reset_garbage_cans",
                Documentation = "Resets all Garbage Cans by removing and replacing them.",
                Callback = ResetGarbageCans
            }
        };

        private static void FillGarbageCans(string command, string[] args)
        {
            var luck = float.TryParse(args?[0], out var luckFloat) ? luckFloat : 0;
            foreach (var garbageCan in GarbageDay.GarbageCans)
            {
                garbageCan.Value.DayStart(luck);
            }
        }

        private static void RemoveGarbageCans(string command, string[] args)
        {
            foreach (var garbageCan in GarbageDay.GarbageCans.Values)
            {
                if (garbageCan.Chest != null) garbageCan.Location.Objects.Remove(garbageCan.Tile);
            }
        }

        private static void ResetGarbageCans(string command, string[] args)
        {
            foreach (var garbageCan in GarbageDay.GarbageCans)
            {
                var chest = new Chest(true, garbageCan.Value.Tile, GarbageDay.ObjectId);
                chest.playerChoiceColor.Value = Color.DarkGray;
                chest.modData.Add("furyx639.GarbageDay", garbageCan.Key);
                chest.modData.Add("Pathoschild.ChestsAnywhere/IsIgnored", "true");
                if (garbageCan.Value.Chest != null)
                {
                    chest.items.CopyFrom(garbageCan.Value.Chest.items);
                    garbageCan.Value.Location.Objects.Remove(garbageCan.Value.Tile);
                }

                garbageCan.Value.Location.Objects.Add(garbageCan.Value.Tile, chest);
            }
        }

        internal class Command
        {
            public Action<string, string[]> Callback;
            public string Documentation;
            public string Name;
        }
    }
}