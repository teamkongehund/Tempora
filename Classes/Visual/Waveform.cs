using Godot;
using System;
using System.Linq;

/// <summary>
/// Waveform visual representation of audio segment with constant time-distance relation.
/// </summary>
public partial class Waveform : Line2D
{
    #region Properties

    private float _length = 400;
    public float Length
    {
        get => _length;
        set
        {
            _length = value;
            if (!IsInitializing) PlotWaveform();
        }
    }

    private float _height = 100;
    public float Height
    {
        get => _height; 
        set
        {
            _height = value;
            if (!IsInitializing) PlotWaveform();
        }
    }

    //public float PixelsPerSecond
    //{
    //    get
    //    {
    //        int totalSamples = AudioDataRange[1] - AudioDataRange[0];
    //        float totalSeconds = (float)totalSamples / (float)AudioFile.SampleRate;
    //        return Length / totalSeconds * 2f;
    //    }
    //}

    private AudioFile _audioFile;
    public AudioFile AudioFile
    {
        get => _audioFile;
        set
        {
            _audioFile = value;
            if (!IsInitializing) PlotWaveform();
        }
    }

    /// <summary>
    /// Instead of putting one data point in the plot arrays, a larger number may show a better waveform.
    /// Even numbers should be used, as the plotter alternates between mix and max for each data point
    /// </summary>
    public int DataPointsPerPixel = 6;

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

            //GD.Print($"Maximum samples: {AudioFile?.AudioData.Length}");
            //GD.Print($"sampleStart and sampleEnd = {sampleStart} , {sampleEnd}");

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
            if (!IsInitializing) PlotWaveform();
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
        PlotWaveform();
    }

    public Waveform(AudioFile audioFile, float length, float height)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        IsInitializing = false;
        PlotWaveform();
    }

    public Waveform(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        TimeRange = timeRange;
        IsInitializing = false;
        PlotWaveform();
    }

    #endregion
    #region Methods
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Width = 1;
	}

    /// <summary>
    /// Generate <see cref="Line2D.Points"/>. This property belongs to a Godot default class, so the setter can't be modified.
    /// So, unless another programming pattern can auto-update <see cref="Line2D.Points"/>, <see cref="PlotWaveform"/> must be manually called
    /// whenever something causes the waveform to change.
    /// </summary>
    private void PlotWaveform()
    {
        int sampleStart = AudioDataRange[0];
        int sampleEnd = AudioDataRange[1];

        int numberOfSamples = sampleEnd - sampleStart;
        float samplesPerDataPoint = ((float)numberOfSamples / Length / DataPointsPerPixel);

        float[] xValues = VectorTools.CreateLinearSpace(0, Length, (int)Length * DataPointsPerPixel);
        float[] yValues = new float[xValues.Length];

        if (AudioFile == null)
        {
            GD.Print("AudioFile was Null");
            return;
        }

        for (int dataPointIndex = 0; dataPointIndex < yValues.Length; dataPointIndex++)
        {
            int sampleAtDataPointStart = (int)(sampleStart + dataPointIndex * samplesPerDataPoint);
            int sampleAtDataPointEnd = (int)(sampleStart + (dataPointIndex+1) * samplesPerDataPoint);

            float pickedValue = 0;

            // Check if any sample value is negative - if so, the pickedvalue = 0
            // make sure not to clamp values when you get the AudioDataRange

            bool dataPointIsBeforeAudio = (sampleAtDataPointStart < 0 || sampleAtDataPointEnd < 0);
            bool dataPointIsAfterAudio = sampleAtDataPointStart > AudioFile.AudioData.Length 
                || sampleAtDataPointEnd > AudioFile.AudioData.Length;

            if (dataPointIsBeforeAudio || dataPointIsAfterAudio) 
                pickedValue = 0;
            else if (sampleAtDataPointEnd - sampleAtDataPointStart == 0) 
                pickedValue = AudioFile.AudioData[sampleAtDataPointStart];
            else
            {
                //float max = AudioFile.AudioData[sampleAtDataPointStart..sampleAtDataPointEnd].Max();
                //float min = AudioFile.AudioData[sampleAtDataPointStart..sampleAtDataPointEnd].Min();

                //if (DataPointsPerPixel > 1)
                //    pickedValue = (dataPointIndex % 2 == 0) ? min : max; // Alternate to capture as much data as possible
                //else
                //    pickedValue = AudioFile.AudioData[sampleAtDataPointStart];

                pickedValue = AudioFile.AudioData[sampleAtDataPointStart];
            }

            yValues[dataPointIndex] = pickedValue * Height / 2;

        }
        
        // This is the toughest job in the Waveform creation process after getting rid of min/max rendering
        Points = VectorTools.CombineArraysToVector2(xValues, yValues); 
    }
    #endregion
}
