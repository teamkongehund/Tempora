using System.Collections.Generic;
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

    //private int fftSize = 16384;
    private int fftSize = 256;

    private int maxFreq = 5000;

    public override void _Ready()
    {
        Render();
    }

    public SpectrogramSegment(AudioFile audioFile, float length, float height, float[] timeRange)
    {
        AudioFile = audioFile;
        AudioDataRange = [0, AudioFile.PcmLeft.Length];
        Height = height;
        Width = length;
        TimeRange = timeRange;
        Render();
    }

    public void Render()
    {
        //// This is the old code that was used to generate the spectrogram.

        //int sampleIndexStart = AudioDataRange[0];
        //int sampleEndIndex = AudioDataRange[1];

        //double[] full_audio = AudioFile.GetPcmAsDoubles(16_000);
        //double[] audio = full_audio[sampleIndexStart..sampleEndIndex];

        //int stepSize = audio.Length / (int)Width;

        //var sg = new SpectrogramGenerator(AudioFile.SampleRate, fftSize, stepSize, maxFreq: maxFreq);
        //sg.Add(audio);
        //sg.Colormap = defaultColormap;
        ////sg.SaveImage("song.png", intensity: 5, dB: true);
        //var ffts = sg.GetFFTs();
        //try
        //{
        //    var bitmap = Spectrogram.Image.GetBitmap(ffts, defaultColormap, intensity: 5, dB: true);
        //    Texture = SpectrogramHelper.ConvertBitmapToImageTextureNew(bitmap);

        //    var textureSize = Texture.GetSize();
        //    Vector2 scale = new Vector2(1f * Width / textureSize.X, 1f * Height / textureSize.Y);
        //    GlobalScale = scale;

        //    var trueSize = new Vector2(scale.X * textureSize.X, scale.Y * textureSize.Y);
        //}
        //catch
        //{

        //}
        ////Position = new Vector2(trueSize.X / 2, trueSize.Y / 2);

        int sampleIndexStart = AudioDataRange[0];
        int sampleEndIndex = AudioDataRange[1];

        double[] full_audio = AudioFile.GetPcmAsDoubles(16_000);
        double[] audio = full_audio[sampleIndexStart..sampleEndIndex];

        int stepSize = audio.Length / (int)Width;

        var sg = new SpectrogramGenerator(AudioFile.SampleRate, fftSize, stepSize, maxFreq: maxFreq);
        sg.Add(audio);
        sg.Colormap = defaultColormap;
        //sg.SaveImage("song.png", intensity: 5, dB: true);
        var ffts = sg.GetFFTs();
        try
        {
            var bitmap = Spectrogram.Image.GetBitmap(ffts, defaultColormap, intensity: 5, dB: true);
            Texture = SpectrogramHelper.GetSpectrogramSlice(sg.GetImage(), 0, (int)Width, (int)Height);

            var textureSize = Texture.GetSize();
            Vector2 scale = new Vector2(1f * Width / textureSize.X, 1f * Height / textureSize.Y);
            GlobalScale = scale;

            var trueSize = new Vector2(scale.X * textureSize.X, scale.Y * textureSize.Y);
        }
        catch
        {
            
        }
        //Position = new Vector2(trueSize.X / 2, trueSize.Y / 2);
    }
}
