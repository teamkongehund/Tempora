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
using GD = Tempora.Classes.DataHelpers.GD;

namespace Tempora.Classes.Visual;

public partial class WaveformRenderViewport : SubViewport
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => RenderWaveform();

    public async void RenderWaveform()
    {
        var audiofile = new AudioFile("res://audio/21csm.mp3");

        var waveform = new Waveform(audiofile, Size.X, Size.Y * 1.2f, [0, audiofile.GetAudioLength()])
        {
            Position = new Vector2(0, Size.Y / 2)
        };
        AddChild(waveform);

        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng("user://renderedWave.png");
        GD.Print("Saved to PNG!");
    }
}