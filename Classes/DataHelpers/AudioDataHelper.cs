using System;
using System.IO;
using Godot;
using NAudio.Wave;
using NAudio.Vorbis;
using System.Runtime.CompilerServices;
using System.Linq;
using Tempora.Classes.Audio;
using NVorbis;
using System.Threading.Channels;
using System.Reflection.PortableExecutable;

namespace Tempora.Classes.DataHelpers;

/// <summary>
/// Helper class to handle and transform audio data. 
/// <para>Explanation and terminology:</para>
/// <para>MP3: This codec stores audio as 16-bit integer. It must be converted to floats to be useful in Tempora. 
/// The MP3 codec usually has silence added to the beginning and end of the file. 
/// See the overloads for <see cref="DecodeMp3"/> for how the exact amount of silence is determined.</para>
/// <para>OGG: This codec stores audio as 32-bit float, which is what Tempora already works with</para>
/// <para>"PCM": Pulse-code modulation. Any format to store audio where the audio is split up into evenly-spaced samples at i.e. 41000 samples/second.
/// The number for each sample is the amplitude of the sample. For floats, the range is -1 to 1.</para>
/// <para>"Mixed" and "Raw": Words mainly used to describe arrays where the audio channels (left and right) are still mixed together and must be separated</para>
/// <para>"floats[][]": Variable that stores the PCM data as floats for each channel.</para>
/// </summary>
public partial class AudioDataHelper
{
    #region Decode audio files
    /// <summary>
    /// See <see href="http://gabriel.mp3-tech.org/mp3infotag.html"/> for documentation on how the code works.
    /// </summary>
    /// <param name="xingHeaderData"></param>
    public static void ExtractLameHeaderInfo(byte[] xingHeaderData, out int encoderDelaySamples, out int endPaddingSamples)
    {
        encoderDelaySamples = 576; // Standard for MP3 files 
        endPaddingSamples = 0;

        if (xingHeaderData.Length < 0xB1)
        {
            GD.Print("LAME header not long enough or missing");
            return;
        }

        // Encoder Delay at start of file in number of samples
        encoderDelaySamples = (xingHeaderData[0xB1] << 4) | (xingHeaderData[0xB2] >> 4);

        // Padding added to end of file in number of samples
        endPaddingSamples = ((xingHeaderData[0xB2] & 0b00001111) << 8) | xingHeaderData[0xB3];
    }

    /// <summary>
    /// Read an MP3 file and output the raw contents. MP3 data is 16-bit integer.
    /// </summary>
    public static void DecodeMp3(byte[] mp3File, out byte[] shortsMixed_byte, out int channels, 
        out int sampleRate, out int numRawBytes, out int startSilenceSamples)
    {
        using var mp3FileMemoryStream = new MemoryStream(mp3File);
        using var reader = new Mp3FileReader(mp3FileMemoryStream);
        DecodeMp3(reader, out shortsMixed_byte, out channels, out sampleRate, out numRawBytes, out startSilenceSamples);
    }

    /// <summary>
    /// Read an MP3 file and output the raw contents. MP3 data is 16-bit integer.
    /// </summary>
    public static void DecodeMp3(string mp3Path, out byte[] shortsMixed_byte, out int channels, 
        out int sampleRate, out int numRawBytes, out int startSilenceSamples)
    {
        using var reader = new Mp3FileReader(mp3Path);
        DecodeMp3(reader, out shortsMixed_byte, out channels, out sampleRate, out numRawBytes, out startSilenceSamples);
    }

    public static void DecodeMp3(Mp3FileReader reader, out byte[] shortsMixed_byte, out int channels, 
        out int sampleRate, out int numRawBytes, out int startSilenceSamples)
    {
        sampleRate = reader.Mp3WaveFormat.SampleRate;
        channels = reader.Mp3WaveFormat.Channels;

        /// Number of samples prepended to beginning of audio.
        int encoderDelaySamples = 576;
        int decoderDelaySamples = 528;

        //Mp3Frame firstFrame = reader.ReadNextFrame();
        Mp3Frame xingFrame = reader.XingHeader.Mp3Frame;
        if (xingFrame != null)
        {
            byte[] frameData = xingFrame.RawData;
            ExtractLameHeaderInfo(frameData, out encoderDelaySamples, out _);
        }

        // Logic: Retrieve encoder delay from Xing header. Assume all decorders add 528 samples.
        // Add 1 due to discrepancy, which is mentioned in https://lame.sourceforge.io/tech-FAQ.txt
        startSilenceSamples = encoderDelaySamples + decoderDelaySamples + 1;

        shortsMixed_byte = new byte[reader.Length];
        numRawBytes = reader.Read(shortsMixed_byte, 0, shortsMixed_byte.Length * channels);
    }

