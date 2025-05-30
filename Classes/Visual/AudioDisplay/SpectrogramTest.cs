using Godot;
using Spectrogram;
using Tempora.Classes.DataHelpers;
using System.Collections.Generic;
using Tempora.Classes.Audio;
using System.Drawing.Imaging;
using System.Drawing;
using Tempora.Classes.Utility;


namespace Tempora.Classes.Visual.AudioDisplay;
public partial class SpectrogramTest : Node2D
{
    // See https://github.com/swharden/Spectrogram

    [Export]
    AudioStreamMP3 audioStreamMP3 = null!;

    [Export]
    Sprite2D spectrogramSprite2D = null!;

	//// Called when the node enters the scene tree for the first time.
	//public override void _Ready()
	//{
 //       (double[] audio, int sampleRate) = ReadMono(ProjectSettings.GlobalizePath(audioStreamMP3.ResourcePath));

 //       int fftSize = 16384;
 //       int targetWidthPx = 3000;
 //       int stepSize = audio.Length / targetWidthPx;

 //       var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, maxFreq: 2200);
 //       sg.Add(audio);
 //       sg.Colormap = new Colormap(new CustomColormap(new List<Color> { GlobalConstants.TemporaBlue, new("ffffff") }));
 //       sg.SaveImage("song.png", intensity: 5, dB: true);
 //   }

    public override void _Ready()
    {
        //TestSpectrogramGeneration();

        Project.Instance.AudioFile = new AudioFile(audioStreamMP3);
        var testTexture = Project.Instance.SpectrogramContext.GetSpectrogramSlice(0, 44100*5, 400, 400);
        spectrogramSprite2D.Texture = testTexture;
        spectrogramSprite2D.Position = new Vector2(GetWindow().Size.X / 2, GetWindow().Size.Y / 2);
    }

    private void TestSpectrogramGeneration()
    {
        int targetWidthPx = GetWindow().Size.X;

        string audioPath = ProjectSettings.GlobalizePath(audioStreamMP3.ResourcePath);
        PcmData pcmData = new PcmData(audioPath);
        double[] audio = pcmData.GetPcmAsDoubles(16_000);
        int sampleRate = pcmData.SampleRate;

        var sg = SpectrogramHelper.GetSpectrogramGenerator_ByWidth(pcmData, targetWidthPx);
        //sg.SaveImage("song.png", intensity: 5, dB: true);

        var testTexture = SpectrogramHelper.GenerateTexture(sg, SpectrogramHelper.TemporaColormap, intensity: 5, dB: true);
        ShowTexture(testTexture);
    }


    /// <summary>
    /// Show a texture in this node's Sprite2D.
    /// </summary>
    /// <param name="texture"></param>
    public void ShowTexture(ImageTexture texture)
    {
        spectrogramSprite2D.Texture = texture;

        var textureSize = spectrogramSprite2D.Texture.GetSize();
        Vector2 scale = new Vector2(1f * GetWindow().Size.X / textureSize.X, 1f * GetWindow().Size.Y / textureSize.Y);
        spectrogramSprite2D.GlobalScale = scale;

        var trueSize = new Vector2(scale.X * textureSize.X, scale.Y * textureSize.Y);
        spectrogramSprite2D.Position = new Vector2(trueSize.X / 2, trueSize.Y / 2);
    }
}
