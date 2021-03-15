using StardewModdingAPI;

namespace ImJustMatt.Common.Integrations
{
    internal abstract class ModIntegration<T> where T : class
    {
        private readonly IModRegistry _modRegistry;
        private readonly string _modUniqueId;
        private T _api;

        internal ModIntegration(IModRegistry modRegistry, string modUniqueId)
        {
            _modRegistry = modRegistry;
            _modUniqueId = modUniqueId;
        }

        protected internal T API => _api ??= _modRegistry.GetApi<T>(_modUniqueId);
        protected internal bool IsLoaded => _modRegistry.IsLoaded(_modUniqueId);
    }
}