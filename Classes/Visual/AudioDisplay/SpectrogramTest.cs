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
        int targetWidthPx = 3000;
        int stepSize = audio.Length / targetWidthPx;

        var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, maxFreq: 2200);
        sg.Add(audio);
        sg.Colormap = defaultColormap;
        sg.SaveImage("song.png", intensity: 5, dB: true);
        var ffts = sg.GetFFTs();
        var bitmap = Spectrogram.Image.GetBitmap(ffts, defaultColormap, intensity: 5, dB: true);
        //var bitmap = sg.GetBitmap(intensity: 5, dB: true);

        //byte[] bitmapArray = ImageToByte2(bitmap);

        //var image = Godot.Image.CreateFromData(bitmap.Width, bitmap.Height, false, Godot.Image.Format.Rgba8, bitmapArray);
        //var imageTexture = ImageTexture.CreateFromImage(image);

        spectrogramSprite2D.Texture = ConvertBitmapToImageTexture(bitmap);
    }


    (double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 16_000)
    {
        using var afr = new NAudio.Wave.AudioFileReader(filePath);
        int sampleRate = afr.WaveFormat.SampleRate;
        int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
        int sampleCount = (int)(afr.Length / bytesPerSample);
        int channelCount = afr.WaveFormat.Channels;
        var audio = new List<double>(sampleCount);
        var buffer = new float[sampleRate * channelCount];
        int samplesRead = 0;
        while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
            audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
        return (audio.ToArray(), sampleRate);
    }

    public static byte[] ImageToByte2(System.Drawing.Image img)
    {
        using (var stream = new MemoryStream())
        {
            //img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            img.Save(stream, System.Drawing.Imaging.ImageFormat.MemoryBmp);
            return stream.ToArray();
        }
    }

    private ImageTexture ConvertBitmapToImageTexture(System.Drawing.Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Ensure the Bitmap format is 8bpp Indexed (Grayscale)
        if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
        {
            Godot.GD.PrintErr("Bitmap format is not 8bppIndexed. Converting to grayscale...");
            bitmap = ConvertTo8bppGrayscale(bitmap);
        }

        // Extract properly aligned pixel data (fix stride issues)
        byte[] pixelData = ExtractIndexedBitmapData(bitmap, out int stride);

        // Convert grayscale to RGB8 format (with correct stride handling)
        byte[] rgbData = ConvertGrayscaleToRGB(pixelData, width, height, stride);

        // Create Godot Image
        Godot.Image image = Godot.Image.CreateFromData(width, height, false, Godot.Image.Format.Rgb8, rgbData);
        ImageTexture texture = ImageTexture.CreateFromImage(image);

        return texture;
    }

    private byte[] ExtractIndexedBitmapData(System.Drawing.Bitmap bitmap, out int stride)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Lock the bitmap for reading
        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format8bppIndexed);

        stride = bmpData.Stride; // Store actual stride
        int dataSize = stride * height; // Use stride instead of width
        byte[] pixelData = new byte[dataSize];

        // Copy bitmap data (including padding)
        Marshal.Copy(bmpData.Scan0, pixelData, 0, dataSize);

        // Unlock bitmap
        bitmap.UnlockBits(bmpData);

        return pixelData;
    }

    private byte[] ConvertGrayscaleToRGB(byte[] grayscaleData, int width, int height, int stride)
    {
        byte[] rgbData = new byte[width * height * 3]; // RGB requires 3 bytes per pixel

        for (int y = 0; y < height; y++)
        {
            int srcRowStart = (height - 1 - y) * stride; // Bottom-up to top-down
            int dstRowStart = y * width * 3;

            for (int x = 0; x < width; x++)
            {
                byte gray = grayscaleData[srcRowStart + x]; // Read grayscale pixel
                int index = dstRowStart + (x * 3);
                rgbData[index] = gray;     // Red
                rgbData[index + 1] = gray; // Green
                rgbData[index + 2] = gray; // Blue
            }
        }

        return rgbData;
    }

    private System.Drawing.Bitmap ConvertTo8bppGrayscale(System.Drawing.Bitmap original)
    {
        int width = original.Width;
        int height = original.Height;
        System.Drawing.Bitmap grayBitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format8bppIndexed);

        using (Graphics g = Graphics.FromImage(grayBitmap))
        {
            // Convert the original image to grayscale
            g.DrawImage(original, new Rectangle(0, 0, width, height));
        }

        return grayBitmap;
    }

}
