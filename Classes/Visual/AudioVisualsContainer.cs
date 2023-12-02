using System.Collections.Generic;
using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class AudioVisualsContainer : VBoxContainer {
    [Signal]
    public delegate void DoubleClickedEventHandler(float playbackTime);
    //[Export] public int NumberOfBlocks = 10;

    /// <summary>
    ///     Forward childrens' signals to Main
    /// </summary>
    /// <param name="playbackTime"></param>
    [Signal]
    public delegate void SeekPlaybackTimeEventHandler(float playbackTime);

    //public float FirstBlockStartTime = 0;

    private int nominalMusicPositionStartForTopBlock;

    public List<WaveformWindow> WaveformWindows = new();

    public int NominalMusicPositionStartForTopBlock {
        get => nominalMusicPositionStartForTopBlock;
        set {
            if (value != nominalMusicPositionStartForTopBlock) {
                nominalMusicPositionStartForTopBlock = value;
                UpdateBlocksScroll();
            }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        //NumberOfBlocks = Settings.Instance.NumberOfBlocks;
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl)) {
                NominalMusicPositionStartForTopBlock += Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                Signals.Instance.EmitSignal("Scrolled");
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && !Input.IsKeyPressed(Key.Ctrl)) {
                NominalMusicPositionStartForTopBlock -= Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                Signals.Instance.EmitSignal("Scrolled");
            }
        }
    }

    public void CreateBlocks() {
        foreach (var child in GetChildren()) child.QueueFree();
        WaveformWindows.Clear();

        var packedWaveformWindow = ResourceLoader.Load<PackedScene>("res://Classes/Visual/WaveformWindow.tscn");

        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        // Instantiate block scenes and add as children
        for (var i = 0; i < Settings.Instance.MaxNumberOfBlocks; i++) {
            var waveformWindow = packedWaveformWindow.Instantiate() as WaveformWindow;
            waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
            AddChild(waveformWindow);
            WaveformWindows.Add(waveformWindow);
        }


        foreach (var waveformWindow in WaveformWindows) {
            waveformWindow.SizeFlagsVertical = SizeFlags.ExpandFill;

            waveformWindow.Playhead.Visible = false;

            waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;
            waveformWindow.AttemptToAddTimingPoint += OnDoubleClick;

            waveformWindow.IsInstantiating = false;
        }

        UpdateNumberOfBlocks();
    }

    public void UpdateNumberOfBlocks() {
        var children = GetChildren();

        for (var i = 0; i < children.Count; i++) {
            var waveformWindow = children[i] as WaveformWindow;
            waveformWindow.Visible = i < Settings.Instance.NumberOfBlocks;
        }
    }


    public void UpdateBlocksScroll() {
        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        var children = GetChildren();

        foreach (WaveformWindow waveformWindow in children) {
            waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
        }
    }

    public void OnSeekPlaybackTime(float playbackTime) {
        EmitSignal(nameof(SeekPlaybackTime), playbackTime);
    }

    public void OnDoubleClick(float playbackTime) {
        EmitSignal(nameof(DoubleClicked), playbackTime);
    }
}