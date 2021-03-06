using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations
{
    internal abstract class ModIntegration<T> where T : class
    {
        protected internal readonly bool IsLoaded;
        protected internal readonly T API;
        internal ModIntegration(IModRegistry modRegistry, string modUniqueId)
        {
            IsLoaded = modRegistry.IsLoaded(modUniqueId);
            API = modRegistry.GetApi<T>(modUniqueId);
        }
    }
}