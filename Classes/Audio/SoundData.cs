using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Based on https://github.com/leezer3/OpenBVE/blob/84064b7ef4e51def0b26e07226c114c004bcd4d3/source/OpenBveApi/Sound.cs

namespace Tempora.Classes.Audio;
/// <summary>Holds PCM audio data regardless of audio format.</summary>
public class SoundData
{
    // --- fields ---
    private int sampleRate;
    private int bitsPerSample;
    private byte[][] pcmBytes;
    private float[][] pcmFloats;
    // --- constructors ---
    /// <summary>Creates a new instance of this class.</summary>
    /// <param name="sampleRate">The number of samples per second.</param>
    /// <param name="bitsPerSample">The number of bits per sample. Allowed values are 8 or 16.</param>
    /// <param name="bytes">The PCM sound data per channel. For 8 bits per sample, samples are unsigned from 0 to 255. For 16 bits per sample, samples are signed from -32768 to 32767 and in little endian byte order.</param>
    /// <exception cref="System.ArgumentNullException">Raised when the bytes array or any of its subarrays is a null reference.</exception>
    /// <exception cref="System.ArgumentException">Raised when the bytes' subarrays are of unequal length.</exception>
    /// <exception cref="System.ArgumentException">Raised when the number of bits per samples is neither 8 nor 16.</exception>
    public SoundData(int sampleRate, int bitsPerSample, byte[][] bytes, float[][] floats)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("The data bytes are a null reference.");
        }
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == null)
            {
                throw new ArgumentNullException("The data bytes of a particular channel is a null reference.");
            }
        }
        for (int i = 1; i < bytes.Length; i++)
        {
            if (bytes[i].Length != bytes[0].Length)
            {
                throw new ArgumentException("The data bytes of the channels are of unequal length.");
            }
        }
        if (bitsPerSample != 8 & bitsPerSample != 16)
        {
            throw new ArgumentException("The number of bits per sample is neither 8 nor 16.");
        }
        else
        {
            this.sampleRate = sampleRate;
            this.bitsPerSample = bitsPerSample;
            pcmBytes = bytes;
            pcmFloats = floats;
        }
    }
    // --- properties ---
    /// <summary>Gets the number of samples per second.</summary>
    public int SampleRate => this.sampleRate;
    /// <summary>Gets the number of bits per sample. Allowed values are 8 or 16.</summary>
    public int BitsPerSample => this.bitsPerSample;
    /// <summary>Gets the PCM sound data per channel. For 8 bits per sample, samples are unsigned from 0 to 255. For 16 bits per sample, samples are signed from -32768 to 32767 and in little endian byte order.</summary>
    public byte[][] Bytes => pcmBytes;
    /// <summary>The PCM sound data per channel. Samples are floats between -1 and 1.</summary>
    public float[][] Floats => pcmFloats;
    /// <summary>
    /// Number of channels
    /// </summary>
    public int Channels => pcmBytes.Length;
}