using System.Linq;
using StardewValley;
using StardewValley.Objects;

namespace ImJustMatt.UtilityChest.Framework.Extensions
{
    public static class ChestExtensions
    {
        private const string CurrentSlotKey = "furyx639.UtilityChest/CurrentSlot";

        public static Item CurrentItem(this Chest chest)
        {
            if (chest.modData.TryGetValue(CurrentSlotKey, out var slotStr) && int.TryParse(slotStr, out var slot)) return chest.items.ElementAtOrDefault(slot);
            chest.modData[CurrentSlotKey] = "0";
            return chest.items.ElementAtOrDefault(0);
        }

        public static void Scroll(this Chest chest, int delta)
        {
            if (!chest.modData.TryGetValue(CurrentSlotKey, out var slotStr) || !int.TryParse(slotStr, out var slot)) slot = 0;
            switch (delta)
            {
                case > 0:
                    slot++;
                    break;
                case < 0:
                    slot--;
                    break;
            }
            if (slot < 0) slot = chest.items.Count - 1;
            if (slot >= chest.items.Count) slot = 0;
            chest.modData[CurrentSlotKey] = slot.ToString();
        }
    }
}