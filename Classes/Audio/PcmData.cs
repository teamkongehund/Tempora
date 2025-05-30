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
using Tempora.Classes.DataHelpers;
// This script is partially based on https://github.com/leezer3/OpenBVE/blob/84064b7ef4e51def0b26e07226c114c004bcd4d3/source/OpenBveApi/Sound.cs whose license is public domain.

namespace Tempora.Classes.Audio;


/// <summary>
/// Stores audio PCM data. The PCM data is stored twice - once as <see cref="PcmFloats"/> and once as <see cref="PcmBytes"/>.
/// <para>"PCM": Pulse-code modulation. 
/// Any format to store audio where the audio is split up into evenly-spaced samples with a given <see cref="SampleRate"/> i.e. 41000 samples/second.
/// The value for each sample is the amplitude of the sample. For floats, the range is -1 to 1.
/// For bytes, it depends on <see cref="BitsPerSample"/></para>
/// </summary>
public class PcmData
{
    #region fields
    protected int sampleRate;
    protected int bitsPerSample;
    protected byte[][] pcmBytes;
    protected float[][] pcmFloats;
    protected int channels;
    protected int startSilenceSamples = 0;
    #endregion
    #region constructors
    /// <param name="sampleRate">The number of samples per second.</param>
    /// <param name="bitsPerSample">The number of bits per sample. Allowed values are 8 or 16.</param>
    /// <param name="pcmBytes">The PCM sound data per channel in little-endian order. For 8 bits per sample, samples are unsigned from 0 to 255. For 16 bits per sample, samples are signed from -32768 to 32767 and in little endian byte order.</param>
    /// <param name="pcmFloats">The PCM sound data per channel. </param>
    /// <exception cref="System.ArgumentNullException">Raised when the bytes array or any of its subarrays is a null reference.</exception>
    /// <exception cref="System.ArgumentException">Raised when the bytes' subarrays are of unequal length.</exception>
    /// <exception cref="System.ArgumentException">Raised when the number of bits per samples is neither 8 nor 16.</exception>
    public PcmData(int sampleRate, int bitsPerSample, byte[][] pcmBytes, float[][] pcmFloats, int playbackOriginSample = 0)
    {
        if (pcmBytes == null)
        {
            throw new ArgumentNullException("The data bytes are a null reference.");
        }
        for (int i = 0; i < pcmBytes.Length; i++)
        {
            if (pcmBytes[i] == null)
            {
                throw new ArgumentNullException("The data bytes of a particular channel is a null reference.");
            }
        }
        for (int i = 1; i < pcmBytes.Length; i++)
        {
            if (pcmBytes[i].Length != pcmBytes[0].Length)
            {
                throw new ArgumentException("The data bytes of the channels are of unequal length.");
            }
        }
        if (bitsPerSample != 8 & bitsPerSample != 16)
        {
            throw new ArgumentException("The number of bits per sample is neither 8 nor 16.");
        }
        else
        {
            this.sampleRate = sampleRate;
            this.bitsPerSample = bitsPerSample;
            this.pcmBytes = pcmBytes;
            this.pcmFloats = pcmFloats;
            this.startSilenceSamples = playbackOriginSample;
        }
    }

    public PcmData(string audioFilePath)
    {
        pcmBytes = new byte[0][];
        pcmFloats = new float[0][];

        string extension = Path.GetExtension(audioFilePath).ToLower();

        switch (extension)
        {
            case ".ogg":
                InitializeFromOgg(audioFilePath);
                break;
            case ".mp3":
                InitializeFromMp3(audioFilePath);
                break;
            //case ".wav":
            //    return WavToSoundData(audioFilePath);
            default:
                throw new NotSupportedException($"Audio format '{extension}' is not supported.");
        }
    }

    public PcmData(byte[] fileBuffer, string extension)
    {
        pcmBytes = new byte[0][];
        pcmFloats = new float[0][];

        switch (extension)
        {
            case ".ogg":
                throw new NotSupportedException("AudioDataHandler does not yet support retrieving SoundData by parsing an OGG buffer.");
            case ".mp3":
                InitializeFromMp3(fileBuffer);
                break;
            default:
                throw new NotSupportedException($"Audio format '{extension}' is not supported.");
        }
    }

