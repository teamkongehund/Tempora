using System.Linq;
using Godot;
using OsuTimer.Classes.Audio;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class Waveform : Node2D {
    #region Methods

    public override void _Draw() {
        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        int numberOfSamples = sampleEndIndex - sampleIndexStart;
        float samplesPerPoint = numberOfSamples / Length / PointsPerPixel;

        int nbPoints = (int)Length * PointsPerPixel;
        var points = new Vector2[nbPoints];

        //float[] xValues = VectorTools.CreateLinearSpace(0, Length, (int)Length * DataPointsPerPixel);
        //float[] yValues = new float[xValues.Length];

        if (AudioFile == null) {
            Gd.Print("AudioFile was Null");
            return;
        }

        for (var pointIndex = 0; pointIndex < nbPoints; pointIndex++) {
            var sampleIndexBegin = (int)(sampleIndexStart + pointIndex * samplesPerPoint);
            var sampleIndexEnd = (int)(sampleIndexStart + (pointIndex + 1) * samplesPerPoint);

            float pickedValue;

            // Check if any sample value is negative - if so, the pickedvalue = 0
            // make sure not to clamp values when you get the AudioDataRange

            bool pointIsBeforeAudioData = sampleIndexBegin < 0 || sampleIndexEnd < 0;
            bool pointIsAfterAudioData = sampleIndexBegin > AudioFile.AudioData.Length
                                         || sampleIndexEnd > AudioFile.AudioData.Length;

            // TODO: don't render audio that doesn't exist
            if (pointIsBeforeAudioData || pointIsAfterAudioData) {
                pickedValue = 0;
            }
            else if (sampleIndexEnd - sampleIndexBegin == 0) {
                pickedValue = AudioFile.AudioData[sampleIndexBegin];
            }
            else {
                float max = AudioFile.AudioData[sampleIndexBegin..sampleIndexEnd].Max();
                float min = AudioFile.AudioData[sampleIndexBegin..sampleIndexEnd].Min();

                if (PointsPerPixel > 1)
                    pickedValue = pointIndex % 2 == 0 ? min : max; // Alternate to capture as much data as possible
                else
                    pickedValue = AudioFile.AudioData[sampleIndexBegin];

                //pickedValue = AudioFile.AudioData[sampleIndexBegin];
            }

            //yValues[pointIndex] = pickedValue * Height / 2;

            points[pointIndex] = new Vector2((float)pointIndex / PointsPerPixel, pickedValue * Height / 2);
        }

        var white = new Color(1f, 1f, 1f);

        //DrawLine(points[0], points[1], white, 1f, true);

        for (var i = 0; i < nbPoints - 1; i++) DrawLine(points[i], points[i + 1], white, 1f);
    }

    #endregion

    #region Properties

    private float length = 400;

    public float Length {
        get => length;
        set {
            length = value;
            QueueRedraw();
        }
    }

    private float height = 100;

    public float Height {
        get => height;
        set {
            height = value;
            QueueRedraw();
        }
    }

    private AudioFile audioFile;

    public AudioFile AudioFile {
        get => audioFile;
        set {
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

    private int[] audioDataRange = { 0, 0 };

    /// <summary>
    ///     Indices for the first and last audio sample to use from <see cref="OsuTimer.Classes.Audio.AudioFile.AudioData" />
    /// </summary>
    public int[] AudioDataRange {
        get {
            if (ShouldDisplayWholeFile)
                return new[] {
                    0,
                    AudioFile?.AudioData.Length ?? 0
                };

            int sampleStart = AudioFile?.SecondsToSampleIndex(TimeRange[0]) ?? 0;
            int sampleEnd = AudioFile?.SecondsToSampleIndex(TimeRange[1]) ?? 0;

            return new[] { sampleStart, sampleEnd };
        }
        set => audioDataRange = value;
    }

    private float[] timeRange;

    public float[] TimeRange {
        get => timeRange;
        set {
            if (value[0] > value[1]) { }

            timeRange = value;
            ShouldDisplayWholeFile = false;
            QueueRedraw();
        }
    }

    #endregion

    #region Initialization

    public Waveform(float length, float height) {
        Height = height;
        Length = length;
    }

    public Waveform(AudioFile audioFile) {
        AudioFile = audioFile;
        AudioDataRange = new[] { 0, AudioFile.AudioData.Length };
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height) {
        AudioFile = audioFile;
        AudioDataRange = new[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        QueueRedraw();
    }

    public Waveform(AudioFile audioFile, float length, float height, float[] timeRange) {
        AudioFile = audioFile;
        AudioDataRange = new[] { 0, AudioFile.AudioData.Length };
        Height = height;
        Length = length;
        TimeRange = timeRange;
        QueueRedraw();
    }

    #endregion
}