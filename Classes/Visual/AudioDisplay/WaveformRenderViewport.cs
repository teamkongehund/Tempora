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