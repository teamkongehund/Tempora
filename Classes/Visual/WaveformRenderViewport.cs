using Godot;
using OsuTimer.Classes.Audio;
using GD = OsuTimer.Classes.Utility.GD;

namespace OsuTimer.Classes.Visual;

public partial class WaveformRenderViewport : SubViewport
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => RenderWaveform();

    public async void RenderWaveform()
    {
        var audiofile = new AudioFile("res://audio/21csm.mp3");

        var waveform = new Waveform(audiofile, Size.X, Size.Y * 1.2f, new float[2] { 0, audiofile.GetAudioLength() })
        {
            Position = new Vector2(0, Size.Y / 2)
        };
        AddChild(waveform);

        _ = await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _ = GetViewport().GetTexture().GetImage().SavePng("user://renderedWave.png");
        GD.Print("Saved to PNG!");
    }
}
