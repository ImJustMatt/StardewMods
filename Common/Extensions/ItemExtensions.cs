using System;
using System.Linq;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace ImJustMatt.Common.Extensions
{
    internal static class ItemExtensions
    {
        private const string CategoryFurniture = "category_furniture";
        private const string CategoryArtifact = "category_artifact";
        private const string DonateMuseum = "donate_museum";
        private const string DonateBundle = "donate_bundle";
        
        public static bool MatchesTagExt(this Item item, string search)
        {
            return item.MatchesTagExt(search, true);
        }

        public static bool MatchesTagExt(this Item item, string search, bool exactMatch)
        {
            return item switch
            {
                Furniture when TagEquals(search, CategoryFurniture, exactMatch) => true,
                Object {Type: "Arch"} when TagEquals(search, CategoryArtifact, exactMatch) => true,
                Object {Type: "Arch"} when TagEquals(search, DonateMuseum, exactMatch) => CanDonateToMuseum(item),
                Object {Type: "Minerals"} when TagEquals(search, DonateMuseum, exactMatch) => CanDonateToMuseum(item),
                Object obj when TagEquals(search, DonateBundle, exactMatch) => CanDonateToBundle(obj),
                _ => item.GetContextTags().Any(tag => TagEquals(search, tag, exactMatch))
            };
        }

        private static bool TagEquals(string search, string match, bool exact)
        {
            return exact && search.Equals(match) || match.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        private static bool CanDonateToMuseum(Item item)
        {
            return Game1.locations
                       .OfType<LibraryMuseum>()
                       .FirstOrDefault()?.isItemSuitableForDonation(item)
                   ?? false;
        }

        private static bool CanDonateToBundle(Object obj)
        {
            return Game1.locations
                       .OfType<CommunityCenter>()
                       .FirstOrDefault()?.couldThisIngredienteBeUsedInABundle(obj)
                   ?? false;
        }
    }
}