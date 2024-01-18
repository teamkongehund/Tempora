using Godot;
using OsuTimer.Classes.Audio;

namespace OsuTimer.Classes.Utility;

public partial class Project : Node
{
    public static Project Instance = null!;

    private AudioFile audioFile = null!;

    private Settings settings = null!;

    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            if (audioFile == value)
                return;
            audioFile = value;
            _ = Signals.Instance.EmitSignal("AudioFileChanged");
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Instance = this;
}