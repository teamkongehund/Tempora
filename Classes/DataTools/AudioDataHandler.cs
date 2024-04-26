using System;
using System.IO;
using Godot;
using NAudio.Wave;
using NAudio.Vorbis;
using System.Runtime.CompilerServices;

namespace Tempora.Classes.Utility;

public partial class AudioDataHandler : Node
{
    public static float[] Mp3ToAudioFloat(byte[] mp3File, out int sampleRate, out int channels) => AudioSamplesToFloat(Mp3ToAudioSamples(mp3File, out sampleRate, out channels));
    public static short[] Mp3ToAudioSamples(byte[] mp3File, out int sampleRate, out int channels)
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

    // Convert audio bytes to samples
    private static short[] AudioBytesToSamples(byte[] audioBytes, int bitsPerSample = 16)
    {
        int bytesPerSample = bitsPerSample / 8;
        short[] audioSamplesShort = new short[audioBytes.Length / bytesPerSample];
        Buffer.BlockCopy(audioBytes, 0, audioSamplesShort, 0, audioBytes.Length);
        return audioSamplesShort;
    }

    // Convert audio samples to float array
    private static float[] AudioSamplesToFloat(short[] audioSamplesShort)
    {
        float[] audioSamplesFloat = new float[audioSamplesShort.Length];
        for (int i = 0; i < audioSamplesShort.Length; i++)
            audioSamplesFloat[i] = audioSamplesShort[i] / (float)short.MaxValue;
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

        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes));
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

        return AudioSamplesToFloat(AudioBytesToSamples(audioBytes));
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
}