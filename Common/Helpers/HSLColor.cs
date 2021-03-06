using System;
using Microsoft.Xna.Framework;

namespace ImJustMatt.ExpandedStorage.Common.Helpers
{
    internal struct HSLColor
    {
        private const float Tolerance = 1 / 255f;

        // HSL stands for Hue, Saturation and Luminance. HSL
        // color space makes it easier to do calculations
        // that operate on these channels
        // Helpful color math can be found here:
        // https://www.easyrgb.com/en/math.php

        /// <summary>
        /// Hue: the 'color' of the color!
        /// </summary>
        public float H;

        /// <summary>
        /// Saturation: How grey or vivid/colorful a color is
        /// </summary>
        public float S;

        /// <summary>
        /// Luminance: The brightness or lightness of the color
        /// </summary>
        public float L;

        public HSLColor(float h, float s, float l)
        {
            H = h;
            S = s;
            L = l;
        }

        public static HSLColor FromColor(Color color)
        {
            return FromRgb(color.R, color.G, color.B);
        }


        public static HSLColor FromRgb(byte R, byte G, byte B)
        {
            var hsl = new HSLColor();

            hsl.H = 0;
            hsl.S = 0;
            hsl.L = 0;

            var r = R / 255f;
            var g = G / 255f;
            var b = B / 255f;
            var min = Math.Min(Math.Min(r, g), b);
            var max = Math.Max(Math.Max(r, g), b);
            var delta = max - min;

            // luminance is the ave of max and min
            hsl.L = (max + min) / 2f;

            if (delta > 0)
            {
                if (hsl.L < 0.5f)
                {
                    hsl.S = delta / (max + min);
                }
                else
                {
                    hsl.S = delta / (2 - max - min);
                }

                var deltaR = ((max - r) / 6f + delta / 2f) / delta;
                var deltaG = ((max - g) / 6f + delta / 2f) / delta;
                var deltaB = ((max - b) / 6f + delta / 2f) / delta;

                if (Math.Abs(r - max) < Tolerance)
                {
                    hsl.H = deltaB - deltaG;
                }
                else if (Math.Abs(g - max) < Tolerance)
                {
                    hsl.H = 1f / 3f + deltaR - deltaB;
                }
                else if (Math.Abs(b - max) < Tolerance)
                {
                    hsl.H = 2f / 3f + deltaG - deltaR;
                }

                if (hsl.H < 0)
                {
                    hsl.H += 1;
                }

                if (hsl.H > 1)
                {
                    hsl.H -= 1;
                }
            }

            return hsl;
        }

        public HSLColor GetComplement()
        {
            // complementary colors are across the color wheel
            // which is 180 degrees or 50% of the way around the
            // wheel. Add 50% to our hue and wrap large/small values
            var h = H + 0.5f;
            if (h > 1)
            {
                h -= 1;
            }

            return new HSLColor(h, S, L);
        }

        public Color ToRgbColor()
        {
            var c = new Color();

            if (S == 0)
            {
                c.R = (byte) (L * 255f);
                c.G = (byte) (L * 255f);
                c.B = (byte) (L * 255f);
            }
            else
            {
                var v2 = L + S - S * L;
                if (L < 0.5f)
                {
                    v2 = L * (1 + S);
                }

                var v1 = 2f * L - v2;

                c.R = (byte) (255f * HueToRgb(v1, v2, H + 1f / 3f));
                c.G = (byte) (255f * HueToRgb(v1, v2, H));
                c.B = (byte) (255f * HueToRgb(v1, v2, H - 1f / 3f));
            }

            c.A = 255;
            return c;
        }

        private static float HueToRgb(float v1, float v2, float vH)
        {
            vH += vH < 0 ? 1 : 0;
            vH -= vH > 1 ? 1 : 0;
            var ret = v1;

            if (6 * vH < 1)
            {
                ret = v1 + (v2 - v1) * 6 * vH;
            }

            else if (2 * vH < 1)
            {
                ret = v2;
            }

            else if (3 * vH < 2)
            {
                ret = v1 + (v2 - v1) * (2f / 3f - vH) * 6f;
            }

            return MathHelper.Clamp(ret, 0, 1);
        }
    }
}