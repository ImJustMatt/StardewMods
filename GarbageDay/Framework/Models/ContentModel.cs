using System.Collections.Generic;
using ImJustMatt.GarbageDay.API;

namespace ImJustMatt.GarbageDay.Framework.Models
{
    internal class ContentModel : IContent
    {
        public IDictionary<string, IDictionary<string, double>> Loot { get; set; } = new Dictionary<string, IDictionary<string, double>>();
    }
}