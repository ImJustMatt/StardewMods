using System;
using ImJustMatt.Common.Extensions;

namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    internal class StorageMenu
    {
        internal readonly int Capacity;

        internal readonly int Offset;

        internal readonly int Padding;

        internal readonly int Rows;

        internal StorageMenu(StorageConfig storage)
        {
            Capacity = storage.Capacity switch
            {
                0 => -1, // Vanilla
                { } capacity when capacity < 0 => 72, // Unlimited
                { } capacity when capacity < 12 => capacity,
                { } capacity => Math.Min(72, capacity.RoundUp(12)) // Specific
            };

            Rows = Math.Max(3, (int) Math.Ceiling(Capacity / 12f));

            Padding = storage.Option("ShowSearchBar") == StorageConfig.Choice.Enable ? 24 : 0;

            Offset = 64 * (Rows - 3);
        }
    }
}