    /// <summary>
    /// Read an OGG file and output the raw contents. OGG data is 32-bit float.
    /// </summary>
    public static void DecodeOgg(string oggPath, out float[] floatsMixed, out int channels, out int sampleRate, out int numSamplesCombined)
    {

        using VorbisWaveReader reader = new VorbisWaveReader(oggPath);
        sampleRate = reader.WaveFormat.SampleRate;
        channels = reader.WaveFormat.Channels;

        int samplesPerChannel;
        int numMixedFloatBytes;
        numMixedFloatBytes = (int)reader.Length;
        samplesPerChannel = numMixedFloatBytes / (channels * sizeof(float));
        floatsMixed = new float[samplesPerChannel * channels];

        // Convert Ogg Vorbis to raw 32-bit float n channels PCM.
        numSamplesCombined = reader.Read(floatsMixed, 0, samplesPerChannel * channels);
        
    } 
    #endregion
    #region Array and sample manipulation
    /// <summary>
    /// Convert a 32-bit float audio sample (between -1 and 1) into a short value (16-bit signed integer)
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static short FloatSampleToShort(float f)
    {
        Math.Clamp(f, -1.0f, 1.0f);
        return (short)(f * short.MaxValue);
    }
    public static byte[] FloatsToShortByteArray(float[] floats)
    {
        int numRawSamples = floats.Length;
        byte[] shorts_byte = new byte[numRawSamples * sizeof(short)];
        using (MemoryStream stream = new MemoryStream(shorts_byte))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            for (int i = 0; i < numRawSamples; i++)
            {
                float sampleFloat = floats[i];
                short sampleShort = FloatSampleToShort(sampleFloat);
                writer.Write(sampleShort);
            }
        }
        return shorts_byte;
    }

    public static byte[][] SeparateMixedShortBytesIntoChannels(byte[] shortsMixed_byte, int channels)
    {
        byte[][] shorts_byte = new byte[channels][];

        int numSamplesCombined = shortsMixed_byte.Length / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        for (int i = 0; i < channels; i++)
        {
            shorts_byte[i] = new byte[shortsMixed_byte.Length / channels];
        }

        for (int sampleIndex = 0; sampleIndex < numSamplesPerChannel; sampleIndex++)
        {
            int byteIndexForSample = sampleIndex * sizeof(short) * channels;
            for (int channelIndex = 0; channelIndex < channels; channelIndex++)
            {
                for (int relativeByteIndex = 0; relativeByteIndex < sizeof(short); relativeByteIndex++)
                {
                    shorts_byte[channelIndex][sampleIndex * sizeof(short) + relativeByteIndex]
                        = shortsMixed_byte[byteIndexForSample + sizeof(short) * channelIndex + relativeByteIndex];
                }
            }
        }

        return shorts_byte;
    }

    public static float[][] SeparateMixedFloatsIntoChannels(float[] floatsMixed, int channels)
    {
        float[][] floats = new float[channels][];

        int samplesPerChannel = floatsMixed.Length / channels;

        for (int i = 0; i < channels; i++)
        {
            floats[i] = new float[samplesPerChannel];
        }

        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            for (int sampleIndex = 0; sampleIndex < samplesPerChannel; sampleIndex++)
            {
                floats[channelIndex][sampleIndex] = floatsMixed[sampleIndex * channels + channelIndex];
            }
        }

        return floats;
    }
    public static float[][] ShortsByteToFloats(byte[] shortsMixed_byte, int channels, out short[][] shorts, out byte[][] shorts_byte)
    {
        int numSamplesCombined = shortsMixed_byte.Length / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        // Separate data into separate buffer streams
        float[][] floats = new float[channels][];
        shorts = new short[channels][];

        for (int i = 0; i < channels; i++)
        {
            floats[i] = new float[numSamplesPerChannel];
            shorts[i] = new short[numSamplesPerChannel];
        }

        shorts_byte = SeparateMixedShortBytesIntoChannels(shortsMixed_byte, channels);

        // Get shorts and floats for each channel
        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            Buffer.BlockCopy(shorts_byte[channelIndex], 0, shorts[channelIndex], 0, shorts_byte[channelIndex].Length);
            for (int sampleIndex = 0; (sampleIndex < numSamplesPerChannel); sampleIndex++)
            {
                floats[channelIndex][sampleIndex] = (float)shorts[channelIndex][sampleIndex] / short.MaxValue;
            }
        }

        return floats;
    }
    #endregion
}