    private void InitializeFromOgg(string path)
    {
        // Based on https://github.com/leezer3/OpenBVE/blob/84064b7ef4e51def0b26e07226c114c004bcd4d3/source/Plugins/Sound.Vorbis/Plugin.Parser.cs#L12

        AudioDataHelper.DecodeOgg(path, out float[] floatsMixed, out int channels, out int sampleRate, out int numSamplesCombined);

        // Convert PCM bit depth from 32-bit float to 16-bit integer.
        byte[] shortsMixed_byte = AudioDataHelper.ConvertToPCM8(floatsMixed);
        float[][] floats = AudioDataHelper.SeparateByChannels(floatsMixed, channels);
        byte[][] shorts_byte = AudioDataHelper.SeparateByChannels(shortsMixed_byte, channels);

        InitializeValues(sampleRate, channels, floats, shorts_byte, 16, 0);
    }

    private void InitializeFromMp3(string path)
    {
        AudioDataHelper.DecodeMp3(path, out byte[] shortsMixed_byte, out int channels,
            out int sampleRate, out int numRawBytes, out int startSilenceSamples);
        var floats = AudioDataHelper.ConvertToFloats(shortsMixed_byte, channels, out _, out byte[][] shorts_byte);
        InitializeValues(sampleRate, channels, floats, shorts_byte, 16, startSilenceSamples);
    }

    private void InitializeFromMp3(byte[] mp3File)
    {
        AudioDataHelper.DecodeMp3(mp3File, out byte[] shortsMixed_byte, out int channels,
            out int sampleRate, out int numRawBytes, out int startSilenceSamples);
        var floats = AudioDataHelper.ConvertToFloats(shortsMixed_byte, channels, out _, out byte[][] shorts_byte);
        InitializeValues(sampleRate, channels, floats, shorts_byte, 16, startSilenceSamples);
    }

    private void InitializeValues(int sampleRate, int channels, float[][] pcmFloats, byte[][] pcmBytes, int bitsPerSample, int startSilenceSamples)
    {
        this.sampleRate = sampleRate;
        this.channels = channels;
        this.pcmFloats = pcmFloats;
        this.pcmBytes = pcmBytes;
        this.bitsPerSample = bitsPerSample;
        this.startSilenceSamples = startSilenceSamples;
    }

    #endregion
    #region Properties
    /// <summary>Gets the number of samples per second.</summary>
    public int SampleRate => this.sampleRate;
    /// <summary>Gets the number of bits per sample. Allowed values are 8 or 16.</summary>
    public int BitsPerSample => this.bitsPerSample;
    /// <summary>Gets the PCM sound data per channel represented as signed 16-bit (short) or unsigned 8-bit int (uint).
    /// That is, for 8 bits per sample, samples are unsigned from 0 to 255. For 16 bits per sample, samples are signed from -32768 to 32767 and in little endian byte order.</summary>
    public byte[][] PcmBytes
    {
        get
        {
            return pcmBytes;
        }
        protected set
        {
            if (value == pcmBytes)
                return;
            pcmBytes = value;
            channels = pcmBytes.Length;
        }
    }

    /// <summary>The PCM sound data per channel. Samples are floats between -1 and 1.</summary>
    public float[][] PcmFloats => pcmFloats;
    /// <summary>
    /// Number of channels
    /// </summary>
    public int Channels => channels;

    /// <summary>
    /// Sample index to be considered as origin due to added silence in start of file. Mainly relevant for mp3 files.
    /// <para>Some playback devices start their playback here, including Godot and osu.</para>
    /// </summary>
    public int PlaybackOriginSample => startSilenceSamples;
    #endregion

    /// <summary>
    /// Returns PCM data as a double array for spectrogram processing.
    /// </summary>
    public double[] GetPcmAsDoubles(double multiplier = 16_000)
    {
        if (pcmFloats[0].Length > 0)
        {
            return pcmFloats[0]
                .Select(x => (double)x * multiplier)
                .ToArray();
        }
        else
        {
            return PCMDataConverter.ConvertToDouble(pcmBytes[0])
                .Select(x => x * multiplier)
                .ToArray();
        }
    }

}