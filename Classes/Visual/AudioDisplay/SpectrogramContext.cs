using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tempora.Classes.DataHelpers;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Spectrogram;

namespace Tempora.Classes.Visual.AudioDisplay;
/// <summary>
/// Context for generating and managing spectrograms.
/// </summary>
public class SpectrogramContext
{
    private Godot.Image? cachedSpectrogramImage;

    public Godot.Image? CachedSpectrogramImage
    {
        get => cachedSpectrogramImage;
        set => cachedSpectrogramImage = value;
    }

    private PcmData? pcmDataUsedForLastGeneration;

    // Using a delegate instead of a direct reference to allow for flexibility in how PCM data is provided.
    public delegate PcmData GetPcmDataDelegate();

    private static PcmData GetPcmDataFromProject() => Project.Instance.AudioFile;

    public GetPcmDataDelegate GetPcmData { get; set; }

    public SpectrogramGenerator SpectrogramGenerator { get; set; }

    public SpectrogramContext(SpectrogramGenerator spectrogramGenerator, GetPcmDataDelegate? pcmDataProvider = null)
    {
        GetPcmData = pcmDataProvider ?? GetPcmDataFromProject;
        SpectrogramGenerator = spectrogramGenerator;
    }

    private bool NeedToUpdateSpectrogram()
    {
        if (cachedSpectrogramImage == null)
            return true;
        if (GetPcmData() != pcmDataUsedForLastGeneration)
            return true;
        return false;
    }

    public void UpdateSpectrogram()
    {
        if (!NeedToUpdateSpectrogram())
            return;
        PcmData pcmData = GetPcmData();
        pcmDataUsedForLastGeneration = pcmData;
        CachedSpectrogramImage = SpectrogramHelper.GenerateGodotImage(
            SpectrogramGenerator,
            SpectrogramHelper.TemporaColormap,
            intensity: 5,
            dB: true
        );
        // Save the image to a file for debugging purposes
        //CachedSpectrogramImage.SavePng("user://spectrogram.png");
    }

    public ImageTexture GetSpectrogramSlice(
        int sampleStart,
        int sampleEnd,
        int targetHeight,
        int targetWidth
        )
    {
        UpdateSpectrogram();
        if (cachedSpectrogramImage == null)
            throw new InvalidOperationException("Cached spectrogram image is null.");

        return SpectrogramHelper.GetSpectrogramSlice(
            cachedSpectrogramImage, 
            SampleToX(sampleStart), 
            SampleToX(sampleEnd), 
            targetHeight,
            targetWidth
            );
    }

    private int SampleToX(int sample)
    {
        int numSamples = GetPcmData().PcmFloats[0].Length;
        
        // out of range samples are allowed
        //if (sample < 0 || sample >= numSamples)
        //    throw new ArgumentOutOfRangeException(nameof(sample), "Sample index out of range.");

        int cachedWidth = cachedSpectrogramImage?.GetWidth() ?? 0;

        return (int)((float)sample / numSamples * cachedWidth);
    }
}
