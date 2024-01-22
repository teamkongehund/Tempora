using Godot;
using Tempora.Classes.Audio;
using GD = Tempora.Classes.Utility.GD;

namespace Tempora.Classes.Visual;

public partial class Waveform : Node2D
{
    #region Methods

    public override void _Draw()
    {
        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        int numberOfSamples = sampleEndIndex - sampleIndexStart;
        float samplesPerPoint = numberOfSamples / Length / PointsPerPixel;

        int nbPoints = (int)Length * PointsPerPixel;
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

            float pickedValue;

            pickedValue = GetPickedValue(sampleIndexBegin, sampleIndexEnd, pointIndex);

            var coordinate = new Vector2((float)pointIndex / PointsPerPixel, pickedValue * Height / 2);

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

        var white = new Color(1f, 1f, 1f);

        DrawMultiline(multilinePoints, white, 1f);
        //DrawPolyline(multilinePoints, white, 1f);

        // Testing saving waveform result as texture or image to use elsewhere
        //Image waveImage = GetViewport().GetTexture().GetImage();
        //waveImage.SavePng("user://renderedWave.png");
    }

    private float GetPickedValue(int sampleIndexBegin, int sampleIndexEnd, int pointIndex)
    {
        float pickedValue;

        bool pointIsBeforeAudioData = sampleIndexBegin < 0 || sampleIndexEnd < 0;
        bool pointIsAfterAudioData = sampleIndexBegin > AudioFile.AudioData.Length
                                     || sampleIndexEnd > AudioFile.AudioData.Length;

        if (pointIsBeforeAudioData || pointIsAfterAudioData)
        {
            pickedValue = 0;
        }
        else if (sampleIndexEnd - sampleIndexBegin == 0)
        {
            pickedValue = AudioFile.AudioData[sampleIndexBegin];
        }
        else
        {
            float min;
            float max;
            if (sampleIndexEnd - sampleIndexBegin <= 10)
            {
                min = EfficientMin(AudioFile.AudioData, sampleIndexBegin, sampleIndexEnd);
                max = EfficientMax(AudioFile.AudioData, sampleIndexBegin, sampleIndexEnd);
            }
            else
            {
                min = EfficientMin(audioFile.AudioDataPer10Min, sampleIndexBegin / 10, sampleIndexEnd / 10);
                max = EfficientMax(audioFile.AudioDataPer10Max, sampleIndexBegin / 10, sampleIndexEnd / 10);
            }

            if (PointsPerPixel > 1)
                pickedValue = pointIndex % 2 == 0 ? min : max; // Alternate to capture as much data as possible
            else
                pickedValue = AudioFile.AudioData[sampleIndexBegin];
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

    private float length = 400;

    public float Length
    {
        get => length;
        set
        {
            length = value;
            QueueRedraw();
        }
    }

    private float height = 100;

    public float Height
    {
        get => height;
        set
        {
            height = value;
            QueueRedraw();
        }
    }

    private AudioFile audioFile = null!;

    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            audioFile = value;
            QueueRedraw();
        }
    }

    /// <summary>
    ///     Instead of putting one data point in the plot arrays, a larger number may show a better waveform.
    ///     Even numbers should be used, as the plotter alternates between mix and max for each data point
    /// </summary>
    public int PointsPerPixel = 2;

    public bool ShouldDisplayWholeFile = true;

    private int[] audioDataRange = [0, 0];

    /// <summary>
    ///     Indices for the first and last audio sample to use from <see cref="Tempora.Classes.Audio.AudioFile.AudioData" />
    /// </summary>
    public int[] AudioDataRange
    {
        get
        {
            if (ShouldDisplayWholeFile)
            {
                return [
                    0,
                    AudioFile?.AudioData.Length ?? 0
                ];
            }

            int sampleStart = AudioFile?.SecondsToSampleIndex(TimeRange[0]) ?? 0;
            int sampleEnd = AudioFile?.SecondsToSampleIndex(TimeRange[1]) ?? 0;

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
            QueueRedraw();
        }
    }

    #endregion

    #region Initialization

    public Waveform(float length, float height)
    {
        Height = height;
        Length = length;
    }

    public Waveform(AudioFile audioFile)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.AudioData.Length];
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.AudioData.Length];
        Height = height;
        Length = length;
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.AudioData.Length];
        Height = height;
        Length = length;
        TimeRange = timeRange;
        QueueRedraw();
    }

    #endregion
}