using System;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Audio;

public partial class AudioPlayer : AudioStreamPlayer
{
    private AudioFile audioFile = null!;
    public AudioFile AudioFile
    {
        get => audioFile;
        set
        {
            if (audioFile == value)
                return;

            if (audioFile != null)
                audioFile.StreamChanged -= OnAudioFileStreamChanged;

            audioFile = value;
            LoadStream();
            audioFile.StreamChanged += OnAudioFileStreamChanged;
        }
    }

    public double PlaybackTime
    {
        get => GetPlaybackTime();
        private set { }
    }

    public override void _Ready()
    {
        //VolumeDb = base.VolumeDb;
        Signals.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
        //Signals.Instance.AudioFileChanged += OnAudioFileChanged;
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

    private void OnSelectedPositionChanged(object? _sender, EventArgs e)
    {
        float time = Timing.Instance.MusicPositionToTime(Context.Instance.SelectedMusicPosition);
        PauseTime = time >= 0 ? (double)time : 0;
    }

    //private void OnAudioFileChanged(object? sender, EventArgs e) => LoadStream();

    private void OnAudioFileStreamChanged(object? sender, EventArgs e) => LoadStream();

    public void Pause() =>
        //PausePosition = GetPlaybackTime();
        Stop();

    public double PauseTime;
    public void Resume()
    {
        Play();
        Seek((float)PauseTime);
    }

    public void PlayPause()
    {
        if (Playing)
            Pause();
        else
            Resume();
    }

    public void SeekPlay(float playbackTime)
    {
        if (!Playing)
            Play();
        if (playbackTime < 0)
            playbackTime = 0;
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

    public double GetPlaybackTime()
    {
        return Playing
            ? GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix() * PitchScale
            : PauseTime;
    }

    //public void LoadMp3()
    //{
    //    Stream = Project.Instance.SongFile.Stream ?? (FileAccess.FileExists(Project.Instance.SongFile.Path)
    //            ? FileHandler.LoadFileAsAudioStreamMp3(Project.Instance.SongFile.Path)
    //            : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.SongFile.Path} exists."));
    //}

    public void LoadStream()
    {
        Stream = AudioFile.Stream;
    }
}