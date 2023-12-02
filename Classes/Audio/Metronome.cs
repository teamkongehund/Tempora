using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Audio;

public partial class Metronome : Node {
    private AudioStreamPlayer click1;
    private AudioStreamPlayer click2;

    public bool On = true;
    private float? previousMusicPosition;
    private double previousPlaybackTime;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        click1 = GetNode<AudioStreamPlayer>("Click1");
        click2 = GetNode<AudioStreamPlayer>("Click2");
    }

    // Todo 2: Replace metronome sounds
    public void Click(float musicPosition) {
        if (!On) return;
        float beatPosition = Timing.Instance.GetBeatPosition(musicPosition);
        if (previousMusicPosition < beatPosition && musicPosition >= beatPosition) {
            if (beatPosition % 1 == 0)
                click1.Play();
            else
                click2.Play();
        }

        previousMusicPosition = musicPosition;
    }
}