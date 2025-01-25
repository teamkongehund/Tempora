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
using NAudio.SoundFont;
using System;
using System.Linq;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using System.IO;
using SaveConfig = Tempora.Classes.Utility.ProjectFileManager.SaveConfig;

namespace Tempora.Classes.Visual;
public partial class FileMenu : PopupMenu
{
	[Export]
	ExportWindow exportWindow = null!;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		IdPressed += OnIdPressed;
	}

	private void OnIdPressed(long id)
	{
		switch (id)
		{
			case 1:
				ProjectFileManager.Instance.NewProject();
				break;
			case 2:
				ProjectFileManager.Instance.LoadFileDialogPopup();
				break;
			case 3:
				ProjectFileManager.Instance.SaveProjectFileDialogPopup();
				break;
			case 4:
				OsuExporter.Instance.ExportAndOpenOsz();
				break;
			case 5:
				ProjectFileManager.Instance.SaveOszFileDialogPopup();
				break;
			case 6:
				exportWindow.Popup();
				break;
			case 7:
				BeatSaberExporter.Instance.SaveFile();
				break;
		}
	}
}
