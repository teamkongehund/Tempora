using Godot;
using System;
using Spectrogram;
using Tempora.Classes.DataHelpers;
using System.Collections.Generic;
using System.Linq;
using Tempora.Classes.Utility;
using Tempora.Classes.Audio;
using Tempora.Classes.DataHelpers;


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

    public static Colormap defaultColormap = new Colormap(new CustomColormap(new List<Godot.Color> { GlobalConstants.TemporaBlue, new ("ffffff") }));

    public override void _Ready()
    {
        int targetWidthPx = GetWindow().Size.X;

        string audioPath = ProjectSettings.GlobalizePath(audioStreamMP3.ResourcePath);
        PcmData pcmData = new PcmData(audioPath);
        double[] audio = pcmData.GetPcmAsDoubles(16_000);
        int sampleRate = pcmData.SampleRate;

        var sg = SpectrogramHelper.GetSpectrogramGenerator_ByWidth(pcmData, targetWidthPx);
        //sg.SaveImage("song.png", intensity: 5, dB: true);

        var testTexture = SpectrogramHelper.GenerateTexture(sg, defaultColormap, intensity: 5, dB: true);
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

    public static Godot.Image ConvertBitmapToGodotImage(System.Drawing.Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Lock the bitmap's data
        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb); // Ensure 32-bit ARGB format

        // Prepare buffer
        int bufferSize = bmpData.Stride * bmpData.Height;
        byte[] buffer = new byte[bufferSize];

        // Copy bitmap data to buffer
        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, buffer, 0, bufferSize);

        // Unlock bitmap
        bitmap.UnlockBits(bmpData);

        // Convert ARGB to RGBA (Godot uses RGBA order)
        for (int i = 0; i < bufferSize; i += 4)
        {
            byte a = buffer[i + 3];  // Alpha
            byte r = buffer[i + 2];  // Red
            byte g = buffer[i + 1];  // Green
            byte b = buffer[i];      // Blue

            buffer[i] = r;
            buffer[i + 1] = g;
            buffer[i + 2] = b;
            buffer[i + 3] = a;
        }

        // Create a Godot Image and populate it with the data
        Godot.Image gdImage = Godot.Image.CreateFromData(width, height, false, Godot.Image.Format.Rgba8, buffer);

        return gdImage;
    }

    public static ImageTexture ConvertBitmapToImageTextureNew(System.Drawing.Bitmap bitmap)
    {
        Godot.Image gdImage = ConvertBitmapToGodotImage(bitmap);
        ImageTexture texture = ImageTexture.CreateFromImage(gdImage);
        return texture;
    }

    //(double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 16_000)
    //{
    //    using var afr = new NAudio.Wave.AudioFileReader(filePath);
    //    int sampleRate = afr.WaveFormat.SampleRate;
    //    int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
    //    int sampleCount = (int)(afr.Length / bytesPerSample);
    //    int channelCount = afr.WaveFormat.Channels;
    //    var audio = new List<double>(sampleCount);
    //    var buffer = new float[sampleRate * channelCount];
    //    int samplesRead = 0;
    //    while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
    //        audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
    //    return (audio.ToArray(), sampleRate);
    //}
}
