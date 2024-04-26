using System;
using System.IO;
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

    public byte[] AudioBuffer
    {
        get;
        private set;
    }

    public float[] AudioDataPer10Max = null!;
    public float[] AudioDataPer10Min = null!;

    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>
    public int Channels;

    public string AudioPath = null!;

    public AudioStream Stream = null!;

    /// <summary>
    ///     Amount of seconds to offset any sample indices. Band-aid fix to compensate the discrepancy between audio playback
    ///     and audio visualization.
    /// </summary>
    public float SampleIndexOffsetInSeconds = 0.025f;

    public int SampleRate;

    public string Extension
    {
        get;
        private set;
    }

    public AudioFile(string path)
    {
        if (!IsAudioFileExtensionValid(path, out string extension))
            throw new Exception($"Failed to create AudioFile with path {path} : Extention was not valid!");

        var audioStream = GetAudioStream(path, out byte[] audioBuffer);

        if (audioStream == null)
            throw new Exception($"Failed to create AudioFile with path {path} : Could not create an AudioStream");

        //float[] audioData = new float[] { 0 };
        //int sampleRate = 44100;
        //int channels = 2;

        //switch (extension)
        //{
        //    case ".mp3":
        //        audioData = AudioDataHandler.Mp3ToAudioFloat(audioBuffer, out sampleRate, out channels);
        //        break;
        //    case ".ogg":
        //        audioData = AudioDataHandler.OggToAudioFloat(audioBuffer, out sampleRate, out channels);
        //        break;
        //}
        //Channels = channels;
        //AudioData = audioData;
        //SampleRate = sampleRate;

        AudioData = AudioDataHandler.ExtractAudioFloat(path, out SampleRate, out Channels);

        Extension = extension;
        Stream = audioStream;
        Extension = extension;
        AudioBuffer = audioBuffer;
        AudioPath = path;
    }

    public AudioFile(AudioStreamMP3 audioStreamMP3)
    {
        byte[] buffer = audioStreamMP3.Data;

        float[] audioData = AudioDataHandler.Mp3ToAudioFloat(buffer, out int sampleRate, out int channels);

        AudioData = audioData;
        SampleRate = sampleRate;
        Stream = audioStreamMP3;
        Channels = channels;
        Extension = ".mp3";
        AudioBuffer = buffer;
    }

    public int SecondsToSampleIndex(float seconds)
    {
        int sampleIndex = (int)Math.Floor((seconds + SampleIndexOffsetInSeconds) * SampleRate * Channels);
        return sampleIndex;
    }

    public float SampleIndexToSeconds(int sampleIndex) => (sampleIndex / (float)SampleRate / Channels) - SampleIndexOffsetInSeconds;

    //public float[] GetAudioDataSegment(int sampleStart, int sampleStop)
    //{
    //    if (sampleStart < 0)
    //        sampleStart = 0;
    //    if (sampleStop < 0)
    //        sampleStop = 0;
    //    if (sampleStop > AudioData.Length)
    //        sampleStop = AudioData.Length;
    //    if (sampleStart > AudioData.Length)
    //        sampleStop = AudioData.Length;

    //    float[] audioDataSegment = AudioData[sampleStart..sampleStop];

    //    return audioDataSegment;
    //}

    //public float[] GetAudioDataSegment(float secondsStart, float secondsStop)
    //{
    //    int sampleStart = SecondsToSampleIndex(secondsStart);
    //    int sampleStop = SecondsToSampleIndex(secondsStop);

    //    return GetAudioDataSegment(sampleStart, sampleStop);
    //}

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

    private bool IsAudioFileExtensionValid(string path, out string extension)
    {
        extension = Path.GetExtension(path).ToLower();
        if (extension != ".mp3" && extension != ".ogg")
            return false;
        return true;
    }

    private AudioStream? GetAudioStream(string path, out byte[] buffer)
    {
        string extension = Path.GetExtension(path).ToLower();
        AudioStream? audioStream = null;
        buffer = FileHandler.GetFileAsBuffer(path);

        switch (extension)
        {
            case ".mp3":
                audioStream = new AudioStreamMP3()
                {
                    Data = buffer
                };
                break;
            case ".ogg":
                audioStream = AudioStreamOggVorbis.LoadFromBuffer(buffer);
                break;
        }
        return audioStream;
    }
}