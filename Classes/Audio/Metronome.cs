using System;
using System.IO;
using Godot;
using NAudio.Wave;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using FileAccess = Godot.FileAccess;

namespace Tempora.Classes.Audio;

public partial class Metronome : Node
{
    private AudioPlayer? audioPlayer;

    private float previousVolumeDb;
    private bool isMuted;

    private AudioStreamPlayer audioStreamPlayer = null!;
    private AudioStreamGenerator audioStreamGenerator = null!;
    private AudioStreamGeneratorPlayback? playback; // Will hold the AudioStreamGeneratorPlayback.
    private float bufferLength = 1f; // Maximum frame time until it becomes unable to keep the buffer filled
    private float sampleHz = 44100;

    private bool lastMetronomeFollowsGrid;
    private int lastGridDivisor;

    private Vector2[] click1Cache = null!;
    private Vector2[] click2Cache = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        click1Cache = CacheWavFile("res://Audio/Click1.wav");
        click2Cache = CacheWavFile("res://Audio/Click2.wav");

        audioPlayer = GetParentOrNull<AudioPlayer>();

        audioStreamGenerator = new AudioStreamGenerator { BufferLength = bufferLength, MixRate = sampleHz };
        audioStreamPlayer = new AudioStreamPlayer
        {
            Stream = audioStreamGenerator,
            Bus = "Metronome",
        };
        AddChild(audioStreamPlayer);

        if (audioPlayer is null) return;

        audioPlayer.Played += StartPlayback;
        audioPlayer.Seeked += SeekPlayback;
        audioPlayer.Paused += StopPlayback;
        audioPlayer.PitchScaleChanged += OnPitchScaleChanged;

        Signals.Instance.SettingsChanged += OnSettingsChanged;
        lastMetronomeFollowsGrid = Settings.Instance.MetronomeFollowsGrid;
        lastGridDivisor = Settings.Instance.GridDivisor;
    }

    private ulong currentFrame;
    private int clickFrames;
    private bool primaryClick;
    private float triggerPosition;
    private float triggerTime;

    private void FillBuffer(int maxBuffer = 4096)
    {
        if (playback is null) return;
        int framesAvailable = playback.GetFramesAvailable();
        var buffer = new Vector2[Mathf.Min(framesAvailable, maxBuffer)];
        int bufferIndex = 0;

        for (int i = 0; i < framesAvailable; i++)
        {
            float currentTime = currentFrame / sampleHz;

            if (currentTime > triggerTime)
            {
                clickFrames = triggerPosition % 1 == 0 ? click1Cache.Length : click2Cache.Length;
                primaryClick = triggerPosition % 1 == 0;
                UpdateTriggerTime(currentTime);
            }

            if (clickFrames > 0)
            {
                buffer[bufferIndex] = primaryClick ? click1Cache[^clickFrames] : click2Cache[^clickFrames];
                clickFrames--;
            }
            else
            {
                buffer[bufferIndex] = Vector2.Zero;
            }

            currentFrame++;
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

    private void UpdateTriggerTime(float currentTime)
    {
        float musicPosition = Timing.Instance.TimeToMusicPosition(currentTime);
        triggerPosition = GetTriggerPosition(musicPosition);
        triggerTime = Timing.Instance.MusicPositionToTime(triggerPosition);
    }

    private void OnPitchScaleChanged(float value)
    {
        audioStreamPlayer.PitchScale = value;
    }

    private void StartPlayback()
    {
        if (playback is not null) return;
        audioStreamPlayer.Play();
        playback = (AudioStreamGeneratorPlayback)audioStreamPlayer.GetStreamPlayback();
        FillBuffer();
    }

    private void SeekPlayback(double time)
    {
        currentFrame = (ulong)(time * sampleHz);
        UpdateTriggerTime((float)time);
        if (playback is null) return;
        StopPlayback();
        StartPlayback();
    }

    private void RefillBuffer()
    {
        if (playback is null) return;
        SeekPlayback(audioPlayer!.PlaybackTime);
    }

    private void StopPlayback()
    {
        audioStreamPlayer.Stop();
        playback = null;
    }

    public override void _Process(double delta)
    {
        FillBuffer();
    }

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

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent is { Keycode: Key.A, Pressed: true } && !isMuted)
            {
                // Mute metronome
                previousVolumeDb = audioStreamPlayer.VolumeDb;
                audioStreamPlayer.VolumeDb = -60;
                isMuted = true;
            }
            else if (keyEvent is { Keycode: Key.A, Pressed: false } && isMuted)
            {
                // Restore metronome volume to previous
                audioStreamPlayer.VolumeDb = previousVolumeDb;
                isMuted = false;
            }
        }
    }

    private static float GetTriggerPosition(float musicPosition)
    {
        return Settings.Instance.MetronomeFollowsGrid
            ? Timing.Instance.GetNextOperatingGridPosition(musicPosition)
            : Timing.Instance.GetNextOperatingBeatPosition(musicPosition);
    }

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
}