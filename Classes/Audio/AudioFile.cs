// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using System.IO;
using System.Linq;
using Godot;
using Tempora.Classes.DataHelpers;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Audio;

/// <summary>
/// Stores audio data and metadata.
/// </summary>
public partial class AudioFile : PcmData
{

    public float[] AudioDataPer10Max = null!;
    public float[] AudioDataPer10Min = null!;


    #region Audio Data

    private float[] pcmLeft = null!;
    /// <summary>
    /// The PCM audio data for one channel
    /// </summary>
    public float[] PcmLeft
    {
        get => pcmLeft;
        set
        {
            pcmLeft = value;
            CalculatePer10s();
        }
    }

    /// <summary>
    /// The audio file contents as stored on the hard drive - i.e. contents of "audio.mp3"
    /// </summary>
    public byte[] FileBuffer
    {
        get;
        private set;
    }
    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>

    public string FilePath = null!;
    #endregion
    /* 
    Explanation of offsets in Tempora.

    The comments I have elsewhere essentially cover the same information, but I tried to write it up as clearly as possible here.
    
    # Tempora uses NAudio's samples as "the truth".
    The playback time you see when you hover audio in Tempora is exactly equivalent to NAudio, whose data is stored in the variable pcmFloats.
    This means that if you have a sample rate of 44100, the sample at NAudio's index 88200 (pcmFloats[0][88200]) should always be correctly displayed at 88200/44100 = 2.00 seconds in Tempora.
    You can verify this by using an audio file whose first audible sound is a quick, short transient. You can then run i.e. Array.FindIndex(pcmFloats[0], x => x > 0.5) to find the sample index.
    Divide this index by the sampleRate to get the time of this sample. This should be the same as when you hover the transient in Tempora.

    # Godot and NAudio do not always have the same origin when the audio is MP3.
    This means that the sample which NAudio understands as index 88200 might have a different index in Godot. 
    We need to figure out where Godot's origin is and take this into account before we start playback of the audio.
    
    # The trouble with headers, encoders and decoders
    My reseach into MP3 files yielded the following conclusions:
    - The MP3 encoder that was used to originally encode the audio typically adds 576 samples of silence in the beginning of the audio file.
    While 576 samples is typical, the exact number can be found by reading the MP3 Lame header, as done in AudioDataHelper.ExtractLameHeaderInfo()
    - All MP3 decodes add 528 samples of delay as mentioned in <see href="https://lame.sourceforge.io/tech-FAQ.txt"
    - One additional sample of silence is added, mentioned in the above link.
    
    So, for a typical MP3 file, there will be 1105 samples of silent samples added to the beginning of the file.
    - These samples ARE included in NAudio's pcmFloats array.
    - These samples ARE NOT (typically) included in Godot's playback logic.
    - The number of silent samples (1105 typically) is stored as the variable startSilenceSamples

    This means that if we want to start playback on NAudio's sample 88200 (visualized as 2:00 in Tempora), 
    we have to tell Godot to start the playback on sample 88200-1105 = 87095

    Doing what's described above works most of the time

    On 2025-05-15 I discovered that some MP3 files in fact have the same offset as NAudio. 
    I observed this for an audio file that did not have a Lame header.
    I assumed that it is the case that if a Lame header is not present, Godot's offset will be the same as NAudio.

    If there are offset issues in the future with MP3 files, maybe step 1 is to challenge this assumption.
     */


    public AudioStream Stream = null!;

    /// <summary>
    /// Audacity's origin seems to be one mp3 frame (1152 samples) earlier than NAudio.  
    /// <para>My best guess as to why: Audacity likely includes the LAME/Xing header in the audio rendering as silence, whereas NAudio doesn't</para>
    /// </summary>
    private float AudacityOriginMP3 => -1152 / (float)sampleRate;
    private const float audacityOriginOGG = 0;
    /// <summary>
    /// For MP3 data, We have 1151 samples less than Audacity has for the same data (according to one single test I made)
    /// 1151 samples / 44100 samples/second = 0.0261 seconds.
    /// This might be down to the differences between Audacity's and NAudio's decoding algorithms, if I were to guess.
    /// </summary>
    private float audacityOrigin;
    public float AudacityOrigin => audacityOrigin;


