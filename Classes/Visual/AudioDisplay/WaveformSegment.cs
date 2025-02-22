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

using System.Collections.Generic;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Visual.AudioDisplay;
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.Visual.AudioDisplay;

public partial class WaveformSegment : Node2D, IAudioSegmentDisplay
{
    #region Methods

    public override void _Draw()
    {
        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        int numberOfSamples = sampleEndIndex - sampleIndexStart;
        float samplesPerPoint = numberOfSamples / Width / PointsPerLengthwisePixel;

        int nbPoints = (int)Width * PointsPerLengthwisePixel;
        if (nbPoints <= 0)
            return;

        var points = new Vector2[nbPoints];

        var multilinePoints = new Vector2[(nbPoints * 2) - 2];

        if (AudioFile == null)
        {
            GD.Print("AudioFile was Null");
            return;
        }

        for (int pointIndex = 0; pointIndex < nbPoints; pointIndex++)
        {
            int sampleIndexBegin = (int)(sampleIndexStart + (pointIndex * samplesPerPoint));
            int sampleIndexEnd = (int)(sampleIndexStart + ((pointIndex + 1) * samplesPerPoint));

            float pickedFloatSample;

            pickedFloatSample = GetPickedFloatSample(sampleIndexBegin, sampleIndexEnd, pointIndex);

            var coordinate = new Vector2((float)pointIndex / PointsPerLengthwisePixel, -pickedFloatSample * Height / 2);

            points[pointIndex] = coordinate;

            if (pointIndex == 0)
            {
                multilinePoints[pointIndex] = coordinate;
            }
            else if (pointIndex == nbPoints - 1)
            {
                multilinePoints[(pointIndex * 2) - 1] = coordinate;
            }
            else
            {
                multilinePoints[(pointIndex * 2) - 1] = coordinate;
                multilinePoints[pointIndex * 2] = coordinate;
            }
        }

        DrawMultiline(multilinePoints, Color, 1f);
        //DrawPolyline(multilinePoints, white, 1f);

        // Testing saving waveform result as texture or image to use elsewhere
        //Image waveImage = GetViewport().GetTexture().GetImage();
        //waveImage.SavePng("user://renderedWave.png");
    }

    private float GetPickedFloatSample(int sampleIndexBegin, int sampleIndexEnd, int pointIndex)
    {
        float pickedValue;

        bool pointIsBeforeAudioData = sampleIndexBegin < 0 || sampleIndexEnd < 0;
        bool pointIsAfterAudioData = sampleIndexBegin > AudioFile.PcmLeft.Length
                                     || sampleIndexEnd > AudioFile.PcmLeft.Length;

        if (pointIsBeforeAudioData || pointIsAfterAudioData)
        {
            throw new System.Exception("Attempted to retrieve a non-existent audio sample.");
        }
        else if (sampleIndexEnd - sampleIndexBegin == 0)
        {
            pickedValue = AudioFile.PcmLeft[sampleIndexBegin];
        }
        else
        {
            float min;
            float max;
            if (sampleIndexEnd - sampleIndexBegin <= 10)
            {
                min = EfficientMin(AudioFile.PcmLeft, sampleIndexBegin, sampleIndexEnd);
                max = EfficientMax(AudioFile.PcmLeft, sampleIndexBegin, sampleIndexEnd);
            }
            else
            {
                min = EfficientMin(AudioFile.AudioDataPer10Min, sampleIndexBegin / 10, sampleIndexEnd / 10);
                max = EfficientMax(AudioFile.AudioDataPer10Max, sampleIndexBegin / 10, sampleIndexEnd / 10);
            }

            if (PointsPerLengthwisePixel > 1)
                pickedValue = pointIndex % 2 == 0 ? min : max; // Alternate to capture as much data as possible
            else
                pickedValue = AudioFile.PcmLeft[sampleIndexBegin];
        }

        return pickedValue;
    }

    /// <summary>
    /// A simple way to get min and max value that does not use Linq or subarrays. This is an attempt at increasing performance.
    /// </summary>
    /// <param name="dataArray"></param>
    /// <param name="sampleBegin"></param>
    /// <param name="sampleEnd"></param>
    /// <returns></returns>
    private static float EfficientMin(float[] dataArray, int sampleBegin, int sampleEnd)
    {
        float min = dataArray[sampleBegin];
        for (int i = sampleBegin + 1; i < sampleEnd; i++)
        {
            float newValue = dataArray[i];
            if (newValue < min)
                min = newValue;
        }
        return min;
    }
    private static float EfficientMax(float[] dataArray, int sampleBegin, int sampleEnd)
    {
        float max = dataArray[sampleBegin];
        for (int i = sampleBegin + 1; i < sampleEnd; i++)
        {
            float newValue = dataArray[i];
            if (newValue > max)
                max = newValue;
        }
        return max;
    }

    #endregion

    #region Properties


    public float Width { get; set; } = 400;

    public float Height { get; set; } = 100;

    public AudioFile AudioFile { get; set; } = null!;

    /// <summary>
    ///     Instead of putting one data point in the plot arrays, a larger number may show a better waveform.
    ///     Even numbers should be used, as the plotter alternates between mix and max for each data point
    /// </summary>
    private int PointsPerLengthwisePixel = 2;

    private int[] audioDataRange = [0, 0];

    /// <summary>
    ///     Indices for the first and last audio sample to use from <see cref="Tempora.Classes.Audio.AudioFile.PcmLeft" />
    /// </summary>
    private int[] AudioDataRange
    {
        get
        {
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
        }
    }

    public Color Color { get => color; set => color = value; }

    public static readonly Color DefaultColor = GlobalConstants.AudioFullExposure;
    public static readonly Color DarkenedColor = GlobalConstants.AudioDarkened;
    private Color color = DefaultColor;
    #endregion

    #region Initialization

    public WaveformSegment(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        Height = height;
        Width = length;
        TimeRange = timeRange;
        Render();
    }

    public void Render() => QueueRedraw();

    #endregion
}