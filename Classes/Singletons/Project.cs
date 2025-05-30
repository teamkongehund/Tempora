// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using System.IO;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.DataHelpers;
using Tempora.Classes.Visual.AudioDisplay;

namespace Tempora.Classes.Utility;

/// <summary>
/// Contains data about the current project.
/// </summary>
public partial class Project : Node
{
    private static Project instance = null!;

    private AudioFile audioFile = null!;

    private Settings settings = null!;

    private string? projectPath = null;
    public string? ProjectPath
    {
        get => projectPath;
        set
        {
            if (value == projectPath)
                return;
            projectPath = value;
            string? projectName = Path.GetFileName(projectPath);
            string titleAddition = projectName != null ? $" - {projectName}" : "";
            GetWindow().Title = "Tempora" + titleAddition;
        }
    }

    public event EventHandler NotificationMessageChanged = null!;
    private string notificationMessage = null!;

    public string NotificationMessage
    {
        get => notificationMessage;
        set
        {
            notificationMessage = value;
            NotificationMessageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public static Project Instance { get => instance; set => instance = value; }

    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            if (audioFile == value)
                return;
            audioFile = value;
            UpdateSpectrogramContextFromSettings();
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.AudioFileChanged), this, EventArgs.Empty);
        }
    }

    public SpectrogramContext SpectrogramContext { get; set; } = null!;

    public void UpdateSpectrogramContextFromSettings()
    {
        int stepSize = Settings.Instance.SpectrogramStepSize;
        int fftSize = Settings.Instance.SpectrogramFftSize;
        int maxFreq = Settings.Instance.SpectrogramMaxFreq;
        bool dB = Settings.Instance.SpectrogramUseDb;
        int intensity = Settings.Instance.SpectrogramIntensity;
        SpectrogramContext = new(SpectrogramHelper.GetSpectrogramGenerator(AudioFile, stepSize, fftSize, maxFreq, 16000));
        SpectrogramContext.DB = dB;
        SpectrogramContext.Intensity = intensity;
        SpectrogramContext.UpdateSpectrogram();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
    }
}