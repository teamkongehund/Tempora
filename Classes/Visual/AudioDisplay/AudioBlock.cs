// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Visual;

public partial class AudioBlock : Control
{
    [Export]
    public AudioDisplayPanel AudioDisplayPanel = null!;
    [Export]
    private Label measureLabel = null!;
    [Export]
    private TimeSignatureLineEdit timeSignatureLineEdit = null!;

    private int musicPositionStart;
    public int NominalMusicPositionStartForWindow
    {
        get => musicPositionStart;
        set
        {
            musicPositionStart = value;

            AudioDisplayPanel.NominalMusicPositionStartForWindow = NominalMusicPositionStartForWindow;
            UpdateLabels();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEvents.Instance.TimingChanged += OnTimingChanged;

        VisibilityChanged += OnVisibilityChanged;

        timeSignatureLineEdit.TimeSignatureSubmitted += OnTimingSignatureSubmitted;
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible)
            return;
        UpdateLabels();
    }

    private void OnVisibilityChanged()
    {
        AudioDisplayPanel.Visible = Visible;
    }

    private void OnTimingSignatureSubmitted(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<int[]> intArrayArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<int[]>)}");
        int[] timeSignature = intArrayArgument.Value;
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);
        measureLabel.Text = NominalMusicPositionStartForWindow.ToString();
        timeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
    }
}