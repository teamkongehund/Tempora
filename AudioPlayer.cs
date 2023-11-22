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

	public double PauseTime;

	public double CurrentPlaybackTime
	{
		get => GetPlaybackTime();
		private set { }
	}

    public override void _Ready()
    {
		Signals.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
    }

	public void OnSelectedPositionChanged()
	{
		float time = Timing.Instance.MusicPositionToTime(Context.Instance.SelectedMusicPosition);
		if (time >= 0)
            PauseTime = time;
		else
			PauseTime = 0;
    }

    public void Pause()
	{
		//PausePosition = GetPlaybackTime();
		Stop();
	}

	public void Resume()
	{
		Play();
		Seek((float)PauseTime);
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
			: PauseTime;
	}

    public void LoadMp3() => Stream = Godot.FileAccess.FileExists(AudioFile.Path)
        ? FileHandler.LoadFileAsAudioStreamMP3(AudioFile.Path)
        : throw new Exception($"Failed to update songPlayer stream - check if {AudioFile.Path} exists.");
}
