using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations.JsonAssets
{
    internal class JsonAssetsIntegration : ModIntegration<IJsonAssetsAPI>
    {
        public JsonAssetsIntegration(IModRegistry modRegistry, string modUniqueId)
            : base(modRegistry, modUniqueId)
        {
        }
    }
}