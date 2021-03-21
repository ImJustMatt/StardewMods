using Pathoschild.Stardew.Automate;
using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.Automate
{
    internal class AutomateIntegration : ModIntegration<IAutomateAPI>
    {
        public AutomateIntegration(IModRegistry modRegistry)
            : base(modRegistry, "Pathoschild.Automate")
        {
        }
    }
}