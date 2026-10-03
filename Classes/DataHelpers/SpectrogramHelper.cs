using System;
using System.Collections.Generic;
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
		int imageWidth = fullImage.GetWidth();
		xStart = Math.Clamp(xStart, 0, imageWidth);
		xEnd = Math.Clamp(xEnd, 0, imageWidth);

		if (xStart >= xEnd)
			return ImageTexture.CreateFromImage(Godot.Image.CreateEmpty(targetWidth, targetHeight, false, fullImage.GetFormat()));

		int sliceWidth = xEnd - xStart;
		int sliceHeight = fullImage.GetHeight();

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

	/// <summary>
	/// Creates a Godot Image directly from FFT data, avoiding System.Drawing (libgdiplus) dependency.
	/// </summary>
	public static Godot.Image GenerateGodotImage(SpectrogramGenerator spectrogramGenerator, Colormap colormap, int intensity = 5, bool dB = true)
	{
		var ffts = spectrogramGenerator.GetFFTs();
		int width = ffts.Count;
		int height = ffts[0].Length;
		byte[] buffer = new byte[width * height * 4];

		for (int col = 0; col < width; col++)
		{
			double[] fft = ffts[col];
			for (int row = 0; row < height; row++)
			{
				double value = fft[row];
				if (dB)
					value = 20 * Math.Log10(value + 1);
				value *= intensity;
				value = Math.Min(value, 255);

				var (r, g, b) = colormap.GetRGB((byte)value);

				// Spectrogram row 0 = lowest frequency, so flip vertically
				int pixelIndex = ((height - 1 - row) * width + col) * 4;
				buffer[pixelIndex] = r;
				buffer[pixelIndex + 1] = g;
				buffer[pixelIndex + 2] = b;
				buffer[pixelIndex + 3] = 255;
			}
		}

		return Godot.Image.CreateFromData(width, height, false, Godot.Image.Format.Rgba8, buffer);
	}

	public static ImageTexture GenerateTexture(SpectrogramGenerator spectrogramGenerator, Colormap colormap, int intensity = 5, bool dB = true)
	{
		Godot.Image gdImage = GenerateGodotImage(spectrogramGenerator, colormap, intensity, dB);
		return ImageTexture.CreateFromImage(gdImage);
	}
}
