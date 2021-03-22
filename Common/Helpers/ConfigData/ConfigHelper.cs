using System.Collections.Generic;
using System.Linq;

namespace Helpers.ConfigData
{
    internal class ConfigHelper
    {
        private const int ColumnWidth = 25;
        public readonly FieldHandler FieldHandler;
        public readonly IList<Field> Fields = new List<Field>();

        internal ConfigHelper(object instance, IEnumerable<KeyValuePair<string, string>> fields) : this(null, instance, fields)
        {
        }

        internal ConfigHelper(IFieldHandler customHandler, object instance, IEnumerable<KeyValuePair<string, string>> fields)
        {
            FieldHandler = new FieldHandler(customHandler);
            foreach (var field in fields)
            {
                var fieldInfo = instance.GetType().GetProperty(field.Key);
                Fields.Add(new Field
                {
                    Name = field.Key,
                    Description = field.Value,
                    Info = fieldInfo,
                    DefaultValue = fieldInfo?.GetValue(instance)
                });
            }
        }

        internal void CopyValues(object source, object target)
        {
            foreach (var field in Fields)
            {
                FieldHandler.CopyValue(field, source, target);
            }
        }

        internal string Summary(object instance, bool header = true) =>
            (header ? $"{"Property",-ColumnWidth} | Value\n{new string('-', ColumnWidth)}-|-{new string('-', ColumnWidth)}\n" : "") +
            string.Join("\n", Fields
                .Select(field => new KeyValuePair<string, object>(field.DisplayName, FieldHandler.GetValue(instance, field)))
                .Where(field => field.Value != null && (field.Value is not string value || !string.IsNullOrWhiteSpace(value)))
                .Select(field => $"{field.Key,-25} | {field.Value}")
                .ToList());
    }
}