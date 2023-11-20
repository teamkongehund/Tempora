using Godot;
using System;

public partial class AudioPlayer : AudioStreamPlayer
{
	private AudioFile _audioFile;
	public AudioFile AudioFile
	{
		get => _audioFile;
		set
		{
			if (value != _audioFile)
			{
				_audioFile = value;
				LoadMp3();
			}
		}
	}

	public double CurrentPlaybackTime
	{
		get => GetPlaybackTime();
		private set { }
	}

	public double GetPlaybackTime()
	{
		return Playing 
			? GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix()
			: GetPlaybackPosition();
	}

    public void LoadMp3() => Stream = Godot.FileAccess.FileExists(AudioFile.Path)
        ? FileHandler.LoadFileAsAudioStreamMP3(AudioFile.Path)
        : throw new Exception($"Failed to update songPlayer stream - check if {AudioFile.Path} exists.");
}
