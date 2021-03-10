using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImJustMatt.ExpandedStorage.Common.Helpers
{
    internal class ConfigHelper
    {
        private const int ColumnWidth = 25;
        private readonly IList<KeyValuePair<string, string>> _properties;
        private GetValueOfProperty _getValue;

        internal ConfigHelper(IList<KeyValuePair<string, string>> properties) : this(null, properties)
        {
        }

        internal ConfigHelper(GetValueOfProperty getValue, IList<KeyValuePair<string, string>> properties)
        {
            _properties = properties;
            _getValue = getValue ?? ValueOfProperty;
        }

        internal IDictionary<string, string> Properties => _properties.ToDictionary(p => p.Key, p => p.Value);

        internal string Summary(object instance, bool header = true) =>
            (header ? $"{"Property",-ColumnWidth} | Value\n{new string('-', ColumnWidth)}-|-{new string('-', ColumnWidth)}\n" : "") +
            string.Join("\n", _properties
                .Select(property => new KeyValuePair<string, object>(PascalCaseToSpace(property.Key), _getValue.Invoke(property.Key, instance)))
                .Where(property => property.Value != null && (property.Value is not string value || !string.IsNullOrWhiteSpace(value)))
                .Select(property => $"{property.Key,-25} | {property.Value}")
                .ToList());

        private static string PascalCaseToSpace(string value) =>
            Regex.Replace(value, "(\\B[A-Z])", " $1");

        private static object ValueOfProperty(string property, object instance)
        {
            var value = instance.GetType().GetProperty(property)?.GetValue(instance, null);
            return value switch
            {
                IList<string> listValues => string.Join(", ", listValues),
                HashSet<string> listValues => string.Join(", ", listValues),
                _ => value
            };
        }

        internal delegate object GetValueOfProperty(string property, object instance);
    }
}