namespace ImJustMatt.ExpandedStorage.Framework.Models
{
    internal class ContentModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string UniqueID { get; set; }
        public string[] UpdateKeys { get; set; } = { };
    }
}