using Godot;
using System;
using Spectrogram;
using Tempora.Classes.DataHelpers;
using System.Collections.Generic;
using System.Linq;
using Tempora.Classes.Utility;
using Tempora.Classes.Audio;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

    public Colormap defaultColormap =  new Colormap(new CustomColormap(new List<Godot.Color> { GlobalConstants.TemporaBlue, new ("ffffff") }));

    public override void _Ready()
    {
        string audioPath = ProjectSettings.GlobalizePath(audioStreamMP3.ResourcePath);

        PcmData pcmData = new PcmData(audioPath);
        double[] audio = pcmData.GetPcmAsDoubles(16_000);
        int sampleRate = pcmData.SampleRate;

        int fftSize = 16384;
        int targetWidthPx = GetWindow().Size.X;
        int stepSize = audio.Length / targetWidthPx;

        var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, maxFreq: 2200);
        sg.Add(audio);
        sg.Colormap = defaultColormap;
        //sg.SaveImage("song.png", intensity: 5, dB: true);
        var ffts = sg.GetFFTs();
        var bitmap = Spectrogram.Image.GetBitmap(ffts, defaultColormap, intensity: 5, dB: true);
        spectrogramSprite2D.Texture = ConvertBitmapToImageTextureNew(bitmap);
        spectrogramSprite2D.Position = new Vector2(spectrogramSprite2D.Texture.GetWidth() / 2, spectrogramSprite2D.Texture.GetHeight() / 2);
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
