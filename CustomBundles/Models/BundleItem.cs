namespace ImJustMatt.CustomBundles.Models
{
    public class BundleItem
    {
        /// <summary>ParentSheetIndex of Object needed for bundle</summary>
        public int ObjectId { get; set; }

        /// <summary>The number of items needed</summary>
        public int NumberNeeded { get; set; }

        /// <summary>Minimum quality required for items</summary>
        public int MinimumQuality { get; set; }
    }
}