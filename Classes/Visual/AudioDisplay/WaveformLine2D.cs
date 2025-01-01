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

using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.Visual;

/// <summary>
///     Waveform visual representation of audio segment with constant time-distance relation.
/// </summary>
public partial class WaveformLine2D : Line2D
{
    #region Properties

    private float length = 400;

    public float Length
    {
        get => length;
        set
        {
            length = value;
            if (!isInitializing)
                PlotWaveform();
        }
    }

    private float height = 100;

    public float Height
    {
        get => height;
        set
        {
            height = value;
            if (!isInitializing)
                PlotWaveform();
        }
    }

    private AudioFile audioFile = null!;

    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            audioFile = value;
            if (!isInitializing)
                PlotWaveform();
        }
    }

    /// <summary>
    ///     Instead of putting one data point in the plot arrays, a larger number may show a better waveform.
    ///     Even numbers should be used, as the plotter alternates between mix and max for each data point
    /// </summary>
    public int DataPointsPerPixel = 4;

    public bool ShouldDisplayWholeFile = true;

    private int[] audioDataRange = [0, 0];

    /// <summary>
    ///     Indices for the first and last audio sample to use from <see cref="Audio.AudioFile.PcmLeft" />
    /// </summary>
    public int[] AudioDataRange
    {
        get
        {
            if (ShouldDisplayWholeFile)
            {
                return [
                    0,
                    AudioFile?.PcmLeft.Length ?? 0
                ];
            }

            int sampleStart = AudioFile?.SampleTimeToSampleIndex(TimeRange[0]) ?? 0;
            int sampleEnd = AudioFile?.SampleTimeToSampleIndex(TimeRange[1]) ?? 0;

            return [sampleStart, sampleEnd];
        }
        set => audioDataRange = value;
    }

    private float[] timeRange = null!;

    public float[] TimeRange
    {
        get => timeRange;
        set
        {
            if (value[0] > value[1])
            {
            }

            timeRange = value;
            ShouldDisplayWholeFile = false;
            if (!isInitializing)
                PlotWaveform();
        }
    }

    private bool isInitializing = true;

    #endregion

    #region Initialization

    public WaveformLine2D(float length, float height)
    {
        Height = height;
        Length = length;
        isInitializing = false;
    }

    public WaveformLine2D(AudioFile audioFile)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        isInitializing = false;
        PlotWaveform();
    }

    public WaveformLine2D(AudioFile audioFile, float length, float height)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        Height = height;
        Length = length;
        isInitializing = false;
        PlotWaveform();
    }

    public WaveformLine2D(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        Height = height;
        Length = length;
        TimeRange = timeRange;
        isInitializing = false;
        PlotWaveform();
    }

    #endregion

    #region Methods

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Width = 1;

    /// <summary>
    ///     Generate <see cref="Line2D.Points" />. This property belongs to a Godot default class, so the setter can't be
    ///     modified.
    ///     So, unless another programming pattern can auto-update <see cref="Line2D.Points" />, <see cref="PlotWaveform" />
    ///     must be manually called
    ///     whenever something causes the waveform to change.
    /// </summary>
    private void PlotWaveform()
    {
        int sampleStart = AudioDataRange[0];
        int sampleEnd = AudioDataRange[1];

        int numberOfSamples = sampleEnd - sampleStart;
        float samplesPerDataPoint = numberOfSamples / Length / DataPointsPerPixel;

        float[] xValues = VectorTools.CreateLinearSpace(0, Length, (int)Length * DataPointsPerPixel);
        float[] yValues = new float[xValues.Length];

        if (AudioFile == null)
        {
            GD.Print("AudioFile was Null");
            return;
        }

        for (int dataPointIndex = 0; dataPointIndex < yValues.Length; dataPointIndex++)
        {
            int sampleAtDataPointStart = (int)(sampleStart + (dataPointIndex * samplesPerDataPoint));
            int sampleAtDataPointEnd = (int)(sampleStart + ((dataPointIndex + 1) * samplesPerDataPoint));

            // Check if any sample value is negative - if so, the pickedvalue = 0
            // make sure not to clamp values when you get the AudioDataRange

            bool dataPointIsBeforeAudio = sampleAtDataPointStart < 0 || sampleAtDataPointEnd < 0;
            bool dataPointIsAfterAudio = sampleAtDataPointStart > AudioFile.PcmLeft.Length
                                         || sampleAtDataPointEnd > AudioFile.PcmLeft.Length;

            float pickedValue;
            if (dataPointIsBeforeAudio || dataPointIsAfterAudio)
            {
                pickedValue = 0;
            }
            else if (sampleAtDataPointEnd - sampleAtDataPointStart == 0)
            {
                pickedValue = AudioFile.PcmLeft[sampleAtDataPointStart];
            }
            else
            {
                float max = AudioFile.PcmLeft[sampleAtDataPointStart..sampleAtDataPointEnd].Max();
                float min = AudioFile.PcmLeft[sampleAtDataPointStart..sampleAtDataPointEnd].Min();

                if (DataPointsPerPixel > 1)
                    pickedValue = dataPointIndex % 2 == 0 ? min : max; // Alternate to capture as much data as possible
                else
                    pickedValue = AudioFile.PcmLeft[sampleAtDataPointStart];

                //pickedValue = AudioFile.AudioData[sampleAtDataPointStart];
            }

            yValues[dataPointIndex] = pickedValue * Height / 2;
        }

        Points = VectorTools.CombineArraysToVector2(xValues, yValues);
    }

    #endregion
}