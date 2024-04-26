using System;
using System.IO;
using Godot;
using NAudio.Wave;
using NAudio.Vorbis;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Tempora.Classes.Utility;

public partial class AudioDataHandler : Node
{
    public static float[] Mp3ToAudioFloat(byte[] mp3File, out int sampleRate, out int channels) => AudioSamplesToFloat(Mp3ToAudioSamples(mp3File, out sampleRate, out channels));


    // General method to extract audio data from any supported format
    public static float[] ExtractAudioFloat(string audioFilePath, out int sampleRate, out int channels)
    {
        string extension = Path.GetExtension(audioFilePath).ToLower();

        switch (extension)
        {
            case ".wav":
                return WavToAudioFloat(audioFilePath, out sampleRate, out channels);
            case ".mp3":
                return Mp3ToAudioFloat(audioFilePath, out sampleRate, out channels);
            case ".ogg":
                return OggToAudioFloat(audioFilePath, out sampleRate, out channels);
            default:
                throw new NotSupportedException($"Audio format '{extension}' is not supported.");
        }
    }

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

        int bytesPerSample = bitsPerSample / 8;
        int numSamples = audioBytes.Length / bytesPerSample;
        int[] audioSamplesInt = new int[numSamples];

        // Iterate through the byte array and convert bytes to integers
        for (int i = 0; i < numSamples; i++)
        {
            var sampleBytes = new byte[bytesPerSample];
            for (int j = 0; j < bytesPerSample; j++)
            {
                sampleBytes[j] = audioBytes[i + j];
            }

            int sampleInt = 0;
            switch (bitsPerSample)
            {
                case 16:
                    var sampleShort = BitConverter.ToInt16(sampleBytes);
                    sampleInt = (int)sampleShort;
                    break;
                case 32:
                    sampleInt = BitConverter.ToInt32(sampleBytes);
                    break;
                default:
                    throw new NotImplementedException("Only 16-bit and 32-bit audio is supported for now. Ping me and maybe I can change this if there's demand for it. (@kongehund)");
            }
            //int sampleInt = BitConverter.ToInt32(sampleBytes, 0);

            audioSamplesInt[i] = sampleInt;
        }

        //    // Iterate through the byte array and convert bytes to integers
        //for (int i = 0; i < numSamples; i++)
        //{
        //    // Combine bytes into an integer based on little-endian encoding
        //    int value = 0;

        //    // Adjust the sign if the most significant bit is set
        //    // the range 0b_0000_0000 to 0b_0111_1111 represent negative samples.
        //    // the range 0b_1000_0000 ro 0b_1111_1111 represent positive samples.
        //    // 0x80 is equal to 0b_1000_0000
        //    // the bitwise & operation will turn every number below 0b_1000_0000 into 0.
        //    bool isNegative = (audioBytes[i * bytesPerSample + bytesPerSample - 1] & 0x80) != 0;

        //    // If the number is negative, set all higher bits to 1
        //    if (isNegative)
        //    {
        //        for (int j = bytesPerSample; j < 4; j++)
        //        {
        //            value |= 0xFF << (8 * j);
        //        }
        //    }

        //    // Combine bytes into an integer
        //    for (int j = bytesPerSample - 1; j >= 0; j--)
        //    {
        //        value |= audioBytes[i * bytesPerSample + j] << (8 * j);
        //    }

        //    audioSamplesInt[i] = value;
        //}

        return audioSamplesInt;
    }

    // Convert audio samples to float array
    private static float[] AudioSamplesToFloat(int[] audioSamplesInt)
    {
        float[] audioSamplesFloat = new float[audioSamplesInt.Length];
        float maxValue = (float)audioSamplesInt.Max();
        //float maxValue = int.MaxValue;
        for (int i = 0; i < audioSamplesInt.Length; i++)
            audioSamplesFloat[i] = audioSamplesInt[i] / maxValue;
        return audioSamplesFloat;
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
}