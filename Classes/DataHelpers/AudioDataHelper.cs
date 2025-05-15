// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

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
/// <para>"PCM 8": 8-bit PCM. </para>
/// <para>"Separated": Audio data separated by channels as opposed to one long 1D sequence of data</para>
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
        startSilenceSamples = 0; // This will be used if there is no Xing header, as it seems Godot will use the same origin as NAudio in that case.

        //Mp3Frame firstFrame = reader.ReadNextFrame();
        Mp3Frame? xingFrame = reader.XingHeader?.Mp3Frame;
        if (xingFrame != null)
        {
            byte[] frameData = xingFrame.RawData;
            ExtractLameHeaderInfo(frameData, out encoderDelaySamples, out _);
            
            // Logic: Retrieve encoder delay from Xing header. Assume all decorders add 528 samples.
            // Add 1 due to discrepancy, which is mentioned in https://lame.sourceforge.io/tech-FAQ.txt
            startSilenceSamples = encoderDelaySamples + decoderDelaySamples + 1;
        }

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
    /// <param name="floatSample"></param>
    /// <returns></returns>
    public static short ConvertToShort(float floatSample)
    {
        Math.Clamp(floatSample, -1.0f, 1.0f);
        return (short)(floatSample * short.MaxValue);
    }

    public static float ConvertToFloat(short shortSample)
    {
        float floatSample = shortSample / short.MaxValue;
        return Math.Clamp(floatSample, -1.0f, 1.0f);
    }

    public static byte[] ConvertToPCM8(float[] floats)
    {
        int numRawSamples = floats.Length;
        byte[] pcm8 = new byte[numRawSamples * sizeof(short)];
        using (MemoryStream stream = new MemoryStream(pcm8))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            for (int i = 0; i < numRawSamples; i++)
            {
                float sampleFloat = floats[i];
                short sampleShort = ConvertToShort(sampleFloat);
                writer.Write(sampleShort);
            }
        }
        return pcm8;
    }

    public static byte[][] SeparateByChannels(byte[] pcm8, int channels)
    {
        byte[][] pcm8_separated_asbytes = new byte[channels][];

        int numSamplesCombined = pcm8.Length / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        for (int i = 0; i < channels; i++)
        {
            pcm8_separated_asbytes[i] = new byte[pcm8.Length / channels];
        }

        for (int sampleIndex = 0; sampleIndex < numSamplesPerChannel; sampleIndex++)
        {
            int byteIndexForSample = sampleIndex * sizeof(short) * channels;
            for (int channelIndex = 0; channelIndex < channels; channelIndex++)
            {
                for (int relativeByteIndex = 0; relativeByteIndex < sizeof(short); relativeByteIndex++)
                {
                    pcm8_separated_asbytes[channelIndex][sampleIndex * sizeof(short) + relativeByteIndex]
                        = pcm8[byteIndexForSample + sizeof(short) * channelIndex + relativeByteIndex];
                }
            }
        }

        return pcm8_separated_asbytes;
    }

    public static float[][] SeparateByChannels(float[] floatsMixed, int channels)
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
    public static float[][] ConvertToFloats(byte[] pcm8, int channels, out short[][] pcm8_separated, out byte[][] pcm8_separated_asbytes)
    {
        int numSamplesCombined = pcm8.Length / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        // Separate data into separate buffer streams
        float[][] floats_separated = new float[channels][];
        pcm8_separated = new short[channels][];

        for (int i = 0; i < channels; i++)
        {
            floats_separated[i] = new float[numSamplesPerChannel];
            pcm8_separated[i] = new short[numSamplesPerChannel];
        }

        pcm8_separated_asbytes = SeparateByChannels(pcm8, channels);

        // Get shorts and floats for each channel
        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            Buffer.BlockCopy(pcm8_separated_asbytes[channelIndex], 0, pcm8_separated[channelIndex], 0, pcm8_separated_asbytes[channelIndex].Length);
            for (int sampleIndex = 0; (sampleIndex < numSamplesPerChannel); sampleIndex++)
            {
                floats_separated[channelIndex][sampleIndex] = (float)pcm8_separated[channelIndex][sampleIndex] / short.MaxValue;
            }
        }

        return floats_separated;
    }

    public static Vector2[] ConvertToStereoVector2Floats(byte[] pcm8)
    {
        float[][] floats = ConvertToFloats(pcm8, 2, out _, out _);
        Vector2[] stereoFloats = new Vector2[floats[0].Length];
        for (int sampleIndex = 0; sampleIndex < floats[0].Length; sampleIndex++)
        {
            stereoFloats[sampleIndex] = new Vector2(floats[0][sampleIndex], floats[1][sampleIndex]);
        }
        return stereoFloats;
    }
    #endregion
}