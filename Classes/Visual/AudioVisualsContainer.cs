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

    [Export]
    private PackedScene packedAudioBlock;

    //public float FirstBlockStartTime = 0;

    private int nominalMusicPositionStartForTopBlock;

    public List<AudioBlock> AudioBlocks = new();

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
        //foreach (var waveformWindow in AudioBlocks)
        //{
        //    waveformWindow.UpdateVisuals();
        //}
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
        AudioBlocks.Clear();

        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        // Instantiate block scenes and add as children
        for (var i = 0; i < Settings.Instance.MaxNumberOfBlocks; i++) {
            var audioBlock = packedAudioBlock.Instantiate() as AudioBlock;
            audioBlock.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
            AddChild(audioBlock);
            AudioBlocks.Add(audioBlock);
        }


        foreach (var audioBlock in AudioBlocks) {
            AudioDisplayPanel audioDisplayPanel = audioBlock.AudioDisplayPanel;
            audioDisplayPanel.SizeFlagsVertical = SizeFlags.ExpandFill;

            audioDisplayPanel.Playhead.Visible = false;

            audioDisplayPanel.SeekPlaybackTime += OnSeekPlaybackTime;
            audioDisplayPanel.AttemptToAddTimingPoint += OnDoubleClick;

            audioDisplayPanel.IsInstantiating = false;
        }

        UpdateNumberOfVisibleBlocks();
    }

    public void UpdateNumberOfVisibleBlocks() {
        var children = GetChildren();

        for (var i = 0; i < children.Count; i++) {
            var waveformWindow = children[i] as AudioBlock;
            waveformWindow.Visible = i < Settings.Instance.NumberOfBlocks;
        }
    }


    public void UpdateBlocksScroll() {
        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        var children = GetChildren();

        foreach (var child in children) {
            if (child is not AudioBlock)
            {
                continue;
            }
            AudioBlock audioBlock = child as AudioBlock;
            audioBlock.NominalMusicPositionStartForWindow = musicPositionStart;
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