using System;
using System.IO;
using System.Linq;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Audio;

public partial class AudioFile : Node
{

    public float[] AudioDataPer10Max = null!;
    public float[] AudioDataPer10Min = null!;


    #region Audio Data
    public SoundData? SoundData;

    private float[] _pcmFloats = null!;
    /// <summary>
    /// The PCM audio data for one channel
    /// </summary>
    public float[] PCMFloats
    {
        get => _pcmFloats;
        set
        {
            _pcmFloats = value;
            CalculatePer10s();
        }
    }

    public byte[] AudioBuffer
    {
        get;
        private set;
    }
    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>
    private int Channels;
    public int SampleRate;

    public string AudioPath = null!; 
    #endregion

    public AudioStream Stream = null!;

    private static float sampleIndexOffsetInSecondsMP3 = 0.025f;
    //private static float sampleIndexOffsetInSecondsMP3 = 0.0f;
    private static float sampleIndexOffsetInSecondsOGG = 0;

    /// <summary>
    ///     Amount of seconds to offset any sample indices for the visuals. Band-aid fix to compensate the discrepancy between audio playback
    ///     and audio visualization. The result can be verified by comparing with how Audacity displays audio. This should not affect timing, only display of audio.
    /// </summary>
    public float SampleIndexOffsetInSeconds = sampleIndexOffsetInSecondsMP3;

    private string extension;
    public string Extension
    {
        get => extension;
        private set
        {
            if (value == extension)
                return;
            extension = value;
            SampleIndexOffsetInSeconds = extension switch
            {
                ".mp3" => sampleIndexOffsetInSecondsMP3,
                ".ogg" => sampleIndexOffsetInSecondsOGG,
                _ => 0
            };
        }
    }

    public AudioFile(string path)
    {
        if (!IsAudioFileExtensionValid(path, out string extension))
            throw new Exception($"Failed to create AudioFile with path {path} : Extention was not valid!");

        var audioStream = GetAudioStream(path, out byte[] audioBuffer);

        if (audioStream == null)
            throw new Exception($"Failed to create AudioFile with path {path} : Could not create an AudioStream");

        //AudioData = AudioDataHandler.ExtractAudioFloat(path, out SampleRate, out Channels);

        SoundData = AudioDataHandler.GetSoundData(path);
        SampleRate = SoundData.SampleRate;
        Channels = SoundData.Channels;

        // For now, use left channel
        PCMFloats = SoundData.Floats[0];

        Extension = extension;
        Stream = audioStream;
        AudioBuffer = audioBuffer;
        AudioPath = path;
    }

    public AudioFile(AudioStreamMP3 audioStreamMP3)
    {
        byte[] buffer = audioStreamMP3.Data;

        //float[] audioData = AudioDataHandler.Mp3ToAudioFloat(buffer, out int sampleRate, out int channels);

        SoundData = AudioDataHandler.GetSoundData(buffer, ".mp3");
        SampleRate = SoundData.SampleRate;
        Channels = SoundData.Channels;

        // For now, use left channel
        PCMFloats = SoundData.Floats[0];

        Stream = audioStreamMP3;
        Extension = ".mp3";
        AudioBuffer = buffer;
    }

    public int SecondsToSampleIndex(float seconds)
    {
        int sampleIndex = (int)Math.Floor((seconds + SampleIndexOffsetInSeconds) * SampleRate );
        return sampleIndex;
    }

    public float SampleIndexToSeconds(int sampleIndex) => (sampleIndex / (float)SampleRate) - SampleIndexOffsetInSeconds;

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
    public float GetAudioLength() => SampleIndexToSeconds(PCMFloats.Length - 1);

    public void CalculatePer10s()
    {
        int smallLength = PCMFloats.Length / 10;
        bool isDataLengthDivisibleBy10 = PCMFloats.Length % 10 == 0;
        int length = isDataLengthDivisibleBy10 ? smallLength : smallLength + 1;

        AudioDataPer10Min = new float[length];
        AudioDataPer10Max = new float[length];

        for (int i = 0; i < length - 1; i++)
        {
            AudioDataPer10Min[i] = PCMFloats[(i * 10)..((i * 10) + 10)].Min();
            AudioDataPer10Max[i] = PCMFloats[(i * 10)..((i * 10) + 10)].Max();
        }
        AudioDataPer10Min[length - 1] = PCMFloats[((length - 1) * 10-1)..^1].Min();
        AudioDataPer10Max[length - 1] = PCMFloats[((length - 1) * 10-1)..^1].Max();
    }

    private bool IsAudioFileExtensionValid(string path, out string extension)
    {
        extension = Path.GetExtension(path).ToLower();
        if (extension != ".mp3" && extension != ".ogg")
            return false;
        return true;
    }

    private AudioStream? GetAudioStream(string path, out byte[] fileBuffer)
    {
        string extension = Path.GetExtension(path).ToLower();
        AudioStream? audioStream = null;
        fileBuffer = FileHandler.GetFileAsBuffer(path);

        switch (extension)
        {
            case ".mp3":
                audioStream = new AudioStreamMP3()
                {
                    Data = fileBuffer
                };
                break;
            case ".ogg":
                audioStream = AudioStreamOggVorbis.LoadFromBuffer(fileBuffer);
                break;
        }
        return audioStream;
    }
}