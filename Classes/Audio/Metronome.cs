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
using NAudio.Wave;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.DataHelpers;
using FileAccess = Godot.FileAccess;

namespace Tempora.Classes.Audio;

public partial class Metronome : Node
{
    [Export]
    private MusicPlayer musicPlayer = null!;

    private float previousVolumeDb;
    private bool isMuted;

    private AudioStreamPlayer audioStreamPlayer = null!;
    private AudioStreamGenerator audioStreamGenerator = null!;
    /// <summary>
    /// Will hold the AudioStreamGeneratorPlayback. Object gets a new instance whenever music playback is stopped and started.
    /// </summary>
    private AudioStreamGeneratorPlayback? playback;
    private float bufferLength = 1f; // Maximum frame time (seconds) until it becomes unable to keep the buffer filled
    private float musicSampleRate = 44100; // Hz
    private float musicPitchScale = 1f;

    private bool lastMetronomeFollowsGrid;
    private int lastGridDivisor;

    private Vector2[] click1Cache = null!;
    private int metronomeSampleRate;
    private Vector2[] click2Cache = null!;

    [Export]
    AudioStreamWav click1Stream = null!;
    [Export]
    AudioStreamWav click2Stream = null!;

    #region Godot
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        click1Cache = AudioDataHelper.ConvertToStereoVector2Floats(click1Stream.Data);
        click2Cache = AudioDataHelper.ConvertToStereoVector2Floats(click2Stream.Data);

        metronomeSampleRate = click1Stream.MixRate;

        audioStreamGenerator = new AudioStreamGenerator { BufferLength = bufferLength, MixRate = metronomeSampleRate };
        audioStreamPlayer = new AudioStreamPlayer
        {
            Stream = audioStreamGenerator,
            Bus = "Metronome",
        };
        AddChild(audioStreamPlayer);

        if (musicPlayer is null) return;
        musicPitchScale = musicPlayer.PitchScale;

        musicSampleRate = Project.Instance.AudioFile?.SampleRate ?? musicSampleRate;
        GlobalEvents.Instance.AudioFileChanged += OnAudioFileChanged;

        musicPlayer.PlaybackStarted += StartPlayback;
        musicPlayer.Seeked += SeekPlayback;
        musicPlayer.Paused += StopPlayback;
        musicPlayer.Finished += StopPlayback;
        musicPlayer.PitchScaleChanged += OnPitchScaleChanged;
        GlobalEvents.Instance.TimingChanged += OnTimingChanged;

