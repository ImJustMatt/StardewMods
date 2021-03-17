using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace ImJustMatt.ExpandedStorage.Common.Helpers
{
    internal class ConfigHelper : ConfigHelper.IFieldHandler
    {
        private const int ColumnWidth = 25;
        public readonly IFieldHandler FieldHandler;
        public readonly IList<Field> Fields = new List<Field>();

        internal ConfigHelper(object instance, IEnumerable<KeyValuePair<string, string>> fields) : this(null, instance, fields)
        {
        }

        internal ConfigHelper(IFieldHandler fieldHandler, object instance, IEnumerable<KeyValuePair<string, string>> fields)
        {
            FieldHandler = fieldHandler;
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

        public bool CanHandle(IField field) => Fields.Any(p => p.Equals(field));

        public object GetValue(object instance, IField field)
        {
            if (FieldHandler?.CanHandle(field) ?? false)
            {
                return FieldHandler.GetValue(instance, field);
            }

            if (field.Info == null)
                return null;
            var value = field.Info.GetValue(instance, null);
            return value switch
            {
                IList<string> listValues => string.Join(", ", listValues),
                HashSet<string> listValues => string.Join(", ", listValues),
                _ => value
            };
        }

        public void SetValue(object instance, IField field, object value)
        {
            if (FieldHandler?.CanHandle(field) ?? false)
            {
                FieldHandler.SetValue(instance, field, value);
            }

            field.Info?.SetValue(instance, value);
        }

        public void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IField field)
        {
            if (FieldHandler?.CanHandle(field) ?? false)
            {
                FieldHandler.RegisterConfigOption(manifest, modConfigMenu, instance, field);
                return;
            }

            if (field.Info?.PropertyType == null)
            {
                return;
            }

            if (field.Info.PropertyType == typeof(KeybindList))
            {
                modConfigMenu.API.RegisterSimpleOption(manifest,
                    field.DisplayName,
                    field.Description,
                    () => (KeybindList) field.Info.GetValue(instance, null),
                    value => field.Info.SetValue(instance, value));
            }
            else if (field.Info.PropertyType == typeof(bool))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    field.DisplayName,
                    field.Description,
                    () => (bool) field.Info.GetValue(instance, null),
                    value => field.Info.SetValue(instance, value)
                );
            }
            else if (field.Info.PropertyType == typeof(int))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    field.DisplayName,
                    field.Description,
                    () => (int) field.Info.GetValue(instance, null),
                    value => field.Info.SetValue(instance, value)
                );
            }
            else if (field.Info.PropertyType == typeof(string))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    field.DisplayName,
                    field.Description,
                    () => (string) field.Info.GetValue(instance, null),
                    value => field.Info.SetValue(instance, value)
                );
            }
        }

        internal string Summary(object instance, bool header = true) =>
            (header ? $"{"Property",-ColumnWidth} | Value\n{new string('-', ColumnWidth)}-|-{new string('-', ColumnWidth)}\n" : "") +
            string.Join("\n", Fields
                .Select(field => new KeyValuePair<string, object>(field.DisplayName, GetValue(instance, field)))
                .Where(field => field.Value != null && (field.Value is not string value || !string.IsNullOrWhiteSpace(value)))
                .Select(field => $"{field.Key,-25} | {field.Value}")
                .ToList());

        internal interface IField
        {
            string DisplayName { get; }
            string Name { get; set; }
            string Description { get; set; }
            PropertyInfo Info { get; set; }
            object DefaultValue { get; set; }
        }

        internal class Field : IField
        {
            public string DisplayName => Regex.Replace(Name, "(\\B[A-Z])", " $1");
            public string Name { get; set; }
            public string Description { get; set; }
            public PropertyInfo Info { get; set; }
            public object DefaultValue { get; set; }
        }

        internal interface IFieldHandler
        {
            bool CanHandle(IField field);
            object GetValue(object instance, IField field);
            void SetValue(object instance, IField field, object value);
            void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IField field);
        }
    }
}