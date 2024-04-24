using System;
using System.IO;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Audio;

public partial class MusicPlayer : AudioStreamPlayer
{
    public static MusicPlayer Instance = null!;
    //new public float VolumeDb; // Hides actual volume from other API's, so they can't mess with volume while fading.

    public event Action<double>? Seeked;
    public event Action? PlaybackStarted;
    public event Action? Paused;
    public event Action<float>? PitchScaleChanged;

    private bool hasFirstAudioLoaded = false;

    public void SetPitchScale(float pitchScale)
    {
        PitchScale = pitchScale;
        PitchScaleChanged?.Invoke(pitchScale);
    }

    public double PlaybackTime
    {
        get => GetPlaybackTime();
        private set { }
    }

    public override void _Ready()
    {
        Instance = this;
        //VolumeDb = base.VolumeDb;
        GlobalEvents.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
        GlobalEvents.Instance.AudioFileChanged += OnAudioFileChanged;
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

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey keyEvent:
                {
                    if (keyEvent.Keycode == Key.Space && keyEvent.Pressed)
                        PlayPause();
                    break;
                }
        }
    }

    private void OnSelectedPositionChanged(object? _sender, EventArgs e)
    {
        float time = Timing.Instance.MusicPositionToTime(Context.Instance.SelectedMusicPosition);
        PauseTime = time >= 0 ? (double)time : 0;
    }

    private void OnAudioFileChanged(object? sender, EventArgs e) => LoadAudio();

    public void Pause()
    {
        //PausePosition = GetPlaybackTime();
        Stop();
        Paused?.Invoke();
    }

    public double PauseTime;
    public void Resume()
    {
        Play();
        Seek((float)PauseTime);
        Seeked?.Invoke(PauseTime);
        PlaybackStarted?.Invoke();
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
        Seeked?.Invoke(playbackTime);
        PlaybackStarted?.Invoke();
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

    private void LoadMp3()
    {
        Stream = Project.Instance.AudioFile.Stream ?? (Godot.FileAccess.FileExists(Project.Instance.AudioFile.Path)
                ? FileHandler.LoadFileAsAudioStreamMp3(Project.Instance.AudioFile.Path)
                : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.AudioFile.Path} exists."));
    }

    private void LoadOgg()
    {
        Stream = Project.Instance.AudioFile.Stream ?? (Godot.FileAccess.FileExists(Project.Instance.AudioFile.Path)
                ? AudioStreamOggVorbis.LoadFromFile(Project.Instance.AudioFile.Path)
                : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.AudioFile.Path} exists."));
    }

    private void LoadAudio()
    {
        string extension = Path.GetExtension(Project.Instance.AudioFile.Path);
        switch (extension)
        {
            case "mp3":
                LoadMp3();
                break;
            case "ogg":
                LoadOgg();
                break;
        }
        if (!hasFirstAudioLoaded)
        {
            hasFirstAudioLoaded = true;
            return;
        }
        string? fileName = Path.GetFileName(Project.Instance.ProjectPath);
        Project.Instance.NotificationMessage = fileName == null
            ? "Audio loaded!"
            : $"Audio loaded! You are still editing {fileName}";
    }
}