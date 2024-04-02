using System;
using System.Linq;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Audio;

public partial class AudioFile : Node
{
    private float[] _audioData = null!;
    public float[] AudioData
    {
        get => _audioData;
        set
        {
            _audioData = value;
            CalculatePer10s();
        }
    }

    public float[] AudioDataPer10Max = null!;
    public float[] AudioDataPer10Min = null!;

    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>
    public int Channels;

    public string Path = null!;

    public AudioStream Stream = null!;

    /// <summary>
    ///     Amount of seconds to offset any sample indices. Band-aid fix to compensate the discrepancy between audio playback
    ///     and audio visualization.
    /// </summary>
    public float SampleIndexOffsetInSeconds = 0.025f;

    public int SampleRate;

    public AudioFile(string path)
    {
        string extension = FileHandler.GetExtension(path);
        if (extension != "mp3")
            throw new Exception($"Failed to create AudioFile with path {path} : Extention was not .mp3!");

        byte[] audioFileBytes = FileHandler.GetFileAsBuffer(path);

        var audioStreamMP3 = new AudioStreamMP3
        {
            Data = audioFileBytes
        };

        float[] audioData = AudioDataHandler.Mp3ToAudioFloat(audioFileBytes, out int sampleRate, out int channels);

        AudioData = audioData;
        SampleRate = sampleRate;
        //Path = path;
        Stream = audioStreamMP3;
        Channels = channels;
    }

    public AudioFile(AudioStreamMP3 audioStreamMP3)
    {
        byte[] audioFileBytes = audioStreamMP3.Data;

        float[] audioData = AudioDataHandler.Mp3ToAudioFloat(audioFileBytes, out int sampleRate, out int channels);

        AudioData = audioData;
        SampleRate = sampleRate;
        Stream = audioStreamMP3;
        Channels = channels;
    }

    public int SecondsToSampleIndex(float seconds)
    {
        int sampleIndex = (int)Math.Floor((seconds + SampleIndexOffsetInSeconds) * SampleRate * Channels);
        //int sampleIndexClamped = Math.Clamp(sampleIndex, 0, AudioData.Length);
        return sampleIndex;
    }

    public float SampleIndexToSeconds(int sampleIndex) => (sampleIndex / (float)SampleRate / Channels) - SampleIndexOffsetInSeconds;

    public float[] GetAudioDataSegment(int sampleStart, int sampleStop)
    {
        if (sampleStart < 0)
            sampleStart = 0;
        if (sampleStop < 0)
            sampleStop = 0;
        if (sampleStop > AudioData.Length)
            sampleStop = AudioData.Length;
        if (sampleStart > AudioData.Length)
            sampleStop = AudioData.Length;

        float[] audioDataSegment = AudioData[sampleStart..sampleStop];

        return audioDataSegment;
    }

    public float[] GetAudioDataSegment(float secondsStart, float secondsStop)
    {
        int sampleStart = SecondsToSampleIndex(secondsStart);
        int sampleStop = SecondsToSampleIndex(secondsStop);

        return GetAudioDataSegment(sampleStart, sampleStop);
    }

    /// <summary>
    /// Return audio duration in seconds
    /// </summary>
    /// <returns></returns>
    public float GetAudioLength() => SampleIndexToSeconds(AudioData.Length - 1);

    public void CalculatePer10s()
    {
        int smallLength = AudioData.Length / 10;
        bool isDataLengthDivisibleBy10 = AudioData.Length % 10 == 0;
        int length = isDataLengthDivisibleBy10 ? smallLength : smallLength + 1;

        AudioDataPer10Min = new float[length];
        AudioDataPer10Max = new float[length];

        for (int i = 0; i < length - 1; i++)
        {
            AudioDataPer10Min[i] = AudioData[(i * 10)..((i * 10) + 10)].Min();
            AudioDataPer10Max[i] = AudioData[(i * 10)..((i * 10) + 10)].Max();
        }
        AudioDataPer10Min[length - 1] = AudioData[((length - 1) * 10)..^1].Min();
        AudioDataPer10Max[length - 1] = AudioData[((length - 1) * 10)..^1].Max();
    }
}