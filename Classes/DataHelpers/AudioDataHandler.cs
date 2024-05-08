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

namespace Tempora.Classes.Utility;

public partial class AudioDataHandler : Node
{
    #region Read audio
    private static void ExtractMp3(byte[] mp3File, out byte[] shortsRaw_byte, out int channels, out int sampleRate, out int numRawBytes)
    {
        using var mp3FileMemoryStream = new MemoryStream(mp3File);
        using var reader = new Mp3FileReader(mp3FileMemoryStream);

        shortsRaw_byte = new byte[reader.Length];
        sampleRate = reader.Mp3WaveFormat.SampleRate;
        channels = reader.Mp3WaveFormat.Channels;
        numRawBytes = reader.Read(shortsRaw_byte, 0, shortsRaw_byte.Length * channels);
    }

    private static void ExtractMp3(string mp3Path, out byte[] shortsRaw_byte, out int channels, out int sampleRate, out int numRawBytes)
    {
        using var reader = new Mp3FileReader(mp3Path);

        shortsRaw_byte = new byte[reader.Length];
        sampleRate = reader.Mp3WaveFormat.SampleRate;
        channels = reader.Mp3WaveFormat.Channels;
        numRawBytes = reader.Read(shortsRaw_byte, 0, shortsRaw_byte.Length * channels);
    }

    private static void ExtractOgg(string oggPath, out float[] floatsRaw, out int channels, out int sampleRate, out int numSamplesCombined)
    {
        int samplesPerChannel;
        int numFloatBytesTotal;

        using (VorbisWaveReader reader = new VorbisWaveReader(oggPath))
        {
            channels = reader.WaveFormat.Channels;
            sampleRate = reader.WaveFormat.SampleRate;

            numFloatBytesTotal = (int)reader.Length;
            samplesPerChannel = numFloatBytesTotal / (channels * sizeof(float));
            floatsRaw = new float[samplesPerChannel * channels];

            // Convert Ogg Vorbis to raw 32-bit float n channels PCM.
            numSamplesCombined = reader.Read(floatsRaw, 0, samplesPerChannel * channels);
        }
    } 
    #endregion
    #region Array and sample manipulation
    /// <summary>
    /// Convert a 32-bit float audio sample (between -1 and 1) into a short value (16-bit signed integer)
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    private static short FloatSampleToShort(float f)
    {
        Math.Clamp(f, -1.0f, 1.0f);
        return (short)(f * short.MaxValue);
    }
    private static void FloatsToShortByteArray(float[] floats, out byte[] shorts_byte)
    {
        int numRawSamples = floats.Length;
        shorts_byte = new byte[numRawSamples * sizeof(short)];
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
    }

    private static byte[][] SeparateShortByteArrayIntoChannels(byte[] shortsRaw_byte, int channels)
    {
        byte[][] shorts_byte = new byte[channels][];

        int numSamplesCombined = shortsRaw_byte.Length / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        for (int i = 0; i < channels; i++)
        {
            shorts_byte[i] = new byte[shortsRaw_byte.Length / channels];
        }

        for (int sampleIndex = 0; sampleIndex < numSamplesPerChannel; sampleIndex++)
        {
            int byteIndexForSample = sampleIndex * sizeof(short) * channels;
            for (int channelIndex = 0; channelIndex < channels; channelIndex++)
            {
                for (int relativeByteIndex = 0; relativeByteIndex < sizeof(short); relativeByteIndex++)
                {
                    shorts_byte[channelIndex][sampleIndex * sizeof(short) + relativeByteIndex]
                        = shortsRaw_byte[byteIndexForSample + sizeof(short) * channelIndex + relativeByteIndex];
                }
            }
        }

        return shorts_byte;
    }

    private static float[][] SeparateFloatsIntoChannels(float[] floatsRaw, int channels)
    {
        float[][] floats = new float[channels][];

        int samplesPerChannel = floatsRaw.Length / channels;

        for (int i = 0; i < channels; i++)
        {
            floats[i] = new float[samplesPerChannel];
        }

        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            for (int sampleIndex = 0; sampleIndex < samplesPerChannel; sampleIndex++)
            {
                floats[channelIndex][sampleIndex] = floatsRaw[sampleIndex * channels + channelIndex];
            }
        }

