using Harmony;
using StardewModdingAPI;

namespace ImJustMatt.Common.Patches
{
    internal abstract class BasePatch<T> where T : IMod
    {
        private protected static IMonitor Monitor;
        private protected static T Mod;

        internal BasePatch(IMod mod, HarmonyInstance harmony)
        {
            Mod = (T) mod;
            Monitor = mod.Monitor;
        }
    }
}