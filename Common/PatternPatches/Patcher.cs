using System;
using Harmony;
using StardewModdingAPI;

namespace ImJustMatt.Common.PatternPatches
{
    internal class Patcher<T> where T : IMod
    {
        private readonly IMod _mod;
        private readonly string _uniqueId;

        internal Patcher(IMod mod)
        {
            _mod = mod;
            _uniqueId = mod.ModManifest.UniqueID;
        }

        internal void ApplyAll(params Type[] patchTypes)
        {
            var harmony = HarmonyInstance.Create(_uniqueId);
            foreach (var patchType in patchTypes)
            {
                ((BasePatch<T>) Activator.CreateInstance(patchType, _mod)).Apply(harmony);
            }
        }
    }
}