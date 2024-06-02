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


    private const float audacityOriginMP3 = -0.0261f;
    private const float audacityOriginOGG = 0;
    /// <summary>
    /// For MP3 data, We have 1151 samples less than Audacity has for the same data (according to one single test I made)
    /// 1151 samples / 44100 samples/second = 0.0261 seconds.
    /// This might be down to the differences between Audacity's and NAudio's decompression algorhithms, if I were to guess.
    /// </summary>
    private float audacityOrigin = audacityOriginMP3;


    //private const float playbackOrigininSecondsMP3 = 0.0512f;
    private const float playbackOrigininSecondsMP3 = 0.0251f;
    private const float playbackOriginInSecondsOGG = 0;
    /// <summary>
    /// The timewise position of playback origin, counting with the first sample <see cref="AudioFile.SoundData.Floats[0][0]"/> being 0:000
    /// Both the Godot AudioStreamPlayer and Osu take the built-in silence in the beginning of MP3 files into account, placing the origin where the audio actuall starts.
    /// </summary>
    private float playbackOriginÍnSeconds = playbackOrigininSecondsMP3;

    private string extension;
    public string Extension
    {
        get => extension;
        private set
        {
            if (value == extension)
                return;
            extension = value;
            audacityOrigin = extension switch
            {
                ".mp3" => audacityOriginMP3,
                ".ogg" => audacityOriginOGG,
                _ => 0
            };
            playbackOriginÍnSeconds = extension switch
            {
                ".mp3" => playbackOrigininSecondsMP3,
                ".ogg" => playbackOriginInSecondsOGG,
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

    public int SampleTimeToSampleIndex(float seconds) => (int)Math.Floor(seconds * SampleRate);
    public float SampleIndexToSampleTime(int sampleIndex) => (sampleIndex / (float)SampleRate);

    /// <summary>
    /// Sample Time is the number of seconds from the very first sample, in seconds.
    /// Playback Time is the number of seconds from the Playback origin.
    /// </summary>
    /// <param name="sampleTime"></param>
    /// <returns></returns>
    public float SampleTimeToPlaybackTime(float sampleTime) => sampleTime - playbackOriginÍnSeconds;
    /// <summary>
    /// Sample Time is the number of seconds from the very first sample, in seconds.
    /// Playback Time is the number of seconds from the Playback origin.
    /// </summary>
    /// <param name="playbackTime"></param>
    /// <returns></returns>
    public float PlaybackTimeToSampleTime(float playbackTime) => playbackTime + playbackOriginÍnSeconds;

    /// <summary>
    /// Return audio duration in seconds
    /// </summary>
    /// <returns></returns>
    public float GetAudioLength() => SampleIndexToSampleTime(PCMFloats.Length - 1);

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