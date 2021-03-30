using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
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
            var luck = float.TryParse(args.ElementAtOrDefault(0), out var luckFloat) ? luckFloat : 0;
            GarbageDay.GarbageCans.Values.Do(garbageCan => garbageCan.DayStart(luck));
        }

        private static void RemoveGarbageCans(string command, string[] args)
        {
            GarbageDay.GarbageCans.Values.Do(garbageCan => garbageCan.Remove());
        }

        private static void ResetGarbageCans(string command, string[] args)
        {
            GarbageDay.GarbageCans.Do(garbageCan => garbageCan.Value.Add(garbageCan.Key));
        }

        internal class Command
        {
            public Action<string, string[]> Callback;
            public string Documentation;
            public string Name;
        }
    }
}