        return floats;
    } 
    #endregion

    private static SoundData Mp3ToSoundData(string audioFilePath)
    {
        ExtractMp3(audioFilePath, out byte[] shortsRaw_byte, out int channels, out int sampleRate, out int numRawBytes);
        return (ExtractedMp3ToSoundData(shortsRaw_byte, channels, sampleRate, numRawBytes));
    }

    private static SoundData Mp3ToSoundData(byte[] mp3File)
    {
        ExtractMp3(mp3File, out byte[] shortsRaw_byte, out int channels, out int sampleRate, out int numRawBytes);
        return (ExtractedMp3ToSoundData(shortsRaw_byte, channels, sampleRate, numRawBytes));
    }

    private static SoundData ExtractedMp3ToSoundData(byte[] shortsRaw_byte, int channels, int sampleRate, int numRawBytes)
    {
        int numSamplesCombined = numRawBytes / sizeof(short);
        int numSamplesPerChannel = numSamplesCombined / channels;

        // Separate data into separate buffer streams
        float[][] floats = new float[channels][];
        short[][] shorts = new short[channels][];

        for (int i = 0; i < channels; i++)
        {
            floats[i] = new float[numSamplesPerChannel];
            shorts[i] = new short[numSamplesPerChannel];
        }

        byte[][] shorts_byte = SeparateShortByteArrayIntoChannels(shortsRaw_byte, channels);

        // Get shorts and floats for each channel
        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            Buffer.BlockCopy(shorts_byte[channelIndex], 0, shorts[channelIndex], 0, shorts_byte[channelIndex].Length);
            for (int sampleIndex = 0; (sampleIndex < numSamplesPerChannel); sampleIndex++)
            {
                floats[channelIndex][sampleIndex] = (float)shorts[channelIndex][sampleIndex] / short.MaxValue;
            }
        }

        return new SoundData(sampleRate, sizeof(short) * 8, shorts_byte, floats);
    }


    /// <summary>
    /// Returns a <see cref="SoundData"/> based on a file path. 
    /// </summary>
    /// <param name="audioFilePath"></param>
    /// <returns></returns>
    private static SoundData OggToSoundData(string audioFilePath)
    {
        // Based on https://github.com/leezer3/OpenBVE/blob/84064b7ef4e51def0b26e07226c114c004bcd4d3/source/Plugins/Sound.Vorbis/Plugin.Parser.cs#L12

        ExtractOgg(audioFilePath, out float[] floatsRaw, out int channels, out int sampleRate, out int numSamplesCombined);

        // Convert PCM bit depth from 32-bit float to 16-bit integer.
        FloatsToShortByteArray(floatsRaw, out byte[] shortsRaw_byte);

        float[][] floats = SeparateFloatsIntoChannels(floatsRaw, channels);
        byte[][] shorts_byte = SeparateShortByteArrayIntoChannels(shortsRaw_byte, channels);

        return new SoundData(sampleRate, sizeof(short) * 8, shorts_byte, floats);
    }

    public static SoundData GetSoundData(string audioFilePath)
    {
        string extension = Path.GetExtension(audioFilePath).ToLower();

        switch (extension)
        {
            case ".ogg":
                return OggToSoundData(audioFilePath);
            case ".mp3":
                return Mp3ToSoundData(audioFilePath);
            //case ".wav":
            //    return WavToSoundData(audioFilePath);
            default:
                throw new NotSupportedException($"Audio format '{extension}' is not supported.");
        }
    }

    public static SoundData GetSoundData(byte[] fileBuffer, string extension)
    {
        switch (extension)
        {
            case ".ogg":
                throw new NotSupportedException("AudioDataHandler does not yet support retrieving SoundData by parsing an OGG buffer.");
            case ".mp3":
                return Mp3ToSoundData(fileBuffer);
            default:
                throw new NotSupportedException($"Audio format '{extension}' is not supported.");
        }
    }
}