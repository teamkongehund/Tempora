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

namespace Tempora.Classes.Visual;

public partial class VolumeSlider : HScrollBar
{
    private int busIndex;
    [Export] public string BusName = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        busIndex = AudioServer.GetBusIndex(BusName);
        ValueChanged += OnValueChanged;

        float invertedValue = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
        //Value = Math.Abs(1 - invertedValue);

        Value = invertedValue;
    }

    private void OnValueChanged(double value)
    {
        AudioServer.SetBusVolumeDb(
            busIndex,
            Mathf.LinearToDb((float)value));
        SaveToSettings(value);
    }

    private void SaveToSettings(double value)
    {
        switch (BusName)
        {
            case "Music":
                Settings.Instance.MusicVolumeNormalized = value;
                break;
            case "Metronome":
                Settings.Instance.MetronomeVolumeNormalized = value;
                break;
            case "Master":
                Settings.Instance.MasterVolumeNormalized = value;
                break;
        }
    }
}