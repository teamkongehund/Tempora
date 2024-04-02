using System;
using System.IO;
using System.Reflection.PortableExecutable;
using Godot;
using NAudio.Utils;
using NAudio.Lame;
using NAudio.Wave;

using Tempora.Classes.Utility;

namespace Tempora.Classes.Audio;

public partial class AudioFile : Node
{
    protected float[] _audioData = null!;
    public virtual float[] AudioData
    {
        get => _audioData;
        set => _audioData = value;
    }

    public float[] AudioDataPer10Max = null!;
    public float[] AudioDataPer10Min = null!;

    /// <summary>
    ///     1 = mono , 2 = stereo
    /// </summary>
    public int Channels;

    public string Path = null!;

    public event EventHandler StreamChanged = null!;

    private AudioStream stream = null!;
    public AudioStream Stream
    {
        get => stream;
        set
        {
            stream = value;
            StreamChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Amount of seconds to offset any sample indices. Band-aid fix to compensate the discrepancy between audio playback
    ///     and audio visualization.
    /// </summary>
    public float SampleIndexOffsetInSeconds = 0.025f;

    public int SampleRate;

    public AudioFile()
    {

    }

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

    public AudioFile(AudioStreamWav audioStreamWav)
    {
        byte[] audioFileBytes = audioStreamWav.Data;

        float[] audioData = AudioDataHandler.WavToAudioFloat(audioFileBytes, out int sampleRate, out int channels);

        AudioData = audioData;
        SampleRate = sampleRate;
        Stream = audioStreamWav;
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

    public static AudioFile PrependSilence(AudioFile oldAudioFile, float secondsToAdd)
    {
        int samplesToAdd = oldAudioFile.SecondsToSampleIndex(secondsToAdd);

        byte[] newData;

        if (oldAudioFile.Stream is AudioStreamMP3 audioStreamMP3)
        {
            var data = audioStreamMP3.Data;

            using MemoryStream oldMemoryStream = new MemoryStream(data);

            using (var reader = new Mp3FileReader(oldMemoryStream))
            {
                // Create a WaveFormat instance based on the input MP3 file
                var waveFormat = reader.WaveFormat;

                // Create a new MemoryStream to hold the modified audio data
                using var newMemoryStream = new MemoryStream();
                // Create a new WaveFileWriter to write the modified audio data
                using (var writer = new LameMP3FileWriter(newMemoryStream, waveFormat, LAMEPreset.STANDARD))
                {

                    // Write one second of silence
                    int numBytesToAdd = (int)(waveFormat.AverageBytesPerSecond * secondsToAdd);
                    var silence = new byte[numBytesToAdd];
                    writer.Write(silence, 0, silence.Length);

                    // Copy the original audio data after the silence
                    //GD.Print($"About to copy. oldMemoryStream.Length: {oldMemoryStream.Length} VS. newMemoryStream.Length: {newMemoryStream.Length}");
                    reader.CopyTo(writer);
                    //GD.Print($"Copy done. oldMemoryStream.Length: {oldMemoryStream.Length} VS. newMemoryStream.Length: {newMemoryStream.Length}");
                }

                newData = newMemoryStream.ToArray();
            }

            AudioStreamMP3 newAudioStreamMP3 = new AudioStreamMP3()
            {
                Data = newData
            };

            AudioFile newAudioFile = new(newAudioStreamMP3);

            return newAudioFile;
        }
        else if (oldAudioFile.Stream is AudioStreamWav audioStreamWav)
        {
            var data = audioStreamWav.Data;

            using MemoryStream oldMemoryStream = new MemoryStream(data);

            using (var reader = new WaveFileReader(oldMemoryStream))
            {
                // Create a WaveFormat instance based on the input MP3 file
                var waveFormat = reader.WaveFormat;

                // Create a new MemoryStream to hold the modified audio data
                using var newMemoryStream = new MemoryStream();
                // Create a new WaveFileWriter to write the modified audio data
                using (var writer = new WaveFileWriter(new IgnoreDisposeStream(newMemoryStream), new WaveFormat(waveFormat.SampleRate, waveFormat.BitsPerSample, waveFormat.Channels)))
                {
                    // Write one second of silence
                    int numBytesToAdd = (int)(waveFormat.AverageBytesPerSecond * secondsToAdd);
                    var silence = new byte[numBytesToAdd];
                    writer.Write(silence, 0, silence.Length);

                    // Copy the original audio data after the silence
                    reader.CopyTo(writer);
                }

                newData = newMemoryStream.ToArray();
            }

            AudioStreamWav newAudioStreamWav = new AudioStreamWav()
            {
                Data = newData
            };

            AudioFile newAudioFile = new(newAudioStreamWav);

            return newAudioFile;
        }

        GD.Print("AudioFile.PrependSilence : Unable to prepend silence. Returning oldAudioFile");
        return oldAudioFile;
    }
}