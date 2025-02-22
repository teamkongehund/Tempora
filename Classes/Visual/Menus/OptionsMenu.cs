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

using Godot;
using System;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class OptionsMenu : PopupMenu
{
	[Export]
	private Control blockAmountScrollBar = null!;
	[Export]
	private Control offsetScrollBar = null!;
	[Export]
	private Control overlapScrollBar = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        IndexPressed += OnIndexPressed;
		SetItemChecked(index_PreserveBpm, Settings.Instance.PreserveBPMWhenChangingTimeSignature);
		SetItemChecked(index_MetronopmeFollowsGrid, Settings.Instance.MetronomeFollowsGrid);
		SetItemChecked(index_AutoScroll, Settings.Instance.AutoScrollWhenAddingTimingPoints);
		SetItemChecked(index_RoundBPM, Settings.Instance.RoundBPM);
        SetItemChecked(index_PlaybackOnNewPoints, Settings.Instance.SeekPlaybackOnTimingPointChanges);
        SetItemChecked(index_MoreSettings, Settings.Instance.ShowMoreSettings);
        SetItemChecked(index_Spectrogram, Settings.Instance.RenderAsSpectrogram);
	}

	int index_PreserveBpm = 0;
	int index_MetronopmeFollowsGrid = 1;
	int index_AutoScroll = 2;
	int index_RoundBPM = 3;
	int index_MoreSettings = 4;
    int index_PlaybackOnNewPoints = 5;
    int index_Spectrogram = 6;

	private void OnIndexPressed(long index)
	{
		switch (index)
		{
			case var expression when (index == index_PreserveBpm):
				ToggleCheckBox(index_PreserveBpm, out bool newStatus);
				Settings.Instance.PreserveBPMWhenChangingTimeSignature = newStatus;
				break;
			case var expression when (index == index_MetronopmeFollowsGrid):
				ToggleCheckBox(index_MetronopmeFollowsGrid, out newStatus);
				Settings.Instance.MetronomeFollowsGrid = newStatus;
				break;
			case var expression when (index == index_MoreSettings):
				ToggleCheckBox(index_MoreSettings, out newStatus);
				ShowHideMoreSettings(newStatus);
				break;
			case var expression when (index == index_AutoScroll):
				ToggleCheckBox(index_AutoScroll, out newStatus);
				Settings.Instance.AutoScrollWhenAddingTimingPoints = newStatus;
				break;
			case var expression when (index == index_RoundBPM):
				ToggleCheckBox(index_RoundBPM, out newStatus);
				Settings.Instance.RoundBPM = newStatus;
				break;
            case var expression when (index == index_PlaybackOnNewPoints):
                ToggleCheckBox(index_PlaybackOnNewPoints, out newStatus);
                Settings.Instance.SeekPlaybackOnTimingPointChanges = newStatus;
                break;
            case var expression when (index == index_Spectrogram):
                ToggleCheckBox(index_Spectrogram, out newStatus);
                Settings.Instance.RenderAsSpectrogram = newStatus;
                break;
        }
	}

	private void ToggleCheckBox(int index, out bool newStatus)
	{
		bool isChecked = IsItemChecked(index);
		SetItemChecked(index, !isChecked);
		newStatus = IsItemChecked(index);
	}

	private void ShowHideMoreSettings(bool visible)
	{
		blockAmountScrollBar.Visible = visible;
		offsetScrollBar.Visible = visible;
		overlapScrollBar.Visible = visible;
	}
}