    /// <summary>
    /// The timewise position of playback origin, counting with the first sample <see cref="AudioFile.SoundData.Floats[0][0]"/> being 0:000
    /// Both the Godot AudioStreamPlayer and Osu take the built-in silence in the beginning of MP3 files into account, placing the origin where the audio actually starts.
    /// <para>Take the example of an audio file "click-quick", which started as .ogg and was encoded as mp3: 
    /// for both audacity and NAudio, in click-quick.ogg, index 1 has value 0.755. 
    /// For NAudio, in click-quick.mp3, index 1106 has value 0.755. Audacity has it one frame (1152 samples) later.</para>
    /// <para>My best take on where the number comes from follows here. 
    /// This logic is implemented in <see cref="AudioDataHelper.DecodeMp3(NAudio.Wave.Mp3FileReader, out byte[], out int, out int, out int, out int)"/></para>
    /// <para>The encoder adds 576 samples of delay. This can be found via <see cref="AudioDataHelper.ExtractLameHeaderInfo(byte[], out int, out int)"/>.
    /// All MP3 decodes add 528 samples of delay as mentioned in <see href="https://lame.sourceforge.io/tech-FAQ.txt"/>.
    /// Add 1 sample, which is also mentioned in the link. This gets us to 1105 for click-quick.mp3, which is what we observe.</para>
    /// </summary>
    private float playbackOrigin_Seconds => startSilenceSamples / (float)sampleRate;

    private string extension = "";
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
                ".mp3" => AudacityOriginMP3,
                ".ogg" => audacityOriginOGG,
                _ => 0
            };
        }
    }

    public AudioFile(string path) : base(path)
    {
        if (!IsAudioFileExtensionValid(path, out string extension))
            throw new Exception($"Failed to create AudioFile with path {path} : Extention was not valid!");

        var audioStream = GetAudioStream(path, out byte[] fileBuffer);

        if (audioStream == null)
            throw new Exception($"Failed to create AudioFile with path {path} : Could not create an AudioStream");

        // For now, use left channel
        PcmLeft = PcmFloats[0];

        Extension = extension;
        Stream = audioStream;
        this.FileBuffer = fileBuffer;
        FilePath = path;
    }

    public AudioFile(AudioStreamMP3 audioStreamMP3) : base(audioStreamMP3.Data, ".mp3")
    {
        // For now, use left channel
        PcmLeft = PcmFloats[0];
        Stream = audioStreamMP3;
        Extension = ".mp3";
        FileBuffer = audioStreamMP3.Data;
    }

    public int SampleTimeToSampleIndex(float seconds) => (int)Math.Floor(seconds * SampleRate);
    public float SampleIndexToSampleTime(int sampleIndex) => (sampleIndex / (float)SampleRate);

    /// <summary>
    /// Sample Time is the number of seconds from the very first sample, in seconds.
    /// Playback Time is the number of seconds from the Playback origin.
    /// </summary>
    /// <param name="sampleTime"></param>
    /// <returns></returns>
    public float SampleTimeToPlaybackTime(float sampleTime) => sampleTime - playbackOrigin_Seconds;
    /// <summary>
    /// Sample Time is the number of seconds from the very first sample, in seconds.
    /// Playback Time is the number of seconds from the Playback origin.
    /// </summary>
    /// <param name="playbackTime"></param>
    /// <returns></returns>
    public float PlaybackTimeToSampleTime(float playbackTime) => playbackTime + playbackOrigin_Seconds;

    /// <summary>
    /// Return audio duration in seconds
    /// </summary>
    /// <returns></returns>
    public float GetAudioLength() => SampleIndexToSampleTime(PcmLeft.Length - 1);

    public void CalculatePer10s()
    {
        int smallLength = PcmLeft.Length / 10;
        bool isDataLengthDivisibleBy10 = PcmLeft.Length % 10 == 0;
        int length = isDataLengthDivisibleBy10 ? smallLength : smallLength + 1;

        AudioDataPer10Min = new float[length];
        AudioDataPer10Max = new float[length];

        for (int i = 0; i < length - 1; i++)
        {
            AudioDataPer10Min[i] = PcmLeft[(i * 10)..((i * 10) + 10)].Min();
            AudioDataPer10Max[i] = PcmLeft[(i * 10)..((i * 10) + 10)].Max();
        }
        AudioDataPer10Min[length - 1] = PcmLeft[((length - 1) * 10-1)..^1].Min();
        AudioDataPer10Max[length - 1] = PcmLeft[((length - 1) * 10-1)..^1].Max();
    }

    public static bool IsAudioFileExtensionValid(string path, out string extension)
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