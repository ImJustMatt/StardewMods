namespace ImJustMatt.CustomBundles.Models
{
    public class BundleReward
    {
        /// <summary>The type of object rewarded</summary>
        public string ObjectType { get; set; }

        /// <summary>ParentSheetIndex of object rewarded</summary>
        public int ObjectId { get; set; }

        /// <summary>The number of items rewarded</summary>
        public int NumberGiven { get; set; }
    }
}