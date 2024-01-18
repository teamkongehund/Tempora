using Godot;
using OsuTimer.Classes.Audio;

namespace OsuTimer.Classes.Utility;

public partial class Project : Node
{
    private static Project instance = null!;

    private AudioFile audioFile = null!;

    private Settings settings = null!;

    public static Project Instance { get => instance; set => instance = value; }

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