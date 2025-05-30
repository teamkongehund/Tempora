using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Spectrogram;
using Tempora.Classes.Audio;
using Tempora.Classes.Visual.AudioDisplay;
using Tempora.Classes.Visual;

namespace Tempora.Classes.DataHelpers;

/// <summary>
/// Helper class for generating and manipulating spectrograms.
/// </summary>
public static class SpectrogramHelper
{
    public static Colormap TemporaColormap = new Colormap(new CustomColormap(new List<Godot.Color> { GlobalConstants.TemporaBlue, new("ffffff") }));

    public static ImageTexture GetSpectrogramSlice(
    Godot.Image fullImage,
    int xStart,
    int xEnd,
    int targetHeight,
    int targetWidth)
    {
        int sliceWidth = xEnd - xStart;
        int sliceHeight = fullImage.GetHeight();

        if (xStart < 0 || xEnd > fullImage.GetWidth() || xStart >= xEnd)
            throw new ArgumentException("Invalid slice bounds");

        // Create image with the slice
        Godot.Image sliceImage = Godot.Image.CreateEmpty(sliceWidth, sliceHeight, false, fullImage.GetFormat());
        sliceImage.BlitRect(fullImage, new Rect2I(xStart, 0, sliceWidth, sliceHeight), new Vector2I(0, 0));

        // Resize vertically if needed
        if (sliceImage.GetHeight() != targetHeight)
        {
            sliceImage.Resize(targetWidth, targetHeight, Godot.Image.Interpolation.Nearest);
        }

        return ImageTexture.CreateFromImage(sliceImage);
    }

    /// <summary>
    /// Generate SpectrogramGenerator from AudioFile based on target width in pixels.
    /// </summary>
    public static SpectrogramGenerator GetSpectrogramGenerator_ByWidth(PcmData pcmData, int targetWidthPixels = 3000, int fftSize = 16384, int maxFreq = 2200, double multiplier = 16_000)
    {
        int stepSize = pcmData.PcmFloats[0].Length / targetWidthPixels;
        return GetSpectrogramGenerator(pcmData, stepSize, fftSize, maxFreq, multiplier);
    }

    /// <summary>
    /// Generate SpectrogramGenerator from AudioFile with known stepSize.
    /// </summary>
    public static SpectrogramGenerator GetSpectrogramGenerator(PcmData pcmData, int stepSize = 100, int fftSize = 16384, int maxFreq = 20000, double multiplier = 16_000)
    {
        if (stepSize < 1) throw new ArgumentOutOfRangeException(nameof(stepSize), "Must be at least 1.");
        double[] audio = pcmData.GetPcmAsDoubles(multiplier);
        int sampleRate = pcmData.SampleRate;
        var spectrogramGenerator = new SpectrogramGenerator(sampleRate, fftSize, stepSize, maxFreq);

        spectrogramGenerator.Add(audio);

        return spectrogramGenerator;
    }

    public static System.Drawing.Bitmap GenerateBitmap(SpectrogramGenerator spectrogramGenerator, Colormap colormap, int intensity = 5, bool dB = true)
    {
        var ffts = spectrogramGenerator.GetFFTs();
        return Spectrogram.Image.GetBitmap(ffts, colormap, intensity, dB);
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

    public static ImageTexture ConvertBitmapToImageTexture(System.Drawing.Bitmap bitmap)
    {
        Godot.Image gdImage = ConvertBitmapToGodotImage(bitmap);
        ImageTexture texture = ImageTexture.CreateFromImage(gdImage);
        return texture;
    }

    public static ImageTexture GenerateTexture(SpectrogramGenerator spectrogramGenerator, Colormap colormap, int intensity = 5, bool dB = true)
    {
        var bitmap = GenerateBitmap(spectrogramGenerator, colormap, intensity, dB);
        return ConvertBitmapToImageTexture(bitmap);
    }

    public static Godot.Image GenerateGodotImage(SpectrogramGenerator spectrogramGenerator, Colormap colormap, int intensity = 5, bool dB = true)
    {
        return ConvertBitmapToGodotImage(GenerateBitmap(spectrogramGenerator, colormap, intensity, dB));
    }
}
