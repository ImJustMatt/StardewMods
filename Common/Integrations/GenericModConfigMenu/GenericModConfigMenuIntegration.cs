using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.GenericModConfigMenu
{
    internal class GenericModConfigMenuIntegration : ModIntegration<IGenericModConfigMenuAPI>
    {
        public GenericModConfigMenuIntegration(IModRegistry modRegistry, string modUniqueId)
            : base(modRegistry, modUniqueId)
        {
        }
    }
}