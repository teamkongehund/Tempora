using Godot;
using System;
using System.Linq;

public partial class Waveform : Node2D
{
    #region Properties

    private float _length = 400;
    public float Length
    {
        get => _length;
        set
        {
            _length = value;
            if (!IsInitializing) QueueRedraw();
        }
    }

    private float _height = 100;
    public float Height
    {
        get => _height;
        set
        {
            _height = value;
            if (!IsInitializing) QueueRedraw();
        }
    }

    private AudioFile _audioFile;
    public AudioFile AudioFile
    {
        get => _audioFile;
        set
        {
            _audioFile = value;
            if (!IsInitializing) QueueRedraw();
        }
    }

    /// <summary>
    /// Instead of putting one data point in the plot arrays, a larger number may show a better waveform.
    /// Even numbers should be used, as the plotter alternates between mix and max for each data point
    /// </summary>
    public int PointsPerPixel = 2;

    public bool ShouldDisplayWholeFile = true;

    private int[] _audioDataRange = new int[] { 0, 0 };
    /// <summary>
    /// Indices for the first and last audio sample to use from <see cref="AudioFile.AudioData"/>
    /// </summary>
    public int[] AudioDataRange
    {
        get
        {
            if (ShouldDisplayWholeFile)
            {
                return new int[]
                {
                    0,
                    AudioFile?.AudioData.Length ?? 0
                };
            }

            int sampleStart = AudioFile?.SecondsToSampleIndex(TimeRange[0]) ?? 0;
            int sampleEnd = AudioFile?.SecondsToSampleIndex(TimeRange[1]) ?? 0;

            return new int[] { sampleStart, sampleEnd };
        }
        set
        {
            _audioDataRange = value;
        }
    }

    private float[] _timeRange;
    public float[] TimeRange
    {
        get { return _timeRange; }
        set
        {
            if (value[0] > value[1])
            {

            }
            _timeRange = value;
            ShouldDisplayWholeFile = false;
            if (!IsInitializing) QueueRedraw();
        }
    }

    private bool IsInitializing = true;

    #endregion
    #region Initialization

    public Waveform(float length, float height)
    {
        Height = height;
        Length = length;
        IsInitializing = false;
    }

    public Waveform(AudioFile audioFile)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        IsInitializing = false;
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        IsInitializing = false;
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        TimeRange = timeRange;
        IsInitializing = false;
        QueueRedraw();
    }

    #endregion
    #region Methods

    /// </summary>
    public override void _Draw()
    {
        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        int numberOfSamples = sampleEndIndex - sampleIndexStart;
        float samplesPerPoint = ((float)numberOfSamples / Length / PointsPerPixel);

        int nbPoints = (int)Length * PointsPerPixel;
        Vector2[] points = new Vector2[nbPoints];

        //float[] xValues = VectorTools.CreateLinearSpace(0, Length, (int)Length * DataPointsPerPixel);
        //float[] yValues = new float[xValues.Length];

        if (AudioFile == null)
        {
            GD.Print("AudioFile was Null");
            return;
        }

        for (int pointIndex = 0; pointIndex < nbPoints; pointIndex++)
        {
            int sampleIndexBegin = (int)(sampleIndexStart + pointIndex * samplesPerPoint);
            int sampleIndexEnd = (int)(sampleIndexStart + (pointIndex + 1) * samplesPerPoint);

            float pickedValue = 0;

            // Check if any sample value is negative - if so, the pickedvalue = 0
            // make sure not to clamp values when you get the AudioDataRange

            bool pointIsBeforeAudioData = (sampleIndexBegin < 0 || sampleIndexEnd < 0);
            bool pointIsAfterAudioData = sampleIndexBegin > AudioFile.AudioData.Length
                || sampleIndexEnd > AudioFile.AudioData.Length;

            // TODO: don't render audio that doesn't exist
            if (pointIsBeforeAudioData || pointIsAfterAudioData)
                pickedValue = 0;
            else if (sampleIndexEnd - sampleIndexBegin == 0)
                pickedValue = AudioFile.AudioData[sampleIndexBegin];
            else
            {
                float max = AudioFile.AudioData[sampleIndexBegin..sampleIndexEnd].Max();
                float min = AudioFile.AudioData[sampleIndexBegin..sampleIndexEnd].Min();

                if (PointsPerPixel > 1)
                    pickedValue = (pointIndex % 2 == 0) ? min : max; // Alternate to capture as much data as possible
                else
                    pickedValue = AudioFile.AudioData[sampleIndexBegin];

                //pickedValue = AudioFile.AudioData[sampleIndexBegin];
            }

            //yValues[pointIndex] = pickedValue * Height / 2;

            points[pointIndex] = new Vector2(pointIndex / PointsPerPixel, pickedValue * Height / 2);
        }

        Color white = new Color(1f, 1f, 1f);

        //DrawLine(points[0], points[1], white, 1f, true);

        for (int i = 0; i < nbPoints - 1; i++)
        {
            DrawLine(points[i], points[i + 1], white, 1f, false);
        }
    }
    #endregion
}
