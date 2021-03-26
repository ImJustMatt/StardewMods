using System.Collections.Generic;
using System.Linq;
using ImJustMatt.Common.Patches;
using ImJustMatt.SlimeFramework.Framework.Controllers;
using ImJustMatt.SlimeFramework.Framework.Extensions;
using ImJustMatt.SlimeFramework.Framework.Patches;
using StardewModdingAPI;

namespace ImJustMatt.SlimeFramework
{
    public class SlimeFramework : Mod
    {
        /// <summary>Dictionary of Custom Slimes</summary>
        internal static readonly IDictionary<string, SlimeController> Slimes = new Dictionary<string, SlimeController>();

        internal static bool TryGetSlime(out SlimeController slime)
        {
            slime = Slimes.FirstOrDefault().Value;
            return slime != null;
        }

        public override void Entry(IModHelper helper)
        {
            GreenSlimeExtensions.Init(helper.Reflection);

            // Assets
            var assetController = new AssetController();
            helper.Content.AssetLoaders.Add(assetController);
            helper.Content.AssetEditors.Add(assetController);

            // Events

            // Patches
            new Patcher(this).ApplyAll(
                typeof(GreenSlimePatches)
            );
        }
    }
}