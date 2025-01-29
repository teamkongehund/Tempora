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
using System.Linq;
using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using System.IO;

// Tempora

namespace Tempora.Classes.Visual;

public partial class Main : Control
{
	//[Export]
	//private string audioPath = "res://Audio/UMO.mp3";
	[Export]
	private AudioStreamMP3 defaultMP3 = null!;
	private MusicPlayer MusicPlayer => MusicPlayer.Instance;
	[Export]
	private AudioVisualsContainer audioVisualsContainer = null!;
	[Export]
	private Metronome metronome = null!;
	[Export]
	private BlockScrollBar blockScrollBar = null!;

	private ProjectFileManager projectFileManager = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		projectFileManager = ProjectFileManager.Instance;

        // This works in Debug if we use i.e. audioPath = "res://Audio/UMO.mp3",
        // but won't work in production, as resources are converted to different file formats.
        //Project.Instance.AudioFile = new AudioFile(audioPath);

        string autoSavePathLocal = $"{ProjectFileManager.AutoSavePath}{ProjectFileManager.ProjectFileExtension}";
        string autoSavePathGlobal = ProjectSettings.GlobalizePath(autoSavePathLocal);
        if (Godot.FileAccess.FileExists(autoSavePathGlobal))
            ProjectFileManager.Instance.LoadProjectFromFilePath(autoSavePathGlobal);
        else
		    Project.Instance.AudioFile = new AudioFile(defaultMP3);

		GlobalEvents.Instance.SettingsChanged += OnSettingsChanged;
		audioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
		GetTree().Root.FilesDropped += OnFilesDropped;

		audioVisualsContainer.CreateBlocks();
		blockScrollBar.UpdateLimits();
		audioVisualsContainer.UpdateBlocksScroll();


		MementoHandler.Instance.AddTimingMemento();
	}

	public override void _Input(InputEvent inputEvent)
	{
		switch (inputEvent)
		{
			case InputEventMouseButton mouseEvent:
				{
					if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
					{
						// Ensure a mouse release is always captured.
						GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.MouseLeftReleased));
						Context.Instance.IsSelectedMeasurePositionMoving = false;
					}
					break;
				}
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
				GrabFocus();
			ReleaseFocus();
		}
	}

	private void OnSettingsChanged(object? sender, EventArgs e) => audioVisualsContainer.UpdateNumberOfVisibleBlocks();

	private void OnFilesDropped(string[] filePaths)
	{
		if (filePaths.Length != 1)
			return;
		string path = filePaths[0];
        bool isAudioFile = AudioFile.IsAudioFileExtensionValid(path, out string extension);

        switch (extension) {
            case var value when value == ProjectFileManager.ProjectFileExtension:
                projectFileManager.LoadProjectFromFilePath(path);
                break;
            case var value when isAudioFile:
		        var audioFile = new AudioFile(path);
		        Project.Instance.AudioFile = audioFile;
                break;
        }
	}

	private void OnSeekPlaybackTime(object? sender, EventArgs e)
	{
		if (e is not GlobalEvents.ObjectArgument<float> floatArgument)
			throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<float>)}");
		float playbackTime = floatArgument.Value;
		MusicPlayer.SeekPlay(playbackTime);
	}

	public override string ToString() => "Main";
}
