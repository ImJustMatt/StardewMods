using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.GenericModConfigMenu
{
    internal class GenericModConfigMenuIntegration : ModIntegration<IGenericModConfigMenuAPI>
    {
        public GenericModConfigMenuIntegration(IModRegistry modRegistry)
            : base(modRegistry, "spacechase0.GenericModConfigMenu")
        {
        }
    }
}