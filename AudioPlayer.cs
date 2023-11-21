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

	public double PausePosition;

	public double CurrentPlaybackTime
	{
		get => GetPlaybackTime();
		private set { }
	}

	public void Pause()
	{
		PausePosition = GetPlaybackTime();
		Stop();
	}

	public void Resume()
	{
		Play();
		Seek((float)PausePosition);
	}

	public void PlayPause()
	{
		if (Playing) Pause();
		else Resume();
	}

	public double GetPlaybackTime()
	{
		return Playing 
			? GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix()
			: PausePosition;
	}

    public void LoadMp3() => Stream = Godot.FileAccess.FileExists(AudioFile.Path)
        ? FileHandler.LoadFileAsAudioStreamMP3(AudioFile.Path)
        : throw new Exception($"Failed to update songPlayer stream - check if {AudioFile.Path} exists.");
}
