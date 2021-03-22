using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using StardewModdingAPI;

namespace Helpers.ConfigData
{
    internal interface IFieldHandler
    {
        bool CanHandle(IField field);
        object GetValue(object instance, IField field);
        void SetValue(object instance, IField field, object value);
        void CopyValue(IField field, object source, object target);
        void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IField field);
    }
}