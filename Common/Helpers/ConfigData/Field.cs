using System.Reflection;
using System.Text.RegularExpressions;

namespace Helpers.ConfigData
{
    internal class Field : IField
    {
        public string DisplayName => Regex.Replace(Name, "(\\B[A-Z])", " $1");
        public string Name { get; set; }
        public string Description { get; set; }
        public PropertyInfo Info { get; set; }
        public object DefaultValue { get; set; }
    }
}