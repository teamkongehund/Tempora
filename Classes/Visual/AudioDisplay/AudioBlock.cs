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
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Visual.AudioDisplay;

public partial class AudioBlock : Control
{
    [Export]
    public AudioDisplayPanel AudioDisplayPanel = null!;
    [Export]
    private Label measureLabel = null!;
    [Export]
    private TimeSignatureLineEdit timeSignatureLineEdit = null!;
    [Export]
    private TimeSignatureStepper timeSignatureStepper = null!;

    private Color defaultFontColor = new("ffffff");

    private int nominalMeasurePosition;
    public int NominalMeasurePosition
    {
        get => nominalMeasurePosition;
        set
        {
            nominalMeasurePosition = value;

            AudioDisplayPanel.NominalMeasurePosition = NominalMeasurePosition;
            UpdateLabels();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEvents.Instance.TimingChanged += OnTimingChanged;

        VisibilityChanged += OnVisibilityChanged;

        timeSignatureLineEdit.TimeSignatureSubmitted += OnTimingSignatureSubmitted;
        timeSignatureStepper.TimeSignatureSubmitted += OnTimingSignatureSubmitted;
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible || Timing.Instance.IsBatchOperationInProgress)
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
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMeasurePosition);
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMeasurePosition);
        measureLabel.Text = NominalMeasurePosition.ToString();
        timeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
        timeSignatureStepper.TimeSignature = timeSignature;
        bool isTimeSigPointHere = Timing.Instance.TimeSignaturePoints.Exists(point => point.Measure == nominalMeasurePosition);
        if (isTimeSigPointHere)
        {
            timeSignatureLineEdit.AddThemeColorOverride("font_color", GlobalConstants.TemporaYellow);
            timeSignatureStepper.UpdateLabelColor(GlobalConstants.TemporaYellow);
        }
        else
        {
            timeSignatureLineEdit.AddThemeColorOverride("font_color", defaultFontColor);
            timeSignatureStepper.UpdateLabelColor(defaultFontColor);
        }
    }
}