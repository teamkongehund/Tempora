using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Spectrogram;

namespace Tempora.Classes.Visual.AudioDisplay
{
    /// <summary>
    /// Custom <see cref="Spectrogram.Colormap"/> generation class. Allows generating spectrograms with Tempora themed colors.
    /// </summary>
    class CustomColormap : IColormap
    {
        private readonly int[] rgb;

        public CustomColormap(List<int> colors)
        {
            if (colors == null || colors.Count < 2)
                throw new ArgumentException("At least two colors are required to create a colormap.");

            rgb = GenerateGradient(colors);
        }

        public CustomColormap(List<Color> colors)
        {
            if (colors == null || colors.Count < 2)
                throw new ArgumentException("At least two colors are required to create a colormap.");

            rgb = GenerateGradient(colors.Select(ToRgbInt).ToList());
        }

        static private int ToRgbInt(Godot.Color color)
        {
            return (color.R8 << 16) | (color.G8 << 8) | color.B8; // Converts to 0xRRGGBB format
        }

        public (byte r, byte g, byte b) GetRGB(byte value)
        {
            byte[] bytes = BitConverter.GetBytes(rgb[value]);
            return (bytes[2], bytes[1], bytes[0]);
        }

        private static int[] GenerateGradient(List<int> colors)
        {
            int[] gradient = new int[256];
            int segments = colors.Count - 1;
            int stepsPerSegment = 256 / segments;

            for (int s = 0; s < segments; s++)
            {
                int startColor = colors[s];
                int endColor = colors[s + 1];

                byte sr = (byte)((startColor >> 16) & 0xFF);
                byte sg = (byte)((startColor >> 8) & 0xFF);
                byte sb = (byte)(startColor & 0xFF);

                byte er = (byte)((endColor >> 16) & 0xFF);
                byte eg = (byte)((endColor >> 8) & 0xFF);
                byte eb = (byte)(endColor & 0xFF);

                for (int i = 0; i < stepsPerSegment; i++)
                {
                    float t = (float)i / (stepsPerSegment - 1);
                    byte r = (byte)(sr + t * (er - sr));
                    byte g = (byte)(sg + t * (eg - sg));
                    byte b = (byte)(sb + t * (eb - sb));
                    gradient[s * stepsPerSegment + i] = (r << 16) | (g << 8) | b;
                }
            }

            // Fill any remaining values in case of rounding issues
            for (int i = segments * stepsPerSegment; i < 256; i++)
            {
                gradient[i] = colors[^1];
            }

            return gradient;
        }
    }
}
