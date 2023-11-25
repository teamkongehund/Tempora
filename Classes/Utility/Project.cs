using Godot;
using System;

public partial class Project : Node
{
	public static Project Instance;

	private AudioFile _audioFile;
	public AudioFile AudioFile
	{
		get => _audioFile;
		set
		{
			if (_audioFile == value) return;
			_audioFile = value;
			Signals.Instance.EmitSignal("AudioFileChanged");
		}
	}

	Settings Settings;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
}
