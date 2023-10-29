using Godot;
using NAudio.Wave;
using System;
using System.IO;

public partial class AudioDataHandler : Node
{

    /// <summary>
    /// Each audio sample consists of TWO consecutive bytes, since the audio is 16-bit.
    /// This method outputs an array with audio samples
    /// </summary>
    /// <param name="audioBytes"></param>
    /// <returns></returns>
    public static short[] AudioBytesToSamples(Byte[] audioBytes)
    {
        short[] audioSamplesShort = new short[audioBytes.Length / 2];
        Buffer.BlockCopy(audioBytes, 0, audioSamplesShort, 0, audioBytes.Length);

        return audioSamplesShort;
    }

    /// <summary>
    /// Use NAudio to extract the audio data from a .wav file (i.e. removes the header) 
    /// </summary>
    /// <param name="wavFile"></param>
    /// <returns></returns>
    public static short[] WavToAudioSamples(Byte[] wavFile, out int sampleRate, out int channels)
    {
        Byte[] audioBytes;
        using (MemoryStream audioFileMemoryStream = new MemoryStream(wavFile))
        {
            using (WaveFileReader reader = new WaveFileReader(audioFileMemoryStream))
            {
                audioBytes = new Byte[reader.Length];
                reader.Read(audioBytes, 0, audioBytes.Length);
                sampleRate = reader.WaveFormat.SampleRate;
                channels = reader.WaveFormat.Channels;
            }
        }

        return AudioBytesToSamples(audioBytes);
    }

    public static short[] WavToAudioSamples(Byte[] wavFile)
    {
        int sampleRate;
        int channels;
        return WavToAudioSamples(wavFile, out sampleRate, out channels);
    }

    public static short[] Mp3ToAudioSamples(Byte[] mp3File, out int sampleRate, out int channels)
    {
        Byte[] audioBytes;
        using (MemoryStream audioFileMemoryStream = new MemoryStream(mp3File))
        {
            using (Mp3FileReader reader = new Mp3FileReader(audioFileMemoryStream))
            {
                audioBytes = new Byte[reader.Length];
                reader.Read(audioBytes, 0, audioBytes.Length);
                sampleRate = reader.Mp3WaveFormat.SampleRate;
                channels = reader.Mp3WaveFormat.Channels;
            }
        }

        return AudioBytesToSamples(audioBytes);
    }

    public static short[] Mp3ToAudioSamples(Byte[] wavFile)
    {
        int sampleRate;
        int channels;
        return Mp3ToAudioSamples(wavFile, out sampleRate, out channels);
    }

    /// <summary>
    /// Convert short[] audio sample array into float[] array spanning -1 to 1
    /// </summary>
    /// <param name="audioSamplesShort"></param>
    /// <returns></returns>
    public static float[] AudioSamplesToFloat(short[] audioSamplesShort)
    {
        float[] audioSamplesFloat = new float[audioSamplesShort.Length];
        for (int i = 0; i < audioSamplesShort.Length; i++)
        {
            audioSamplesFloat[i] = audioSamplesShort[i] / (float)short.MaxValue;
        }
        return audioSamplesFloat;
    }

    /// <summary>
    /// Extract audio data from a .wav and convert into a float array with range -1..1
    /// </summary>
    /// <param name="wavFile"></param>
    /// <returns></returns>
    public static float[] WavToAudioFloat(Byte[] wavFile) => AudioSamplesToFloat(WavToAudioSamples(wavFile));
    public static float[] WavToAudioFloat(Byte[] wavFile, out int sampleRate, out int channels) => AudioSamplesToFloat(WavToAudioSamples(wavFile, out sampleRate, out channels));

    public static float[] Mp3ToAudioFloat(Byte[] mp3File) => AudioSamplesToFloat(Mp3ToAudioSamples(mp3File));
    public static float[] Mp3ToAudioFloat(Byte[] mp3File, out int sampleRate, out int channels) => AudioSamplesToFloat(Mp3ToAudioSamples(mp3File, out sampleRate, out channels));
}
