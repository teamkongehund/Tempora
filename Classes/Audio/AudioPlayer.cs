using System;
using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Audio;

public partial class AudioPlayer : AudioStreamPlayer {

    //new public float VolumeDb; // Hides actual volume from other API's, so they can't mess with volume while fading.

    public double PlaybackTime {
        get => GetPlaybackTime();
        private set { }
    }


    public override void _Ready() {
        //VolumeDb = base.VolumeDb;
        Signals.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
        Signals.Instance.AudioFileChanged += LoadMp3;
    }

    public override void _Process(double delta)
    {
        if (isTurnedDown)
        {
            if (PlaybackTime >= timeToResumeHarsly)
            {
                VolumeDb = 0;
                isTurnedDown = false;
            }
        }
    }

    public void OnSelectedPositionChanged() {
        float time = Timing.Instance.MusicPositionToTime(Context.Instance.SelectedMusicPosition);
        if (time >= 0)
            PauseTime = time;
        else
            PauseTime = 0;
    }

    public void Pause() {
        //PausePosition = GetPlaybackTime();
        Stop();
    }

    public double PauseTime;
    public void Resume() {
        Play();
        Seek((float)PauseTime);
    }

    public void PlayPause() {
        if (Playing) Pause();
        else Resume();
    }

    public void SeekPlay(float playbackTime)
    {
        if (!Playing) Play();
        if (playbackTime < 0) playbackTime = 0;
        Seek(playbackTime);
    }

    private bool isTurnedDown = false;
    private float timeToResumeHarsly;
    /// <summary>
    /// Workaround AudioPlayer's built-in fade-in. 
    /// </summary>
    /// <param name="playbackTime"></param>
    public void SeekPlayHarsh(float playbackTime)
    {
        float earlierPlaybackTime = playbackTime - 0.02f;
        
        SeekPlay(earlierPlaybackTime);

        VolumeDb = -60;
        isTurnedDown = true;
        timeToResumeHarsly = playbackTime;
    }

    public double GetPlaybackTime() {
        return Playing
            ? GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix()
            : PauseTime;
    }

    public void LoadMp3() {
        Stream = FileAccess.FileExists(Project.Instance.AudioFile.Path)
            ? FileHandler.LoadFileAsAudioStreamMp3(Project.Instance.AudioFile.Path)
            : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.AudioFile.Path} exists.");
    }
}