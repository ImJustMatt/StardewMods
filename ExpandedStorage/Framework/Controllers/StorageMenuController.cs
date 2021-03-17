using System;
using ImJustMatt.Common.Extensions;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class StorageMenuController
    {
        internal readonly int Capacity;

        internal readonly int Offset;

        internal readonly int Padding;

        internal readonly int Rows;

        internal StorageMenuController(StorageConfigController config)
        {
            Capacity = config.Capacity switch
            {
                0 => -1, // Vanilla
                { } capacity when capacity < 0 => 72, // Unlimited
                { } capacity when capacity < 12 => capacity,
                { } capacity => Math.Min(72, capacity.RoundUp(12)) // Specific
            };

            Rows = Capacity > 0 ? (int) Math.Ceiling(Capacity / 12f) : 3;

            Padding = config.Option("ShowSearchBar", true) == StorageConfigController.Choice.Enable ? 24 : 0;

            Offset = 64 * (Rows - 3);
        }
    }
}