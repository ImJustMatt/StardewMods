using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ImJustMatt.Common.Extensions;
using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace ImJustMatt.ExpandedStorage.Common.Helpers
{
    internal class ConfigHelper : ConfigHelper.IPropertyHandler
    {
        private const int ColumnWidth = 25;
        public readonly IList<Property> Properties = new List<Property>();
        public readonly IPropertyHandler PropertyHandler;

        internal ConfigHelper(object instance, IEnumerable<KeyValuePair<string, string>> properties) : this(null, instance, properties)
        {
        }

        internal ConfigHelper(IPropertyHandler propertyHandler, object instance, IEnumerable<KeyValuePair<string, string>> properties)
        {
            PropertyHandler = propertyHandler;
            foreach (var property in properties)
            {
                var propertyInfo = instance.GetType().GetProperty(property.Key);
                Properties.Add(new Property
                {
                    Name = property.Key,
                    Description = property.Value,
                    Info = propertyInfo,
                    DefaultValue = propertyInfo?.GetValue(instance)
                });
            }
        }

        public bool CanHandle(IProperty property) => Properties.Any(p => p.Equals(property));

        public object GetValue(object instance, IProperty property)
        {
            if (PropertyHandler?.CanHandle(property) ?? false)
            {
                return PropertyHandler.GetValue(instance, property);
            }

            if (property.Info == null)
                return null;
            var value = property.Info.GetValue(instance, null);
            return value switch
            {
                IList<string> listValues => string.Join(", ", listValues),
                HashSet<string> listValues => string.Join(", ", listValues),
                _ => value
            };
        }

        public void SetValue(object instance, IProperty property, object value)
        {
            if (PropertyHandler?.CanHandle(property) ?? false)
            {
                PropertyHandler.SetValue(instance, property, value);
            }

            property.Info?.SetValue(instance, value);
        }

        public void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IProperty property)
        {
            if (PropertyHandler?.CanHandle(property) ?? false)
            {
                PropertyHandler.RegisterConfigOption(manifest, modConfigMenu, instance, property);
                return;
            }

            if (property.Info?.PropertyType == null)
            {
                return;
            }

            if (property.Info.PropertyType == typeof(KeybindList))
            {
                modConfigMenu.API.RegisterSimpleOption(manifest,
                    property.DisplayName,
                    property.Description,
                    () => (SButton) ((property.Info.GetValue(instance, null) as KeybindList)?.GetSingle() ?? property.DefaultValue),
                    value => property.Info.SetValue(instance, KeybindList.ForSingle(value)));
            }
            else if (property.Info.PropertyType == typeof(bool))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    property.DisplayName,
                    property.Description,
                    () => (bool) property.Info.GetValue(instance, null),
                    value => property.Info.SetValue(instance, value)
                );
            }
            else if (property.Info.PropertyType == typeof(int))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    property.DisplayName,
                    property.Description,
                    () => (int) property.Info.GetValue(instance, null),
                    value => property.Info.SetValue(instance, value)
                );
            }
            else if (property.Info.PropertyType == typeof(string))
            {
                modConfigMenu.API.RegisterSimpleOption(
                    manifest,
                    property.DisplayName,
                    property.Description,
                    () => (string) property.Info.GetValue(instance, null),
                    value => property.Info.SetValue(instance, value)
                );
            }
        }

        internal string Summary(object instance, bool header = true) =>
            (header ? $"{"Property",-ColumnWidth} | Value\n{new string('-', ColumnWidth)}-|-{new string('-', ColumnWidth)}\n" : "") +
            string.Join("\n", Properties
                .Select(property => new KeyValuePair<string, object>(property.DisplayName, GetValue(instance, property)))
                .Where(property => property.Value != null && (property.Value is not string value || !string.IsNullOrWhiteSpace(value)))
                .Select(property => $"{property.Key,-25} | {property.Value}")
                .ToList());

        internal interface IProperty
        {
            string DisplayName { get; }
            string Name { get; set; }
            string Description { get; set; }
            PropertyInfo Info { get; set; }
            object DefaultValue { get; set; }
        }

        internal class Property : IProperty
        {
            public string DisplayName => Regex.Replace(Name, "(\\B[A-Z])", " $1");
            public string Name { get; set; }
            public string Description { get; set; }
            public PropertyInfo Info { get; set; }
            public object DefaultValue { get; set; }
        }

        internal interface IPropertyHandler
        {
            bool CanHandle(IProperty property);
            object GetValue(object instance, IProperty property);
            void SetValue(object instance, IProperty property, object value);
            void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IProperty property);
        }
    }
}