        GlobalEvents.Instance.SettingsChanged += OnSettingsChanged;
        lastMetronomeFollowsGrid = Settings.Instance.MetronomeFollowsGrid;
        lastGridDivisor = Settings.Instance.GridDivisor;
    }
    public override void _Process(double delta)
    {
        //UpdateCurrentMusicFrame(musicPlayer!.PlaybackTime); // Ensures music and metronome don't go out of sync due to accumulating rounding errors. 
        FillBuffer();
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent is { Keycode: Key.Z, Pressed: true } && !isMuted)
            {
                // Mute metronome
                previousVolumeDb = audioStreamPlayer.VolumeDb;
                audioStreamPlayer.VolumeDb = -60;
                isMuted = true;
            }
            else if (keyEvent is { Keycode: Key.Z, Pressed: false } && isMuted)
            {
                // Restore metronome volume to previous
                audioStreamPlayer.VolumeDb = previousVolumeDb;
                isMuted = false;
            }
        }
    } 
    #endregion

    #region Buffer

    private double currentBufferMusicSample;
    private int numClickFramesLeftToAdd;
    private bool isPrimaryClick;
    private double triggerPosition;
    private double triggerTime;

    private float timeBetweenCurrentFrameSyncs = 3;

    private void FillBuffer(int maxBuffer = 4096)
    {
        if (playback is null) return;
        int framesAvailable = playback.GetFramesAvailable();
        var buffer = new Vector2[Mathf.Min(framesAvailable, maxBuffer)];
        int bufferIndex = 0;

        float sampleRateRatio = metronomeSampleRate / musicSampleRate;

        double initialBufferMusicFrame = currentBufferMusicSample;
        for (int i = 0; i < framesAvailable; i++)
        {
            double currentBufferMusicTime = currentBufferMusicSample / musicSampleRate;

            if (currentBufferMusicTime > triggerTime)
            {
                numClickFramesLeftToAdd = triggerPosition % 1 == 0 ? click1Cache.Length : click2Cache.Length;
                isPrimaryClick = triggerPosition % 1 == 0;
                UpdateTriggerTime(currentBufferMusicTime);
            }

            if (numClickFramesLeftToAdd > 0)
            {
                buffer[bufferIndex] = isPrimaryClick ? click1Cache[^numClickFramesLeftToAdd] : click2Cache[^numClickFramesLeftToAdd];
                numClickFramesLeftToAdd--;
            }
            else
            {
                buffer[bufferIndex] = Vector2.Zero;
            }

            currentBufferMusicSample = initialBufferMusicFrame + musicPitchScale * (i+1) / sampleRateRatio;
            bufferIndex++;

            if (bufferIndex >= buffer.Length)
            {
                playback.PushBuffer(buffer);
                bufferIndex = 0;
            }
        }

        if (bufferIndex > 0)
            playback.PushBuffer(buffer[..bufferIndex]);
    }

    private void RefillBuffer()
    {
        if (playback is null) return;
        SeekPlayback(musicPlayer.PlaybackTime);
    }

    private void UpdateTriggerTime(double currentPlaybackTime)
    {
        double currentSampleTime = Project.Instance.AudioFile.PlaybackTimeToSampleTime(currentPlaybackTime);
        double musicPosition = Timing.Instance.SampleTimeToMusicPosition(currentSampleTime);
        triggerPosition = GetTriggerPosition(musicPosition);
        var triggerSampleTime = Timing.Instance.MusicPositionToSampleTime(triggerPosition);
        triggerTime = triggerSampleTime;
        //triggerTime = Project.Instance.AudioFile.SampleTimeToPlaybackTime(triggerSampleTime);
    }
    #endregion


    #region Playback control
    private void StartPlayback()
    {
        if (playback is not null) return;
        audioStreamPlayer.Play();
        playback = (AudioStreamGeneratorPlayback)audioStreamPlayer.GetStreamPlayback();
        FillBuffer();
    }

    private void SeekPlayback(double playbackTime)
    {
        currentBufferMusicSample = Project.Instance.AudioFile.PlaybackTimeToSampleTime((float)playbackTime) * musicSampleRate;
        UpdateTriggerTime(playbackTime);
        if (playback is null) return;
        StopPlayback();
        StartPlayback();
    }

    private void StopPlayback()
    {
        audioStreamPlayer.Stop();
        playback = null;
    } 
    #endregion

    #region Events
    private void OnPitchScaleChanged(float value)
    {
        musicPitchScale = value;
        RefillBuffer();
    }
    private void OnTimingChanged(object? sender, EventArgs e)
    {
        RefillBuffer();
    }
    private void RefillBuffer(object? sender, EventArgs e) => RefillBuffer();

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (Settings.Instance.MetronomeFollowsGrid != lastMetronomeFollowsGrid)
        {
            lastMetronomeFollowsGrid = Settings.Instance.MetronomeFollowsGrid;
            RefillBuffer();
        }
        if (Settings.Instance.GridDivisor != lastGridDivisor)
        {
            lastGridDivisor = Settings.Instance.GridDivisor;
            RefillBuffer();
        }
    }
    private void OnAudioFileChanged(object? sender, EventArgs e)
    {
        musicSampleRate = Project.Instance.AudioFile.SampleRate;
        //audioStreamGenerator = new AudioStreamGenerator { BufferLength = bufferLength, MixRate = musicSampleRate };
        //audioStreamGenerator.MixRate = musicSampleRate;
    }
    #endregion


    private static double GetTriggerPosition(double musicPosition)
    {
        return Settings.Instance.MetronomeFollowsGrid
            ? Timing.Instance.GetNextOperatingGridPosition(musicPosition)
            : Timing.Instance.GetNextOperatingBeatPosition(musicPosition);
    }


    [Obsolete]
    private static Vector2[] CacheWavFile(string path)
    {
        using FileAccess? file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        using var fileStream = new MemoryStream(file.GetBuffer((long)file.GetLength()));
        using var waveReader = new WaveFileReader(fileStream);
        var cache = new Vector2[waveReader.SampleCount];
        for (int i = 0; i < waveReader.SampleCount; i++)
        {
            float[]? sample = waveReader.ReadNextSampleFrame();
            cache[i] = new Vector2(sample[0], sample[1]);
        }

        return cache;
    }

    private static int GetSampleRate(string path)
    {
        using FileAccess? file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        using var fileStream = new MemoryStream(file.GetBuffer((long)file.GetLength()));
        using var waveReader = new WaveFileReader(fileStream);

        return waveReader.WaveFormat.SampleRate;
    }
}