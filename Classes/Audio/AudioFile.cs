using System;
using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Audio;

public partial class AudioFile : Node {
    public float[] AudioData;

    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>
    public int Channels;

    public string Path;

    /// <summary>
    ///     Amount of seconds to offset any sample indices. Band-aid fix to compensate the discrepancy between audio playback
    ///     and audio visualization.
    /// </summary>
    public float SampleIndexOffsetInSeconds = 0.025f;

    public int SampleRate;

    public AudioFile(string path) {
        string extension = FileHandler.GetExtension(path);
        if (extension != "mp3") throw new Exception($"Failed to create AudioFile with path {path} : Extention was not .mp3!");

        byte[] audioFileBytes = FileHandler.GetFileAsBuffer(path);

        int sampleRate;
        int channels;
        float[] audioData = AudioDataHandler.Mp3ToAudioFloat(audioFileBytes, out sampleRate, out channels);

        AudioData = audioData;
        SampleRate = sampleRate;
        Path = path;
        Channels = channels;
    }

    public int SecondsToSampleIndex(float seconds) {
        var sampleIndex = (int)Math.Floor((seconds + SampleIndexOffsetInSeconds) * SampleRate * Channels);
        //int sampleIndexClamped = Math.Clamp(sampleIndex, 0, AudioData.Length);
        return sampleIndex;
    }

    public float SampleIndexToSeconds(int sampleIndex) {
        return sampleIndex / (float)SampleRate / Channels - SampleIndexOffsetInSeconds;
    }

    public float[] GetAudioDataSegment(int sampleStart, int sampleStop) {
        if (sampleStart < 0) sampleStart = 0;
        if (sampleStop < 0) sampleStop = 0;
        if (sampleStop > AudioData.Length) sampleStop = AudioData.Length;
        if (sampleStart > AudioData.Length) sampleStop = AudioData.Length;

        float[] audioDataSegment = AudioData[sampleStart..sampleStop];

        return audioDataSegment;
    }

    public float[] GetAudioDataSegment(float secondsStart, float secondsStop) {
        int sampleStart = SecondsToSampleIndex(secondsStart);
        int sampleStop = SecondsToSampleIndex(secondsStop);

        return GetAudioDataSegment(sampleStart, sampleStop);
    }

    /// <summary>
    /// Return audio duration in seconds
    /// </summary>
    /// <returns></returns>
    public float GetAudioLength()
    {
        return SampleIndexToSeconds(AudioData.Length - 1);
    }
}