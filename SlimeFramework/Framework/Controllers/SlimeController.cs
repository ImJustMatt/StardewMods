using ImJustMatt.SlimeFramework.Framework.Models;

namespace ImJustMatt.SlimeFramework.Framework.Controllers
{
    internal class SlimeController : SlimeModel
    {
        public string Data => string.Join("/",
            Health,
            DamageToFarmer,
            MinCoinsToDrop,
            MaxCoinsToDrop,
            false,
            DurationOfRandomMovements,
            "", // ObjectsToDrop
            Resilience,
            Jitteriness,
            MoveTowardPlayer,
            Speed,
            MissChance,
            true,
            ExperienceGained,
            DisplayName
        );
    }
}