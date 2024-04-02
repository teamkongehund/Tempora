using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Audio;

public partial class Metronome : Node
{
    [Export]
    private AudioStreamPlayer click1 = null!;
    [Export]
    private AudioStreamPlayer click2 = null!;
    [Export]
    private Timer updateTimer = null!;
    private AudioPlayer audioPlayer = null!;

    public bool On = true;
    private float? previousMusicPosition;
    private double previousPlaybackTime;

    private float previousVolumeDb;
    private bool isMuted = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        audioPlayer = Project.Instance.SongPlayer;
        updateTimer.Timeout += UpdateMetronome;
    }

    public override void _Process(double delta)
    {
        
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

    public void UpdateMetronome()
    {
        if (isMuted || !audioPlayer.Playing)
            return;

        double playbackTime = audioPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        //click1.Play();
        Click(musicPosition);
    }

    private static float GetTriggerPosition(float musicPosition)
    {
        return Settings.Instance.MetronomeFollowsGrid
            ? Timing.Instance.GetOperatingGridPosition(musicPosition)
            : Timing.Instance.GetOperatingBeatPosition(musicPosition);
    }

    public void Click(float musicPosition)
    {
        if (!On)
            return;
        float triggerPosition = GetTriggerPosition(musicPosition);
        if (previousMusicPosition < triggerPosition && musicPosition >= triggerPosition)
        {
            if (triggerPosition % 1 == 0 && click1.GetPlaybackPosition() <= 0.01f)
                click1.Play();
            else if (click1.GetPlaybackPosition() <= 0.01f)
                click2.Play();
        }

        previousMusicPosition = musicPosition;
    }
}