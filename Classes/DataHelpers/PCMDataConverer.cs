using System;
using System.Linq;

namespace Tempora.Classes.DataHelpers
{
    public static class PCMDataConverter
    {
        /// <summary>
        /// Converts a float array (PCM data) to a double array.
        /// </summary>
        public static double[] ConvertToDouble(float[] pcmFloats)
        {
            return pcmFloats.Select(x => (double)x).ToArray();
        }

        /// <summary>
        /// Converts a 16-bit PCM byte array to a double array.
        /// </summary>
        public static double[] ConvertToDouble(byte[] pcmBytes)
        {
            int numSamples = pcmBytes.Length / sizeof(short);
            double[] doubles = new double[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                short sample = BitConverter.ToInt16(pcmBytes, i * sizeof(short));
                doubles[i] = (double)sample / short.MaxValue; // Normalize to range -1.0 to 1.0
            }
            return doubles;
        }

        /// <summary>
        /// Converts a double array back to a float array.
        /// </summary>
        public static float[] ConvertToFloat(double[] pcmDoubles)
        {
            return pcmDoubles.Select(x => (float)x).ToArray();
        }

        /// <summary>
        /// Converts a float array to a 16-bit PCM byte array.
        /// </summary>
        public static byte[] ConvertToPCMBytes(float[] pcmFloats)
        {
            int numSamples = pcmFloats.Length;
            byte[] pcmBytes = new byte[numSamples * sizeof(short)];

            for (int i = 0; i < numSamples; i++)
            {
                short sample = (short)(pcmFloats[i] * short.MaxValue);
                byte[] byteData = BitConverter.GetBytes(sample);
                Buffer.BlockCopy(byteData, 0, pcmBytes, i * sizeof(short), sizeof(short));
            }

            return pcmBytes;
        }
    }
}
