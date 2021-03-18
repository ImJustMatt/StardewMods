using System.Collections.Generic;
using ImJustMatt.GarbageDay.API;

namespace ImJustMatt.GarbageDay.Framework.Models
{
    internal class ContentModel : IContent
    {
        public HashSet<string> Maps { get; set; } = new();
        public IDictionary<string, double> GlobalLoot { get; set; } = new Dictionary<string, double>();
        public IDictionary<string, IDictionary<string, double>> LocalLoot { get; set; } = new Dictionary<string, IDictionary<string, double>>();
    }
}