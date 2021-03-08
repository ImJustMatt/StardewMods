using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImJustMatt.ExpandedStorage.Common.Helpers
{
    internal class TableSummary
    {
        private readonly IDictionary<string, string> _properties;

        internal TableSummary(IDictionary<string, string> properties)
        {
            _properties = properties;
        }

        internal string Report(object instance, bool header = true) =>
            (header ? $"{"Property",-25} | Value\n{new string('-', 26)}|{new string('-', 7)}\n" : "") +
            string.Join("\n", _properties
                .Select(property => new KeyValuePair<string, object>(PascalCaseToSpace(property.Key), ValueOfProperty(property.Key, instance)))
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
    }
}