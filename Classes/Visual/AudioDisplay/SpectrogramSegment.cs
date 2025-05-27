using System.Collections.Generic;
using System.Security.AccessControl;
using Godot;
using Spectrogram;
using Tempora.Classes.Audio;
using Tempora.Classes.DataHelpers;

namespace Tempora.Classes.Visual.AudioDisplay;
public partial class SpectrogramSegment : Sprite2D, IAudioSegmentDisplay
{
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

    private Colormap defaultColormap = new Colormap(new CustomColormap(new List<Godot.Color> { GlobalConstants.TemporaBlue, new("ffffff") }));

    public float Width { get; set; } = 400;

    public float Height { get; set; } = 100;

    public AudioFile AudioFile { get; set; } = null!;

    public Color Color { get => color; set => color = value; }

    public static readonly Color DefaultColor = GlobalConstants.AudioFullExposure;
    public static readonly Color DarkenedColor = GlobalConstants.AudioDarkened;
    private Color color = DefaultColor;

    private SpectrogramContext spectrogramContext;

    public override void _Ready()
    {
        Render();
    }

    public SpectrogramSegment(AudioFile audioFile, SpectrogramContext spectrogramContext, float length, float height, float[] timeRange)
    {
        this.spectrogramContext = spectrogramContext;
        InstantiateAndRender(audioFile, spectrogramContext, length, height, timeRange);
    }

    /// <summary>
    /// Useful when the object is already in the scene tree and needs to be re-instantiated with new data.
    /// </summary>
    public void InstantiateAndRender(AudioFile audioFile, SpectrogramContext spectrogramContext, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        this.spectrogramContext = spectrogramContext;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        Height = height;
        Width = length;
        TimeRange = timeRange;
        Render();
    }

    public void Render()
    {
        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        Texture = spectrogramContext.GetSpectrogramSlice(sampleIndexStart, sampleEndIndex, (int)Height, (int)Width);

        var textureSize = Texture.GetSize();
    }
}
