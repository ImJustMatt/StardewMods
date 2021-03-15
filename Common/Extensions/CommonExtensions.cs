using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace ImJustMatt.Common.Extensions
{
    internal static class CommonExtensions
    {
        public static void InvokeAll(this EventHandler eventHandler, object caller)
        {
            foreach (var @delegate in eventHandler.GetInvocationList()) @delegate.DynamicInvoke(caller, null);
        }

        public static SButton? GetSingle(this KeybindList keyBindList)
        {
            return keyBindList.Keybinds.SingleOrDefault(k => k.IsBound)?.Buttons.First();
        }

        public static int RoundUp(this int i, int d = 1)
        {
            return (int) (d * Math.Ceiling((float) i / d));
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        private static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source,
            Random rng)
        {
            var buffer = source.ToList();
            for (var i = 0; i < buffer.Count; i++)
            {
                var j = rng.Next(i, buffer.Count);
                yield return buffer[j];
                buffer[j] = buffer[i];
            }
        }
    }
}