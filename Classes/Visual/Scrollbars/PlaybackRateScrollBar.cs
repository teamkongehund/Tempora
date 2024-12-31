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

using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Visual;

public partial class PlaybackRateScrollBar : LabeledScrollbarHorizontal
{
    private MusicPlayer MusicPlayer => MusicPlayer.Instance;

    public override void _Ready()
    {
        base._Ready();

        MusicPlayer.Instance.PitchScaleChanged += OnPitchScaleChanged;
    }

    protected override void UpdateValueLabel() => valueLabel.Text = (hScrollBar.Value * 100).ToString("0") + " %";

    protected override void UpdateTarget()
    {
        MusicPlayer.SetPitchScale((float)hScrollBar.Value);
    }

    protected override void SetInitialValue() => hScrollBar.Value = 1;

    private void OnPitchScaleChanged(float pitchScale) => UpdateValue(pitchScale);

    /// <summary>
    /// Used to manually update the scrollbar from a different class, like <see cref="MusicPlayer"/>.
    /// </summary>
    private void UpdateValue(double pitchScale)
    {
        hScrollBar.Value = pitchScale;
        UpdateValueLabel();
    }
}