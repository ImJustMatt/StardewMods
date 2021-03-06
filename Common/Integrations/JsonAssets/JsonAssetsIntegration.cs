using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.JsonAssets
{
    internal class JsonAssetsIntegration : ModIntegration<IJsonAssetsAPI>
    {
        public JsonAssetsIntegration(IModRegistry modRegistry)
            : base(modRegistry, "spacechase0.JsonAssets")
        {
        }
    }
}