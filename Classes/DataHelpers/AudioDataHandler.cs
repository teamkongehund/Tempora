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
    public static float[] Mp3ToAudioFloat(byte[] mp3File, out int sampleRate, out int channels) => AudioSamplesToFloat(Mp3ToAudioSamples(mp3File, out sampleRate, out channels));


    //// General method to extract audio data from any supported format
    //public static float[] ExtractAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    //{
    //    string extension = Path.GetExtension(audioFilePath).ToLower();

    //    switch (extension)
    //    {
    //        case ".wav":
    //            return WavToAudioFloat(audioFilePath, out sampleRate, out channels);
    //        case ".mp3":
    //            return Mp3ToAudioFloat(audioFilePath, out sampleRate, out channels);
    //        case ".ogg":
    //            return OggToAudioFloat(audioFilePath, out sampleRate, out channels);
    //        default:
    //            throw new NotSupportedException($"Audio format '{extension}' is not supported.");
    //    }
    //}

    #region old approach (short)
    //public static short[] Mp3ToAudioSamples(byte[] mp3File, out int sampleRate, out int channels)
    //{
    //    byte[] audioBuffer;
    //    int bitsPerSample;
    //    using (var audioFileMemoryStream = new MemoryStream(mp3File))
    //    {
    //        using var reader = new Mp3FileReader(audioFileMemoryStream);
    //        audioBuffer = new byte[reader.Length];
    //        reader.Read(audioBuffer, 0, audioBuffer.Length);
    //        sampleRate = reader.Mp3WaveFormat.SampleRate;
    //        channels = reader.Mp3WaveFormat.Channels;
    //        bitsPerSample = reader.WaveFormat.BitsPerSample;
    //    }

    //    return AudioBytesToSamples(audioBuffer);
    //}
    //// Convert audio bytes to samples
    //private static short[] AudioBytesToSamples(byte[] audioBytes, int bitsPerSample = 16)
    //{
    //    int bytesPerSample = bitsPerSample / 8;
    //    int numSamples = audioBytes.Length / bytesPerSample;
    //    short[] audioSamplesShort = new short[audioBytes.Length / numSamples];
    //    Buffer.BlockCopy(audioBytes, 0, audioSamplesShort, 0, audioBytes.Length);
    //    return audioSamplesShort;
    //}

    //// Convert audio samples to float array
    //private static float[] AudioSamplesToFloat(short[] audioSamplesShort)
    //{
    //    float[] audioSamplesFloat = new float[audioSamplesShort.Length];
    //    for (int i = 0; i < audioSamplesShort.Length; i++)
    //        audioSamplesFloat[i] = audioSamplesShort[i] / (float)short.MaxValue;
    //    return audioSamplesFloat;
    //}

    //// Extract audio data from a .wav file and convert it into a float array
    //private static float[] WavToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    //{
    //    byte[] audioBytes;
    //    int bitsPerSample;
    //    using (var reader = new WaveFileReader(audioFilePath))
    //    {
    //        sampleRate = reader.WaveFormat.SampleRate;
    //        channels = reader.WaveFormat.Channels;
    //        bitsPerSample = reader.WaveFormat.BitsPerSample;
    //        audioBytes = new byte[reader.Length];
    //        reader.Read(audioBytes, 0, audioBytes.Length);
    //    }

    //    return AudioSamplesToFloat(AudioBytesToSamples(audioBytes));
    //}

    //// Extract audio data from an .mp3 file and convert it into a float array
    //private static float[] Mp3ToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    //{
    //    byte[] audioBytes;
    //    int bitsPerSample;
    //    using (var reader = new Mp3FileReader(audioFilePath))
    //    {
    //        sampleRate = reader.Mp3WaveFormat.SampleRate;
    //        channels = reader.Mp3WaveFormat.Channels;
    //        bitsPerSample = reader.Mp3WaveFormat.BitsPerSample;
    //        using (var ms = new MemoryStream())
    //        {
    //            reader.CopyTo(ms);
    //            audioBytes = ms.ToArray();
    //        }
    //    }

    //    return AudioSamplesToFloat(AudioBytesToSamples(audioBytes));
    //}

    //// Extract audio data from an .ogg file and convert it into a float array
    //private static float[] OggToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    //{
    //    byte[] audioBytes;
    //    int bitsPerSample;
    //    using (var reader = new VorbisWaveReader(audioFilePath))
    //    {
    //        sampleRate = reader.WaveFormat.SampleRate;
    //        channels = reader.WaveFormat.Channels;
    //        bitsPerSample = reader.WaveFormat.BitsPerSample;
    //        using (var ms = new MemoryStream())
    //        {
    //            reader.CopyTo(ms);
    //            audioBytes = ms.ToArray();
    //        }
    //    }

    //    return AudioSamplesToFloat(AudioBytesToSamples(audioBytes, bitsPerSample));
    //} 
    #endregion

    #region new approach (int)
    public static int[] Mp3ToAudioSamples(byte[] mp3File, out int sampleRate, out int channels)
    {
        byte[] audioBuffer;
        int bitsPerSample;
        using (var audioFileMemoryStream = new MemoryStream(mp3File))
        {
            using var reader = new Mp3FileReader(audioFileMemoryStream);
            audioBuffer = new byte[reader.Length];
            reader.Read(audioBuffer, 0, audioBuffer.Length);
            sampleRate = reader.Mp3WaveFormat.SampleRate;
            channels = reader.Mp3WaveFormat.Channels;
            bitsPerSample = reader.WaveFormat.BitsPerSample;
        }

        return AudioBytesToSamples(audioBuffer);
    }

    private static int[] AudioBytesToSamples(byte[] audioBytes, int bitsPerSample = 16)
    {
        if (bitsPerSample == 0)
            GD.Print($"AudioDataHandler.AudioBytesToSamples: Warning: interpreting 0 bits per sample as 16.");
        bitsPerSample = (bitsPerSample == 0) ? 16 : bitsPerSample;

        // debug
        //bitsPerSample = 32;

        int bytesPerSample = bitsPerSample / 8;
        int numSamples = audioBytes.Length / bytesPerSample;
        int[] audioSamplesInt = new int[numSamples];

        // Iterate through the byte array and convert bytes to integers
        for (int i = 0; i < numSamples; i++)
        {
            // Combine bytes into an integer based on little-endian encoding
            int value = 0;

            // Adjust the sign if the most significant bit is set
            // the range 0b_0000_0000 to 0b_0111_1111 represent negative samples.
            // the range 0b_1000_0000 ro 0b_1111_1111 represent positive samples.
            // 0x80 is equal to 0b_1000_0000
            // the bitwise & operation will turn every number below 0b_1000_0000 into 0.
            bool isNegative = (audioBytes[i * bytesPerSample + bytesPerSample - 1] & 0x80) != 0;

            // If the number is negative, set all higher bits to 1
            if (isNegative)
            {
                for (int j = bytesPerSample; j < 4; j++)
                {
                    value |= 0xFF << (8 * j);
                }
            }

            // Combine bytes into an integer
            for (int j = bytesPerSample - 1; j >= 0; j--)
            {
                value |= audioBytes[i * bytesPerSample + j] << (8 * j);
            }

            audioSamplesInt[i] = value;
        }

        return audioSamplesInt;
    }

    // Convert audio samples to float array
    private static float[] AudioSamplesToFloat(int[] audioSamplesInt)
    {
        float[] audioSamplesFloat = new float[audioSamplesInt.Length];
        float maxValue = (float)audioSamplesInt.Max(); // TODO: Investigate why we have to do this.
        //float maxValue = int.MaxValue;
        for (int i = 0; i < audioSamplesInt.Length; i++)
            audioSamplesFloat[i] = audioSamplesInt[i] / maxValue;
        return audioSamplesFloat;
    }

    private static float[] AudioBytesToFloats(byte[] audioBytes)
    {
        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes));
    }

    // Extract audio data from a .wav file and convert it into a float array
    private static float[] WavToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    {
        byte[] audioBytes;
        int bitsPerSample;
        using (var reader = new WaveFileReader(audioFilePath))
        {
            sampleRate = reader.WaveFormat.SampleRate;
            channels = reader.WaveFormat.Channels;
            bitsPerSample = reader.WaveFormat.BitsPerSample;
            audioBytes = new byte[reader.Length];
            reader.Read(audioBytes, 0, audioBytes.Length);
        }

        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes, bitsPerSample));
    }

    // Extract audio data from an .mp3 file and convert it into a float array
    private static float[] Mp3ToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    {
        byte[] audioBytes;
        int bitsPerSample;
        using (var reader = new Mp3FileReader(audioFilePath))
        {
            sampleRate = reader.Mp3WaveFormat.SampleRate;
            channels = reader.Mp3WaveFormat.Channels;
            bitsPerSample = reader.Mp3WaveFormat.BitsPerSample;
            using (var ms = new MemoryStream())
            {
                reader.CopyTo(ms);
                audioBytes = ms.ToArray();
            }
        }

        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes, bitsPerSample));
    }

    // Extract audio data from an .ogg file and convert it into a float array
    private static float[] OggToAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    {
        byte[] audioBytes;
        int bitsPerSample;
        using (var reader = new VorbisWaveReader(audioFilePath))
        {
            sampleRate = reader.WaveFormat.SampleRate;
            channels = reader.WaveFormat.Channels;
            bitsPerSample = reader.WaveFormat.BitsPerSample;
            using (var ms = new MemoryStream())
            {
                reader.CopyTo(ms);
                audioBytes = ms.ToArray();
            }
        }

        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes, bitsPerSample));
    }
    #endregion

    #region new new approach (SoundData)
    private static SoundData Mp3ToSoundData(string audioFilePath)
    {
        byte[] dataBytes;
        int bitsPerSample;
        int channels;
        int sampleRate;
        int numSamplesTotal;

        using (var reader = new Mp3FileReader(audioFilePath))
        {
            dataBytes = new byte[reader.Length];
            sampleRate = reader.Mp3WaveFormat.SampleRate;
            channels = reader.Mp3WaveFormat.Channels;
            bitsPerSample = reader.Mp3WaveFormat.BitsPerSample;
            numSamplesTotal = reader.Read(dataBytes, 0, dataBytes.Length * channels);
        }

        //float[] dataFloats = new float[dataBytes.Length / sizeof(float)];

        // Separate data into separate buffer streams
        byte[][] buffers = new byte[channels][];
        float[][] floats = new float[channels][];
        short[][] shorts = new short[channels][];

        for (int i = 0; i < channels; i++)
        {
            buffers[i] = new byte[dataBytes.Length / channels];
            floats[i] = new float[numSamplesTotal / channels];
            shorts[i] = new short[numSamplesTotal / channels];
        }

        // Get buffer for each channel
        for (int sampleIndex = 0; sampleIndex < numSamplesTotal / channels; sampleIndex++)
        {
            for (int channelIndex = 0; channelIndex < channels; channelIndex++)
            {
                buffers[channelIndex][sampleIndex] = dataBytes[sampleIndex + channelIndex];
            }
        }

        // Get shorts and floats for each channel
        for (int channelIndex = 0; channelIndex < channels; channelIndex++)
        {
            Buffer.BlockCopy(buffers[channelIndex], 0, shorts[channelIndex], 0, buffers[channelIndex].Length);
            for (int sampleIndex = 0; (sampleIndex < numSamplesTotal / channels); sampleIndex++)
            {
                floats[channelIndex][sampleIndex] = (float)shorts[channelIndex][sampleIndex]/short.MaxValue;
            }
        }

        return new SoundData(sampleRate, sizeof(short) * 8, buffers, floats);
    }

    /// <summary>
    /// Returns a <see cref="SoundData"/> based on a file path. 
    /// </summary>
    /// <param name="audioFilePath"></param>
    /// <returns></returns>
    private static SoundData OggToSoundData(string audioFilePath)
    {
        // Based on https://github.com/leezer3/OpenBVE/blob/84064b7ef4e51def0b26e07226c114c004bcd4d3/source/Plugins/Sound.Vorbis/Plugin.Parser.cs#L12
        using (VorbisWaveReader reader = new VorbisWaveReader(audioFilePath))
        {
            int channels = reader.WaveFormat.Channels;
            int samplesPerChannel = (int)reader.Length / (channels * sizeof(float));
            float[] dataFloats = new float[samplesPerChannel * channels];

            // Convert Ogg Vorbis to raw 32-bit float n channels PCM.
            int numSamplesTotal = reader.Read(dataFloats, 0, samplesPerChannel * channels);

            // Separate float samples into separate arrays
            float[][] floats = new float[channels][];
            byte[] dataBytes = new byte[numSamplesTotal * sizeof(short)];
            byte[][] buffers = new byte[channels][];

            for (int i = 0; i < channels; i++)
            {
                buffers[i] = new byte[dataBytes.Length / channels];
                floats[i] = new float[samplesPerChannel]; // if OGG stops working, change this back to dataBytes.Length
            }

            for (int sampleIndex = 0; sampleIndex < samplesPerChannel; sampleIndex++)
            {
                for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    floats[channelIndex][sampleIndex] = dataFloats[sampleIndex + channelIndex];
                }
            }

            // Convert PCM bit depth from 32-bit float to 16-bit integer.
            using (MemoryStream stream = new MemoryStream(dataBytes))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < numSamplesTotal; i++)
                {
                    float sample = dataFloats[i];

                    if (sample < -1.0f)
                    {
                        sample = -1.0f;
                    }

                    if (sample > 1.0f)
                    {
                        sample = 1.0f;
                    }

                    writer.Write((short)(sample * short.MaxValue));
                }
            }

            // Get buffer for each channel
            for (int sampleIndex = 0; sampleIndex < numSamplesTotal / channels; sampleIndex++)
            {
                for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    for (int byteIndex = 0; byteIndex < sizeof(short); byteIndex++)
                    {
                        buffers[channelIndex][sampleIndex * sizeof(short) + byteIndex]
                            = dataBytes[sampleIndex * sizeof(short) * channels + sizeof(short) * channelIndex + byteIndex];
                    }
                }
            }

            return new SoundData(reader.WaveFormat.SampleRate, sizeof(short) * 8, buffers, floats);
        }
    }

    //// General method to extract audio data from any supported format
    //public static SoundData GetSound(string audioFilePath)
    //{
    //    string extension = Path.GetExtension(audioFilePath).ToLower();

    //    switch (extension)
    //    {
    //        case ".ogg":
    //            return OggToSoundData(audioFilePath);
    //        default:
    //            throw new NotSupportedException($"Audio format '{extension}' is not supported.");
    //    }
    //}

    public static SoundData GetSound(string audioFilePath)
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
    #endregion
}