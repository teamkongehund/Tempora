using Godot;
using System;
using System.Linq;

/// <summary>
/// Waveform visual representation of audio segment with constant time-distance relation.
/// </summary>
public partial class Waveform : Line2D
{
    private float _length = 400;
    public float Length
    {
        get => _length;
        set
        {
            _length = value;
            UpdatePoints();
        }
    }

    private float _height = 100;
    public float Height
    {
        get => _height; 
        set
        {
            _height = value;
            UpdatePoints();
        }
    }

    public float PixelsPerSecond
    {
        get
        {
            int totalSamples = AudioDataRange[1] - AudioDataRange[0];
            float totalSeconds = totalSamples / AudioFile.SampleRate;
            return Length / totalSeconds * 2;
        }
    }

    private AudioFile _audioFile;
    public AudioFile AudioFile
    {
        get => _audioFile;
        set
        {
            _audioFile = value;
            UpdatePoints();
        }
    }

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
            _timeRange = value;
            ShouldDisplayWholeFile = false;
            UpdatePoints();
        }
    }

    public Waveform(float length, float height)
    {
        Height = height;
        Length = length;
    }

    public Waveform(AudioFile audioFile)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        UpdatePoints();
    }

    public Waveform(AudioFile audioFile, float length, float height)
    {
        AudioFile = audioFile;
        AudioDataRange = new int[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        UpdatePoints();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        //if (AudioDataRange[1] == 0) AudioDataRange[1] = AudioFile.AudioData.Length;
        Width = 1;
	}

    /// <summary>
    /// Generate <see cref="Line2D.Points"/>. This property belongs to a Godot default class, so the setter can't be modified.
    /// So, unless another programming pattern can auto-update <see cref="Line2D.Points"/>, <see cref="UpdatePoints"/> must be manually called
    /// whenever something causes the waveform to change.
    /// </summary>
    private void UpdatePoints()
    {
        int sampleStart = AudioDataRange[0];
        int sampleEnd = AudioDataRange[1];

        int numberOfSamples = sampleEnd - sampleStart;
        int samplesPerPixel = (numberOfSamples / (int)Length);

        float[] xValues = VectorTools.CreateLinearSpace(0, Length, (int)Length);
        float[] yValues = new float[xValues.Length];

        if (AudioFile == null)
        {
            GD.Print("AudioFile was Null");
            return;
        }

        for (int i = 0; i < yValues.Length; i++)
        {
            int sampleAtPixelStart = sampleStart + i * samplesPerPixel;
            int sampleAtPixelEnd = sampleStart + (i+1) * samplesPerPixel;

            // FIXME: the mean is not an appropriate value to use, as it doesn't capture peaks
            float mean = AudioFile.AudioData[sampleAtPixelStart..sampleAtPixelEnd].Average();

            //yValues[i] = AudioFile.AudioData[sampleStart + i * samplesPerPixel] * Height / 2;
            yValues[i] = mean * Height / 2;
        }
        
        Points = VectorTools.CombineArraysToVector2(xValues, yValues);
    }

    public float PlaybackTimeToPixelPosition(float playbackTime)
    {
        // Warning: Unless fixed, there may be a discrepancy if the waveform goes before the audio file beginning,
        // or goes after the audio file end. 

        float pixelPosition = (playbackTime - TimeRange[0]) * PixelsPerSecond ;

        return pixelPosition;
    }

    public float PixelPositionToPlaybackTime(float pixelPosition)
    {
        float playbackTime = pixelPosition / PixelsPerSecond + TimeRange[0];

        return playbackTime;
    }
}
