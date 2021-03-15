using ImJustMatt.ExpandedStorage.Common.Helpers;
using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.GenericModConfigMenu
{
    internal class GenericModConfigMenuIntegration : ModIntegration<IGenericModConfigMenuAPI>
    {
        public GenericModConfigMenuIntegration(IModRegistry modRegistry)
            : base(modRegistry, "spacechase0.GenericModConfigMenu")
        {
        }

        public void RegisterConfigOptions(IManifest manifest, ConfigHelper configHelper, object instance)
        {
            if (!IsLoaded)
                return;

            foreach (var property in configHelper.Properties)
            {
                configHelper.RegisterConfigOption(manifest, this, instance, property);
            }
        }
    }
}