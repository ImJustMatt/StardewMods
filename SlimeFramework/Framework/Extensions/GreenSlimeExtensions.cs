using ImJustMatt.SlimeFramework.Framework.Controllers;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace ImJustMatt.SlimeFramework.Framework.Extensions
{
    internal static class GreenSlimeExtensions
    {
        private static IReflectionHelper _reflection;

        public static void Init(IReflectionHelper reflection)
        {
            _reflection = reflection;
        }

        public static void MakeCustomSlime(this GreenSlime slime, SlimeController slimeController)
        {
            slime.Name = "";
            slime.reloadSprite();
            slime.Sprite.SpriteHeight = 24;
            slime.Sprite.UpdateSourceRect();
            _reflection.GetMethod(slime, "parseMonsterInfo").Invoke(slime.Name);
            slime.color.Value = Color.White;
        }
    }
}