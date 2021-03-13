using System.Collections.Generic;
using System.Linq;

namespace ImJustMatt.CustomBundles.Models
{
    public class Bundle
    {
        public string BundleName { get; set; }
        public BundleReward BundleReward { get; set; }
        public IList<BundleItem> BundleItems { get; set; }
        public int ColorIndex { get; set; }
        public int NumberOfItems { get; set; }
        public string TranslatedName { get; set; }

        public string GetData => string.Join("/",
            BundleName,
            $"{BundleReward.ObjectType} {BundleReward.ObjectId} {BundleReward.NumberGiven}",
            string.Join(" ",
                BundleItems.Select(i => $"{i.ObjectId} {i.NumberNeeded} {i.MinimumQuality}")
            ),
            ColorIndex,
            NumberOfItems,
            TranslatedName
        );

        public bool HasData => BundleItems?.Any() ?? false;
    }
}