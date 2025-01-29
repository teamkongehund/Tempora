// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

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

    public float DefaultPlaybackRate = 1f;

    public float AlternativePlaybackRate = 0.5f;

    private bool isUsingAlternativePlaybackRate = false;
    private bool IsUsingAlternativePlaybackRate
    {
        get => isUsingAlternativePlaybackRate;
        set
        {
            if (isUsingAlternativePlaybackRate == value)
                return;
            isUsingAlternativePlaybackRate = value;
            SetPitchScale(isUsingAlternativePlaybackRate ? AlternativePlaybackRate : DefaultPlaybackRate);
        }
    }

    public new void SetPitchScale(float pitchScale)
    {
        if (isUsingAlternativePlaybackRate)
            AlternativePlaybackRate = pitchScale;
        else
            DefaultPlaybackRate = pitchScale;

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
        GlobalEvents.Instance.TimingPointAdded += OnTimingPointAdded;
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
                    if (keyEvent.Keycode == Key.X && keyEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl))
                        IsUsingAlternativePlaybackRate = !IsUsingAlternativePlaybackRate;
                    break;
                }
        }
    }

    private void OnSelectedPositionChanged(object? _sender, EventArgs e)
    {
        float time = Timing.Instance.MeasurePositionToSampleTime(Context.Instance.SelectedMeasurePosition);
        PauseTime = time >= 0 ? (double)time : 0;
    }

    private void OnAudioFileChanged(object? sender, EventArgs e)
    {
        Pause();
        LoadAudio();
    }

    private void OnTimingPointAdded(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArg)
            return;
        if (!Settings.Instance.SeekPlaybackOnTimingPointChanges)
            return;
        var timingPoint = timingPointArg.Value;
        SeekPlay(timingPoint.Offset - 0.05f);
    }

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

    public void SeekPlay(float sampleTime)
    {
        float playbackTime = Project.Instance.AudioFile.SampleTimeToPlaybackTime(sampleTime);
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
        Stream = Project.Instance.AudioFile.Stream ?? (Godot.FileAccess.FileExists(Project.Instance.AudioFile.FilePath)
                ? FileHandler.LoadFileAsAudioStreamMp3(Project.Instance.AudioFile.FilePath)
                : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.AudioFile.FilePath} exists."));
    }

    private void LoadOgg()
    {
        Stream = Project.Instance.AudioFile.Stream ?? (Godot.FileAccess.FileExists(Project.Instance.AudioFile.FilePath)
                ? AudioStreamOggVorbis.LoadFromFile(Project.Instance.AudioFile.FilePath)
                : throw new Exception($"Failed to update songPlayer stream - check if {Project.Instance.AudioFile.FilePath} exists."));
    }

    private void LoadAudio()
    {
        var audioFile = Project.Instance.AudioFile;
        string extension = audioFile.Extension ?? Path.GetExtension(Project.Instance.AudioFile?.FilePath ?? "").ToLower();
        switch (extension)
        {
            case ".mp3":
                LoadMp3();
                break;
            case ".ogg":
                LoadOgg();
                break;
            default:
                throw new Exception("Could not determine audio codec");
        }
        if (!hasFirstAudioLoaded)
        {
            hasFirstAudioLoaded = true;
            return;
        }
        string? fileName = Path.GetFileName(Project.Instance.ProjectPath);
        Project.Instance.NotificationMessage = fileName == null
            ? "Audio loaded!"
            : $"Audio loaded! You are currently editing {fileName}";
    }
}