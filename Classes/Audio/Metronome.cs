using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Audio;

public partial class Metronome : Node
{
    private AudioStreamPlayer click1 = null!;
    private AudioStreamPlayer click2 = null!;

    public bool On = true;
    private float? previousMusicPosition;
    private double previousPlaybackTime;

    private float previousVolumeDb;
    private bool isMuted = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        click1 = GetNode<AudioStreamPlayer>("Click1");
        click2 = GetNode<AudioStreamPlayer>("Click2");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.A && keyEvent.Pressed && !isMuted)
            {
                // Mute metronome
                previousVolumeDb = click1.VolumeDb;

                click1.VolumeDb = -60;
                click2.VolumeDb = -60;

                isMuted = true;
            }
            else if (keyEvent.Keycode == Key.A && !keyEvent.Pressed && isMuted)
            {
                // Restore metronome volume to previous
                click1.VolumeDb = previousVolumeDb;
                click2.VolumeDb = previousVolumeDb;

                isMuted = false;
            }
        }
    }

    public void Click(float musicPosition)
    {
        if (!On)
            return;
        float beatPosition = Timing.Instance.GetBeatPosition(musicPosition);
        if (previousMusicPosition < beatPosition && musicPosition >= beatPosition)
        {
            if (beatPosition % 1 == 0 && click1.GetPlaybackPosition() <= 0.01f)
                click1.Play();
            else if (click1.GetPlaybackPosition() <= 0.01f)
                click2.Play();
        }

        previousMusicPosition = musicPosition;
    }
}