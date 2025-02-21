using Godot;
using System;
using Spectrogram;
using Tempora.Classes.DataHelpers;
using System.Collections.Generic;
using System.Linq;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual.AudioDisplay;
public partial class SpectrogramTest : Node2D
{
    // See https://github.com/swharden/Spectrogram

    [Export]
    AudioStreamMP3 audioStreamMP3 = null!;

    [Export]
    Sprite2D spectrogramSprite2D = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        (double[] audio, int sampleRate) = ReadMono(ProjectSettings.GlobalizePath(audioStreamMP3.ResourcePath));

        int fftSize = 16384;
        int targetWidthPx = 3000;
        int stepSize = audio.Length / targetWidthPx;

        var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, maxFreq: 2200);
        sg.Add(audio);
        sg.Colormap = new Colormap(new CustomColormap(new List<Color> { GlobalConstants.TemporaBlue, new("ffffff") }));
        sg.SaveImage("song.png", intensity: 5, dB: true);
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
}
